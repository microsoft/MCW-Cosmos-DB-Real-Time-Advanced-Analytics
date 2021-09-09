using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TransactionGenerator;

namespace TransactionGenerator
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Transaction
    {
        /// <summary>
        /// Cosmos DB item id.
        /// </summary>
        [JsonProperty] public string id { get; set; }

        [JsonProperty] public string TransactionID { get; set; }

        [JsonProperty] public string AccountID { get; set; }

        [JsonProperty] public double TransactionAmountUSD { get; set; }

        [JsonProperty] public double TransactionAmount { get; set; }

        [JsonProperty] public string TransactionCurrencyCode { get; set; }

        [JsonProperty] public int LocalHour { get; set; }

        [JsonProperty] public string TransactionIPaddress { get; set; }

        [JsonProperty] public string IpState { get; set; }

        [JsonProperty] public string IpPostcode { get; set; }

        [JsonProperty] public string IpCountryCode { get; set; }

        [JsonProperty] public bool? IsProxyIP { get; set; }

        [JsonProperty] public string BrowserLanguage { get; set; }

        [JsonProperty] public string PaymentInstrumentType { get; set; }

        [JsonProperty] public string CardType { get; set; }

        [JsonProperty] public string PaymentBillingPostalCode { get; set; }

        [JsonProperty] public string PaymentBillingState { get; set; }

        [JsonProperty] public string PaymentBillingCountryCode { get; set; }

        [JsonProperty] public string CvvVerifyResult { get; set; }

        [JsonProperty] public int DigitalItemCount { get; set; }

        [JsonProperty] public int PhysicalItemCount { get; set; }

        [JsonProperty] public string AccountPostalCode { get; set; }

        [JsonProperty] public string AccountState { get; set; }

        [JsonProperty] public string AccountCountry { get; set; }

        [JsonProperty] public int AccountAge { get; set; }

        [JsonProperty] public bool IsUserRegistered { get; set; }

        [JsonProperty] public double PaymentInstrumentAgeInAccount { get; set; }

        [JsonProperty] public int NumPaymentRejects1dPerUser { get; set; }

        [JsonProperty] public DateTime TransactionDateTime { get; set; }

        // Used to set the expiration policy.
        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        // This property is used to indicate the type of document this is within the collection.
        // This allows consumers to query documents stored within the collection by the type.
        // This is needed because a collection can contain any number of document types within,
        // since it does not enforce any type of schema.
        [JsonProperty] public string CollectionType => "Transaction";

        [JsonIgnore]
        public string PartitionKey => IpCountryCode;

        [JsonIgnore]
        protected string CsvHeader { get; set; }

        [JsonIgnore]
        protected string CsvString { get; set; }

        public string GetData()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Transaction FromString(string line, string header)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentException($"{nameof(line)} cannot be null, empty, or only whitespace");
            }

            var tokens = line.Split(',');
            if (tokens.Length != 28)
            {
                throw new ArgumentException($"Invalid record: {line}");
            }

            var tx = new Transaction
            {
                CsvString = line,
                CsvHeader = header
            };
            try
            {
                // Generate a random Cosmos DB document ID to allow for re-inserting the same data each time the generator runs.
                tx.id = Guid.NewGuid().ToString();
                tx.TransactionID = tokens[0];
                tx.AccountID = tokens[1];
                tx.TransactionAmountUSD = double.TryParse(tokens[2], out var dresult) ? dresult : 0.0;
                tx.TransactionAmount = double.TryParse(tokens[3], out dresult) ? dresult : 0.0;
                tx.TransactionCurrencyCode = tokens[4];
                tx.LocalHour = int.TryParse(tokens[5], out var iresult) ? iresult : 0;
                tx.TransactionIPaddress = tokens[6];
                tx.IpState = tokens[7];
                tx.IpPostcode = tokens[8];
                tx.IpCountryCode = string.IsNullOrWhiteSpace(tokens[9]) ? "unk" : tokens[9];
                tx.IsProxyIP = bool.TryParse(tokens[10], out var bresult) && bresult;
                tx.BrowserLanguage = tokens[11];
                tx.PaymentInstrumentType = tokens[12];
                tx.CardType = tokens[13];
                tx.PaymentBillingPostalCode = tokens[14];
                tx.PaymentBillingState = tokens[15];
                tx.PaymentBillingCountryCode = tokens[16];
                tx.CvvVerifyResult = tokens[17];
                tx.DigitalItemCount = int.TryParse(tokens[18], out iresult) ? iresult : 0;
                tx.PhysicalItemCount = int.TryParse(tokens[19], out iresult) ? iresult : 0;
                tx.AccountPostalCode = tokens[20];
                tx.AccountState = tokens[21];
                tx.AccountCountry = tokens[22];
                tx.AccountAge = int.TryParse(tokens[23], out iresult) ? iresult : 0;
                tx.IsUserRegistered = bool.TryParse(tokens[24], out bresult) && bresult;
                tx.PaymentInstrumentAgeInAccount = double.TryParse(tokens[25], out dresult) ? dresult : 0;
                tx.NumPaymentRejects1dPerUser = int.TryParse(tokens[26], out iresult) ? iresult : 0;
                tx.TransactionDateTime = DateTime.TryParse(tokens[27], out var dtresult) ? dtresult : DateTime.UtcNow;

                // Set the TTL value to 60 days, which deletes the document in Cosmos DB after around two months, saving
                // storage costs while meeting Woodgrove Bank's requirement to keep the streaming data available for
                // that amount of time so they can reprocess or query the raw data within the collection as needed.
                // TODO 7: Complete the code to set the time to live (TTL) value to 60 days (in seconds).
                // COMPLETE THIS CODE ... tx.TimeToLive = ...

                return tx;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid record: {line}", ex);
            }
        }
    }
}
