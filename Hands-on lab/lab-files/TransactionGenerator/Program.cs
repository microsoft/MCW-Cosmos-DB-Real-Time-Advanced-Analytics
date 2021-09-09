using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using System.Security;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Bulkhead;
using TransactionGenerator.OutputHelpers;
using Microsoft.Azure.Cosmos;

namespace TransactionGenerator
{
    internal class Program
    {
        //private static DocumentClient _cosmosDbClient;
        private static CosmosClient _cosmosDbClient;
        private static IConfigurationRoot _configuration;

        private const string DatabaseName = "Woodgrove";
        private const string CollectionName = "transactions";
        private const string PartitionKey = "/ipCountryCode";
        private static readonly object LockObject = new object();
        // AutoResetEvent to signal when to exit the application.
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);
        // Track statistics to compare Event Hub vs. Cosmos DB.
        // At any time, requests pending = made - succeeded - failed.
        private static long _totalMessages = 0;
        private static long _eventHubRequestsMade = 0;
        private static long _eventHubRequestsSucceeded = 0;
        private static long _eventHubRequestsFailed = 0;
        private static long _eventHubRequestsSucceededInBatch = 0;
        private static long _eventHubElapsedTime = 0;
        private static long _eventHubTotalElapsedTime = 0;
        private static long _cosmosRequestsMade = 0;
        private static long _cosmosRequestsSucceeded = 0;
        private static long _cosmosRequestsFailed = 0;
        private static long _cosmosRequestsSucceededInBatch = 0;
        private static long _cosmosElapsedTime = 0;
        private static long _cosmosTotalElapsedTime = 0;
        private static double _cosmosRUsPerBatch = 0;

        private static readonly Statistic[] LatestStatistics = new Statistic[0];

        // Separate Event Hub and Cosmos DB calls into two bulkheads to prevent thread starvation caused by failing or waiting calls.
        // Let any number (int.MaxValue) of calls _queue for an execution slot in the bulkhead to allow the generator to send as many calls as possible.
        private const int MaxParallelization = 100000;
        private static readonly BulkheadPolicy BulkheadForEventHubCalls = Policy.BulkheadAsync(MaxParallelization, int.MaxValue);
        private static readonly BulkheadPolicy BulkheadForCosmosDbCalls = Policy.BulkheadAsync(MaxParallelization, int.MaxValue);

        // Extract payment transaction data from the sample CSV file, serialize, and return the collection.
        private static List<Transaction> GetTransactionData(Func<string, string, Transaction> factory)
        {
            var transactions = new List<Transaction>();

            Console.WriteLine("Retrieving sample transaction data...");

            using (var reader = new StreamReader(File.OpenRead(@"cleaned-transactions.csv")))
            {
                var header = reader.ReadLines()
                    .First();
                var lines = reader.ReadLines()
                    .Skip(1);

                // Instantiate a Transaction object from the CSV line and header data, using the passed in factory:
                transactions.AddRange(lines.Select(line => factory(line, header)));
            }

            Console.WriteLine($"Sample transaction data retrieved. {transactions.Count} records found.");

            return transactions;
        }

        // Send data to Cosmos DB and Event Hub:
        private static async Task SendData(List<Transaction> transactions,
            List<EventHubProducerClient> eventHubClients, int randomSeed, int waittime,
            CancellationToken externalCancellationToken, IProgress<Progress> progress,
            bool onlyWriteToCosmosDb)
        {
            if (transactions == null)
            {
                throw new ArgumentNullException(nameof(transactions));
            }

            if (waittime > 0)
            {
                var span = TimeSpan.FromMilliseconds(waittime);
                await Task.Delay(span, externalCancellationToken);
            }

            if (externalCancellationToken == null) throw new ArgumentNullException(nameof(externalCancellationToken));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Perform garbage collection prior to timing for statistics.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var internalCancellationTokenSource = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, internalCancellationTokenSource.Token).Token;
            var random = new Random(randomSeed);
            var tasks = new List<Task>();
            var messages = new ConcurrentQueue<ColoredMessage>();
            var eventHubsTimer = new Stopwatch();
            var cosmosTimer = new Stopwatch();

            // Create the Cosmos DB database and container references:
            var database = _cosmosDbClient.GetDatabase(DatabaseName);
            var container = database.GetContainer(CollectionName);

            // Ensure none of what follows runs synchronously.
            await Task.FromResult(true).ConfigureAwait(false);

            // For each line, send to Event Hub and Cosmos DB.
            foreach (var transaction in transactions)
            {
                if (externalCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _totalMessages++;
                var thisRequest = _totalMessages;

                #region Write to Cosmos DB
                _cosmosRequestsMade++;
                tasks.Add(BulkheadForCosmosDbCalls.ExecuteAsync(async ct =>
                {
                    try
                    {
                        cosmosTimer.Start();

                        // Send to Cosmos DB:
                        // TODO 1: Complete this code to send the Transaction object to Cosmos DB. Capture the returned ResourceResponse object to a new variable.
                        // COMPLETE THIS CODE ... var response = await ...

                        cosmosTimer.Stop();
                        _cosmosElapsedTime = cosmosTimer.ElapsedMilliseconds;

                        // Keep running total of RUs consumed:
                        // TODO 2: Complete this code to append the number of RU/s consumed to the _cosmosRUsPerBatch variable.
                        // WRITE CODE HERE

                        _cosmosRequestsSucceededInBatch++;
                    }
                    catch (CosmosException de)
                    {
                        if (!ct.IsCancellationRequested) messages.Enqueue(new ColoredMessage($"Cosmos DB request {thisRequest} eventually failed with: {de.Message}; Retry-after: {de.RetryAfter} seconds.", Color.Red));

                        _cosmosRequestsFailed++;
                    }
                    catch (Exception e)
                    {
                        if (!ct.IsCancellationRequested) messages.Enqueue(new ColoredMessage($"Cosmos DB request {thisRequest} eventually failed with: {e.Message}", Color.Red));

                        _cosmosRequestsFailed++;
                    }
                }, combinedToken)
                    .ContinueWith((t, k) =>
                    {
                        if (t.IsFaulted) messages.Enqueue(new ColoredMessage($"Request to Cosmos DB failed with: {t.Exception?.Flatten().InnerExceptions.First().Message}", Color.Red));

                        _cosmosRequestsFailed++;
                    }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion)
                );
                #endregion Write to Cosmos DB

                #region Write to Event Hub
                // Only send messages to Event Hub instances if onlyWriteToCosmosDb is set to false.
                if (onlyWriteToCosmosDb == false)
                {
                    _eventHubRequestsMade++;
                    tasks.Add(BulkheadForEventHubCalls.ExecuteAsync(async ct =>
                        {
                            try
                            {
                                var eventData = new EventData(new BinaryData(transaction.GetData()));
                                
                                eventHubsTimer.Start();

                                // Send to Event Hubs:
                                foreach (var eventHubClient in eventHubClients)
                                {
                                    // TODO 4: Complete code to send to Event Hub.
                                    // COMPLETE THIS CODE ... using var eventBatch = // Send eventData and set the partition key to the IpCountryCode field.
                                }

                                eventHubsTimer.Stop();
                                _eventHubElapsedTime = eventHubsTimer.ElapsedMilliseconds;

                                // TODO 5: Complete code to increment the count of number of Event Hub requests that succeeded.
                                // COMPLETE THIS CODE
                            }
                            catch (Exception e)
                            {
                                if (!ct.IsCancellationRequested) messages.Enqueue(new ColoredMessage($"Event Hubs request {thisRequest} eventually failed with: {e.Message}", Color.Red));

                                _eventHubRequestsFailed++;
                            }
                        }, combinedToken)
                        .ContinueWith((t, k) =>
                        {
                            if (t.IsFaulted) messages.Enqueue(new ColoredMessage($"Request to Event Hubs failed with: {t.Exception?.Flatten().InnerExceptions.First().Message}", Color.Red));

                            _eventHubRequestsFailed++;
                        }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion)
                    );
                }
                #endregion Write to Event Hub

                if (_totalMessages % 1000 == 0)
                {
                    eventHubsTimer.Stop();
                    cosmosTimer.Stop();
                    _eventHubTotalElapsedTime += _eventHubElapsedTime;
                    _cosmosTotalElapsedTime += _cosmosElapsedTime;
                    _eventHubRequestsSucceeded += _eventHubRequestsSucceededInBatch;
                    _cosmosRequestsSucceeded += _cosmosRequestsSucceededInBatch;

                    // Calculate RUs/second/month:
                    var ruPerSecond = (_cosmosRUsPerBatch / (_cosmosElapsedTime * .001));
                    var ruPerMonth = ruPerSecond * 86400 * 30;

                    // Random delay every 1000 messages that are sent.
                    //await Task.Delay(random.Next(100, 1000), externalCancellationToken).ConfigureAwait(false);

                    // The obvious and recommended method for sending a lot of data is to do so in batches. This method can
                    // multiply the amount of data sent with each request by hundreds or thousands. However, the point of
                    // our exercise is not to maximize throughput and send as much data as possible, but to compare the
                    // relative performance between Event Hubs and Cosmos DB.

                    // Output statistics. Be on the lookout for the following:
                    //  - Compare Event Hub to Cosmos DB statistics. They should have similar processing times and successful calls.
                    //  - Inserted line shows successful inserts in this batch and throughput for writes/second with RU/s usage and estimated monthly ingestion rate added to Cosmos DB statistics.
                    //  - Processing time: Shows whether the processing time for the past 1,000 requested inserts is faster or slower than the other service.
                    //  - Total elapsed time: Running total of time taken to process all documents.
                    //      - If this value continues to be grow higher for Cosmos DB vs. Event Hubs, that's a good indicator that the Cosmos DB requests are being throttled. Consider increasing the RU/s for the container.
                    //  - Succeeded shows number of accumulative successful inserts to the service.
                    //  - Pending are items in the bulkhead queue. This amount will continue to grow if the service is unable to keep up with demand.
                    //  - Accumulative failed requests that encountered an exception.
                    messages.Enqueue(new ColoredMessage($"Total requests: requested {_totalMessages:00} ", Color.Cyan));
                    if (onlyWriteToCosmosDb == false)
                    {
                        messages.Enqueue(new ColoredMessage(string.Empty));
                        messages.Enqueue(new ColoredMessage($"Event Hub: inserted {_eventHubRequestsSucceededInBatch:00} docs @ {(_eventHubRequestsSucceededInBatch / (_eventHubElapsedTime * .001)):0.00} writes/s ", Color.White));
                        messages.Enqueue(new ColoredMessage($"Event Hub: processing time {_eventHubElapsedTime} ms ({(_eventHubElapsedTime > _cosmosElapsedTime ? "slower" : "faster")})", Color.Magenta));
                        messages.Enqueue(new ColoredMessage($"Event Hub: total elapsed time {(_eventHubTotalElapsedTime * .001):0.00} seconds ({(_eventHubTotalElapsedTime > _cosmosTotalElapsedTime ? "slower" : "faster")})", Color.Magenta));
                        messages.Enqueue(new ColoredMessage($"Event Hub: total succeeded {_eventHubRequestsSucceeded:00} ", Color.Green));
                        messages.Enqueue(new ColoredMessage($"Event Hub: total pending {_eventHubRequestsMade - _eventHubRequestsSucceeded - _eventHubRequestsFailed:00} ", Color.Yellow));
                        messages.Enqueue(new ColoredMessage($"Event Hub: total failed {_eventHubRequestsFailed:00}", Color.Red));

                        eventHubsTimer.Restart();
                        _eventHubElapsedTime = 0;
                        _eventHubRequestsSucceededInBatch = 0;
                    }

                    messages.Enqueue(new ColoredMessage(string.Empty));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: inserted {_cosmosRequestsSucceededInBatch:00} docs @ {(_cosmosRequestsSucceededInBatch / (_cosmosElapsedTime * .001)):0.00} writes/s, {ruPerSecond:0.00} RU/s ({(ruPerMonth / (1000 * 1000 * 1000)):0.00}B max monthly 1KB writes) ", Color.White));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: processing time {_cosmosElapsedTime} ms ({(_cosmosElapsedTime > _eventHubElapsedTime ? "slower" : "faster")})", Color.Magenta));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: total elapsed time {(_cosmosTotalElapsedTime * .001):0.00} seconds ({(_cosmosTotalElapsedTime > _eventHubTotalElapsedTime ? "slower" : "faster")})", Color.Magenta));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: total succeeded {_cosmosRequestsSucceeded:00} ", Color.Green));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: total pending {_cosmosRequestsMade - _cosmosRequestsSucceeded - _cosmosRequestsFailed:00} ", Color.Yellow));
                    messages.Enqueue(new ColoredMessage($"Cosmos DB: total failed {_cosmosRequestsFailed:00}", Color.Red));
                    messages.Enqueue(new ColoredMessage(string.Empty));

                    // Restart timers and reset batch settings:                    
                    cosmosTimer.Restart();
                    _cosmosElapsedTime = 0;
                    _cosmosRUsPerBatch = 0;
                    _cosmosRequestsSucceededInBatch = 0;

                    // Output all messages available right now, in one go.
                    progress.Report(ProgressWithMessages(ConsumeAsEnumerable(messages)));
                }
            }

            messages.Enqueue(new ColoredMessage("Data generation complete", Color.Magenta));
            progress.Report(ProgressWithMessages(ConsumeAsEnumerable(messages)));

            BulkheadForEventHubCalls.Dispose();
            BulkheadForCosmosDbCalls.Dispose();
            eventHubsTimer.Stop();
            cosmosTimer.Stop();
        }

        /// <summary>
        /// Extracts properties from either the appsettings.json file or system environment variables.
        /// </summary>
        /// <returns>
        /// EventHubConnectionString: Connection string to Event Hub for sending data.
        /// EventHub2ConnectionString: Connection string to second Event Hub deployed to a different region.
        /// EventHub3ConnectionString: Connection string to third Event Hub deployed to a different region.
        /// CosmosDbEndpointUrl: The endpoint URL copied from your Cosmos DB properties.
        /// CosmosDbAuthorizationKey: The authorization key copied from your Cosmos DB properties.
        /// MillisecondsToRun: The maximum amount of time to allow the generator to run before stopping transmission of data. The default value is 600. Data will also stop transmitting after the included Untagged_Transactions.csv file's data has been sent.
        /// MillisecondsToLead: The amount of time to wait before sending payment transaction data. Default value is 0.
        /// OnlyWriteToCosmosDb: If set to true, no records will be sent to Event Hubs instances. Default value is false.
        /// </returns>
        private static (string EventHubConnectionString,
                        string EventHub2ConnectionString,
                        string EventHub3ConnectionString,
                        string CosmosDbEndpointUrl,
                        string CosmosDbAuthorizationKey,
                        int MillisecondsToRun,
                        int MillisecondsToLead,
                        bool OnlyWriteToCosmosDb) ParseArguments()
        {
            try
            {
                // The Configuration object will extract values either from the machine's environment variables, or the appsettings.json file.
                var eventHubConnectionString = _configuration["EVENT_HUB_1_CONNECTION_STRING"];
                var eventHub2ConnectionString = _configuration["EVENT_HUB_2_CONNECTION_STRING"];
                var eventHub3ConnectionString = _configuration["EVENT_HUB_3_CONNECTION_STRING"];
                var cosmosDbEndpointUrl = _configuration["COSMOS_DB_ENDPOINT"];
                var cosmosDbAuthorizationKey = _configuration["COSMOS_DB_AUTH_KEY"];
                var numberOfMillisecondsToRun = (int.TryParse(_configuration["SECONDS_TO_RUN"], out var outputSecondToRun) ? outputSecondToRun : 0) * 1000;
                var numberOfMillisecondsToLead = (int.TryParse(_configuration["SECONDS_TO_LEAD"], out var outputSecondsToLead) ? outputSecondsToLead : 0) * 1000;
                bool.TryParse(_configuration["ONLY_WRITE_TO_COSMOS_DB"], out var onlyWriteToCosmosDb);

                if (string.IsNullOrWhiteSpace(eventHubConnectionString) && !onlyWriteToCosmosDb)
                {
                    throw new ArgumentException("EVENT_HUB_CONNECTION_STRING must be provided");
                }

                if (string.IsNullOrWhiteSpace(cosmosDbEndpointUrl))
                {
                    throw new ArgumentException("COSMOS_DB_ENDPOINT must be provided");
                }

                if (string.IsNullOrWhiteSpace(cosmosDbAuthorizationKey))
                {
                    throw new ArgumentException("COSMOS_DB_AUTH_KEY must be provided");
                }

                return (eventHubConnectionString, eventHub2ConnectionString, eventHub3ConnectionString, cosmosDbEndpointUrl, cosmosDbAuthorizationKey,
                    numberOfMillisecondsToRun, numberOfMillisecondsToLead, onlyWriteToCosmosDb);
            }
            catch (Exception e)
            {
                WriteLineInColor(e.Message, ConsoleColor.Red);
                Console.ReadLine();
                throw;
            }
        }

        public static async Task Main(string[] args)
        {
            // Setup configuration to either read from the appsettings.json file (if present) or environment variables.
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            _configuration = builder.Build();

            var arguments = ParseArguments();
            // Set an optional timeout for the generator.
            var cancellationSource = arguments.MillisecondsToRun == 0 ? new CancellationTokenSource() : new CancellationTokenSource(arguments.MillisecondsToRun);
            var cancellationToken = cancellationSource.Token;
            var statistics = new Statistic[0];

            // Set the Cosmos DB connection policy.
            // TODO 3: Complete the code below to create a Cosmos DB connection policy that has a Direct Connection Mode.
            // COMPLETE THIS CODE ... var clientOptions = new CosmosClientOptions...

            var numberOfMillisecondsToLead = arguments.MillisecondsToLead;

            var taskWaitTime = 0;

            if (numberOfMillisecondsToLead > 0)
            {
                taskWaitTime = numberOfMillisecondsToLead;
            }

            var progress = new Progress<Progress>();
            progress.ProgressChanged += (sender, progressArgs) =>
            {
                foreach (var message in progressArgs.Messages)
                {
                    WriteLineInColor(message.Message, message.Color.ToConsoleColor());
                }
                statistics = progressArgs.Statistics;
            };

            WriteLineInColor("Payment Generator", ConsoleColor.White);
            Console.WriteLine("======");
            WriteLineInColor("Press Ctrl+C or Ctrl+Break to cancel.", ConsoleColor.Cyan);
            Console.WriteLine("Statistics for generated payment data will updated for every 1000 sent");
            Console.WriteLine(string.Empty);

            ThreadPool.SetMinThreads(100, 100);

            // Handle Control+C or Control+Break.
            Console.CancelKeyPress += (o, e) =>
            {
                WriteLineInColor("Stopped generator. No more events are being sent.", ConsoleColor.Yellow);
                cancellationSource.Cancel();

                // Allow the main thread to continue and exit...
                WaitHandle.Set();

                OutputStatistics(statistics);
            };

            // Get records to insert:
            var paymentRecords = GetTransactionData(Transaction.FromString);

            // Instantiate Event Hub client(s):
            var eventHubClients = new List<EventHubProducerClient>
            {
                // TODO 6: Create an Event Hub Client from a connection string, using the EventHubConnectionString value.

                // TODO 8: Create additional Event Hub clients from remaining two connection strings.
            };

            // Instantiate Cosmos DB client and start sending messages to Event Hubs and Cosmos DB:
            using (_cosmosDbClient = new CosmosClient(arguments.CosmosDbEndpointUrl,
                arguments.CosmosDbAuthorizationKey, clientOptions))
            {
                await InitializeCosmosDb();

                // Find and output the collection details, including # of RU/s.
                var container = GetContainerIfExists(DatabaseName, CollectionName);
                var throughputResponse = await container.ReadThroughputAsync(requestOptions: null, cancellationToken: cancellationToken);
                
                if (throughputResponse != null)
                {
                    WriteLineInColor($"Found container `{CollectionName}` with a current throughput of {throughputResponse.Resource.Throughput} RU/s with a max throughput of {throughputResponse.Resource.AutoscaleMaxThroughput} reads/second; {throughputResponse.Resource.AutoscaleMaxThroughput / 5} writes/second @ 1KB doc size", ConsoleColor.Green);
                    var estimatedCostPerMonth = 0.06 * throughputResponse.Resource.AutoscaleMaxThroughput;
                    var estimatedCostPerHour = estimatedCostPerMonth / (24 * 30);
                    WriteLineInColor($"The container will cost a maximum of ${estimatedCostPerHour:0.00} per hour (${estimatedCostPerMonth:0.00} per month (per write region))", ConsoleColor.Green);
                }

                // Start sending data to both Event Hubs and Cosmos DB.
                await SendData(paymentRecords, eventHubClients, 100, taskWaitTime, cancellationToken, progress, arguments.OnlyWriteToCosmosDb);
            }

            cancellationSource.Cancel();
            Console.WriteLine();
            WriteLineInColor("Done sending generated transaction data", ConsoleColor.Cyan);
            Console.WriteLine();
            Console.WriteLine();

            OutputStatistics(statistics);

            // Keep the console open.
            Console.ReadLine();
            WaitHandle.WaitOne();
        }

        private static void OutputStatistics(Statistic[] statistics)
        {
            if (!statistics.Any()) return;
            // Output statistics.
            var longestDescription = statistics.Max(s => s.Description.Length);
            foreach (var stat in statistics)
            {
                WriteLineInColor(stat.Description.PadRight(longestDescription) + ": " + stat.Value, stat.Color.ToConsoleColor());
            }
        }

        private static async Task InitializeCosmosDb()
        {
            Database database = await _cosmosDbClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            
            // Create a partition based on the ipCountryCode value from the Transactions data set.
            // This partition was selected because the data will most likely include this value, and
            // it allows us to partition by location from which the transaction originated. This field
            // also contains a wide range of values, which is preferable for partitions.
            var containerProperties = new ContainerProperties(CollectionName, PartitionKey)
            {
                // The default indexing policy for newly created containers is set to automatic and enforces
                // range indexes for any string or number.
                IndexingPolicy = {Automatic = true}
            };

            // Create the container with a maximum autoscale throughput of 4000 RU/s. By default, the
            // minimum RU/s will be 400.
            var container = await database.CreateContainerIfNotExistsAsync(
                containerProperties: containerProperties,
                throughputProperties: ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 4000));
        }

        /// <summary>
        /// Get the container if it exists, null if it doesn't.
        /// </summary>
        /// <returns>The requested container</returns>
        private static Container GetContainerIfExists(string databaseName, string containerName)
        {
            var database = _cosmosDbClient.GetDatabase(databaseName);

            return database?.GetContainer(containerName);
        }

        public static Progress ProgressWithMessage(string message)
        {
            return new Progress(LatestStatistics, new ColoredMessage(message, Color.Default));
        }

        public static Progress ProgressWithMessage(string message, Color color)
        {
            return new Progress(LatestStatistics, new ColoredMessage(message, color));
        }

        public static Progress ProgressWithMessages(IEnumerable<ColoredMessage> messages)
        {
            return new Progress(LatestStatistics, messages);
        }

        public static void WriteLineInColor(string msg, ConsoleColor color)
        {
            lock (LockObject)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        public static IEnumerable<T> ConsumeAsEnumerable<T>(ConcurrentQueue<T> concurrentQueue)
        {
            while (concurrentQueue.TryDequeue(out T got))
            {
                yield return got;
            }
        }

        private static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();

            foreach (char c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }
    }
}