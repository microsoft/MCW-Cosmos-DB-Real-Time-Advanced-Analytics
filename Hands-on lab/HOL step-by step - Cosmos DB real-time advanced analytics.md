![](https://github.com/Microsoft/MCW-Template-Cloud-Workshop/raw/master/Media/ms-cloud-workshop.png 'Microsoft Cloud Workshops')

<div class="MCWHeader1">
Cosmos DB real-time advanced analytics
</div>

<div class="MCWHeader2">
Hands-on lab step-by-step
</div>

<div class="MCWHeader3">
January 2019
</div>

Information in this document, including URL and other Internet Web site references, is subject to change without notice. Unless otherwise noted, the example companies, organizations, products, domain names, e-mail addresses, logos, people, places, and events depicted herein are fictitious, and no association with any real company, organization, product, domain name, e-mail address, logo, person, place or event is intended or should be inferred. Complying with all applicable copyright laws is the responsibility of the user. Without limiting the rights under copyright, no part of this document may be reproduced, stored in or introduced into a retrieval system, or transmitted in any form or by any means (electronic, mechanical, photocopying, recording, or otherwise), or for any purpose, without the express written permission of Microsoft Corporation.

Microsoft may have patents, patent applications, trademarks, copyrights, or other intellectual property rights covering subject matter in this document. Except as expressly provided in any written license agreement from Microsoft, the furnishing of this document does not give you any license to these patents, trademarks, copyrights, or other intellectual property.

The names of manufacturers, products, or URLs are provided for informational purposes only and Microsoft makes no representations and warranties, either expressed, implied, or statutory, regarding these manufacturers or the use of the products with any Microsoft technologies. The inclusion of a manufacturer or product does not imply endorsement of Microsoft of the manufacturer or product. Links may be provided to third party sites. Such sites are not under the control of Microsoft and Microsoft is not responsible for the contents of any linked site or any link contained in a linked site, or any changes or updates to such sites. Microsoft is not responsible for webcasting or any other form of transmission received from any linked site. Microsoft is providing these links to you only as a convenience, and the inclusion of any link does not imply endorsement of Microsoft of the site or the products contained therein.

© 2019 Microsoft Corporation. All rights reserved.

Microsoft and the trademarks listed at <https://www.microsoft.com/en-us/legal/intellectualproperty/Trademarks/Usage/General.aspx> are trademarks of the Microsoft group of companies. All other trademarks are property of their respective owners.

**Contents**

<!-- TOC -->

- [Cosmos DB real-time advanced analytics hands-on lab step-by-step](#cosmos-db-real-time-advanced-analytics-hands-on-lab-step-by-step)
  - [Abstract and learning objectives](#abstract-and-learning-objectives)
  - [Overview](#overview)
  - [Solution architecture](#solution-architecture)
  - [Requirements](#requirements)
  - [Exercise 1: Collecting streaming transaction data](#exercise-1-collecting-streaming-transaction-data)
    - [Task 1: Configuring Event Hubs and the transaction generator](#task-1-configuring-event-hubs-and-the-transaction-generator)
    - [Task 2: Ingesting streaming data into Cosmos DB](#task-2-ingesting-streaming-data-into-cosmos-db)
    - [Task 3: Choosing between Cosmos DB and Event Hubs for ingestion](#task-3-choosing-between-cosmos-db-and-event-hubs-for-ingestion)
  - [Exercise 2: Understanding and preparing the transaction data at scale](#exercise-2-understanding-and-preparing-the-transaction-data-at-scale)
    - [Task 1: Create a service principal for OAuth access to the ADLS Gen2 filesystem](#task-1-create-a-service-principal-for-oauth-access-to-the-adls-gen2-filesystem)
    - [Task 2: Grant ADLS Gen2 access permissions to the service principal](#task-2-grant-adls-gen2-access-permissions-to-the-service-principal)
    - [Task 3: Retrieve your Azure AD tenant ID](#task-3-retrieve-your-azure-ad-tenant-id)
    - [Task 4: Install the Azure Cosmos DB Spark Connector and scikit-learn libraries in Databricks](#task-4-install-the-azure-cosmos-db-spark-connector-and-scikit-learn-libraries-in-databricks)
    - [Task 5: Explore historical transaction data with Azure Databricks and Spark](#task-5-explore-historical-transaction-data-with-azure-databricks-and-spark)
    - [Task 6: Responding to streaming transactions using the Cosmos DB Change Feed and Spark Structured Streaming in Azure Databricks](#task-6-responding-to-streaming-transactions-using-the-cosmos-db-change-feed-and-spark-structured-streaming-in-azure-databricks)
  - [Exercise 3: Creating and evaluating fraud models](#exercise-3-creating-and-evaluating-fraud-models)
    - [Task 1: Install the AzureML and Scikit-Learn libraries in Databricks](#task-1-install-the-azureml-and-scikit-learn-libraries-in-databricks)
    - [Task 2: Prepare and deploy scoring web service](#task-2-prepare-and-deploy-scoring-web-service)
    - [Task 3: Prepare batch scoring model](#task-3-prepare-batch-scoring-model)
  - [Exercise 4: Scaling globally](#exercise-4-scaling-globally)
    - [Task 1: Distributing batch scored data globally using Cosmos DB](#task-1-distributing-batch-scored-data-globally-using-cosmos-db)
    - [Task 2: Using an Azure Databricks job to batch score transactions on a schedule](#task-2-using-an-azure-databricks-job-to-batch-score-transactions-on-a-schedule)
  - [Exercise 5: Reporting](#exercise-5-reporting)
    - [Task 1: Utilizing Power BI to summarize and visualize global fraud trends](#task-1-utilizing-power-bi-to-summarize-and-visualize-global-fraud-trends)
    - [Task 2: Creating dashboards in Azure Databricks](#task-2-creating-dashboards-in-azure-databricks)
  - [After the hands-on lab](#after-the-hands-on-lab)
    - [Task 1: Delete the resource group](#task-1-delete-the-resource-group)

<!-- /TOC -->

# Cosmos DB real-time advanced analytics hands-on lab step-by-step

## Abstract and learning objectives

Woodgrove Bank, who provides payment processing services for commerce, is looking to design and implement a PoC of an innovative fraud detection solution. They want to provide new services to their merchant customers, helping them save costs by applying machine learning and advanced analytics to detect fraudulent transactions. Their customers are around the world, and the right solutions for them would minimize any latencies experienced using their service by distributing as much of the solution as possible, as closely as possible, to the regions in which their customers use the service.

In this hands-on lab session, you will implement a PoC of the data pipeline that could support the needs of Woodgrove Bank.

At the end of this workshop, you will be better able to implement solutions that leverage the strengths of Cosmos DB in support of advanced analytics solutions that require high throughput ingest, low latency serving and global scale in combination with scalable machine learning, big data and real-time processing capabilities.

## Overview

Woodgrove Bank, who provides payment processing services for commerce, is looking to design and implement a proof-of-concept (PoC) of an innovative fraud detection solution. They want to provide new services to their merchant customers, helping them save costs by applying machine learning and advanced analytics to detect fraudulent transactions. Their customers are around the world, and the right solutions for them would minimize any latencies experienced using their service by distributing as much of the solution as possible, as closely as possible, to the regions in which their customers use the service.

## Solution architecture

Below is a diagram of the solution architecture you will build in this lab. Please study this carefully, so you understand the whole of the solution as you are working on the various components.

![The Solution diagram is described in the text following this diagram.](../Media/outline-architecture.png 'Solution diagram')

The solution begins with the payment transaction systems writing transactions to Azure Cosmos DB. Using the built-in change feed feature in Cosmos DB, the transactions can be read as a stream of incoming data within an Azure Databricks notebook, using the `azure-cosmosdb-spark` connector, and stored long-term within an Azure Databricks Delta table backed by Azure Data Lake Storage. The Delta tables efficiently manage inserts and updates (e.g., upserts) to the transaction data. Tables created in Databricks over this data can be accessed by business analysts using dashboards and reports in Power BI, by using Power BI's Spark connector. Alternately, semantic models can be stored in Azure Analysis Service to serve data to Power BI, eliminating the need to keep a dedicated Databricks cluster running for reporting.
Data scientists and engineers can create their own reports against Databricks tables, using Azure Databricks notebooks.

Azure Databricks also supports training and validating the machine learning model, using historical data stored in Azure Data Lake Storage. The model can be periodically re-trained using the data stored in Delta tables or other historical tables. The Azure Machine Learning service is used to deploy the trained model as a real-time scoring web service running on a highly available Azure Kubernetes Service cluster (AKS cluster). The trained model is also used in scheduled offline scoring through Databricks jobs, and the "suspicious activity" output is stored in Azure Cosmos DB so it is globally available in regions closest to Woodgrove Bank's customers through their web applications.

Finally, Azure Key Vault is used to securely store secrets, such as account keys and connection strings, and serves as a backing for Azure Databricks secret scopes.

> **Note**: The preferred solution is only one of many possible, viable approaches.

## Requirements

1. Microsoft Azure subscription (non-Microsoft subscription, must be a pay-as-you subscription).
2. An Azure Databricks cluster running Databricks Runtime 5.1 or above. Azure Databricks integration with Azure Data Lake Storage Gen2 is **fully supported in Databricks Runtime 5.1**.
   - **IMPORTANT**: To complete the OAuth 2.0 access components of this hands-on lab you must:
     - Have a cluster running Databricks Runtime 5.1 and above.
     - Have permissions within your Azure subscription to create an App Registration and service principal within Azure Active Directory.

## Exercise 0:

Duration: 05 minutes.

If you are using pre-configured environment for Before Hands on Lab part, please execute the below task otherwise you can skip this.
If you've already created cosmosdb account then you'll need to add the collections manually. Please follow the steps from step 4 to step 15.

### Task 1: Provision Cosmos DB

In this task, you will create an Azure Cosmos DB account, database, and container for ingesting streaming payment data and for serving batch processed data.

1. In the [Azure portal](https://portal.azure.com), select **+ Create a resource**, enter "cosmos db" into the Search the Marketplace box, select **Azure Cosmos DB** from the results, and then select **Create**.

   ![Create a resource is highlighted in the left-hand navigation menu of the Azure portal, cosmos db is entered into the Search the Marketplace box, and Azure Cosmos DB is highlighted in the results.](media/create-resource-cosmos-db.png 'Create an Azure Cosmos DB account')

2. On the Create Cosmos DB blade's basics tab, enter the following:

   - **Subscription**: Select the subscription you are using for this hands-on lab.
   - **Resource Group**: Choose the hands-on-lab-SUFFIX resource group.
   - **Account Name**: Enter a globally unique name (indicated by a green check mark).
   - **API**: Select Core (SQL).
   - **Location**: Select the region you are using for resources in this hands-on lab.
   - **Geo-Redundancy**: Enable.
   - **Multi-region Writes**: Enable.

   ![The Create Cosmos DB blade's basics tab is displayed, with the previously mentioned settings entered into the appropriate fields.](media/create-cosmos-db-blade.png 'Create Cosmos DB')

3. On the Review blade, select **Create**.

4. Navigate to the newly provisioned Azure Cosmos DB account in the Azure portal, then select **Data Explorer** on the left-hand menu.

   ![Data Explorer is selected within the left-hand menu](media/cosmos-db-data-explorer-link.png 'Select Data Explorer')

5. Select **New Collection** in the top toolbar.

   ![The New Collection button is highlighted on the top toolbar](media/new-collection-button.png 'New Collection')

6. In the **Add Collection** blade, configure the following:

   - **Database id**: Select **Create new**, then enter "Woodgrove" for the id.
   - **Provision database throughput**: Unchecked.
   - **Collection id**: Enter "transactions".
   - **Partition key**: Enter "/ipCountryCode".
   - **Throughput**: Enter 15000.
   
   >**Note**: The /ipCountryCode partition was selected because the data will most likely include this value, and it allows us to partition by location from which the transaction originated. This field also contains a wide range of values, which is preferable for partitions.

   ![The Add Collection blade is displayed, with the previously mentioned settings entered into the appropriate fields.](media/cosmos-db-add-collection-blade.png 'Add Collection blade')

7. Select **Firewall and virtual networks** from the left-hand menu, then select Allow access from **All networks**. Select **Save**. This will allow the payment generator application to send data to your Cosmos DB collection.

   ![The Firewall and virtual networks blade is displayed with the All networks radio button highlighted and selected.](media/cosmos-db-firewall.png 'Firewall and virtual networks blade')

8. Select **Keys** from the left-hand menu.

   ![Keys is selected within the left-hand menu](media/cosmos-db-keys-link.png 'Select Keys')

9. Copy both the **URI** and **Primary Key** values and save to Notepad or similar text editor for later use in the TransactionGenerator console application. You will also insert these values into Key Vault in the next steps.

   ![The Cosmos DB Read-write Keys blade is displayed with highlights around the copy buttons for both URI and Primary Key.](media/cosmos-db-keys.png 'Cosmos DB Read-write Keys')

10. Open a new browser tab or window and navigate to your Azure Key Vault account in the Azure portal, then select **Secrets** under Settings on the left-hand menu. On the Secrets blade, select **+ Generate/Import** on the top toolbar.

    ![Secrets is highlighted on the left-hand menu, and Generate/Import is highlighted on the top toolbar of the Secrets blade.](media/key-vault-secrets.png 'Key Vault secrets blade')

11. On the Create a secret blade, enter the following:

    - **Upload options**: Select Manual.
    - **Name**: Enter "Cosmos-DB-URI".
    - **Value**: Paste the Azure Cosmos DB URI value you copied in an earlier step.

    ![The Create a secret blade is displayed, with the previously mentioned values entered into the appropriate fields.](media/key-vault-create-uri-secret.png 'Create a secret')

12. Select **Create**.

13. Select **+ Generate/Import** again on the top toolbar to create another secret.

14. On the Create a secret blade, enter the following:

    - **Upload options**: Select Manual.
    - **Name**: Enter "Cosmos-DB-Key".
    - **Value**: Paste the Azure Cosmos DB Primary Key value you copied in an earlier step.

    ![The Create a secret blade is displayed, with the previously mentioned values entered into the appropriate fields.](media/key-vault-create-key-secret.png 'Create a secret')

15. Select **Create**.

If you are using pre-configured environment for Before Hands on Lab part, please execute the below task otherwise you can skip this.
We know there is no API from Databricks to configure secret with Key Vault so this can not be automated.

### Task 2: Configure Azure Databricks Key Vault-backed secrets

In this task, you will connect to your Azure Databricks workspace and configure Azure Databricks secrets to use your Azure Key Vault account as a backing store.

1. Return to the [Azure portal](https://portal.azure.com), navigate to the Azure Databricks workspace you provisioned above, and select **Launch Workspace** from the overview blade, signing into the workspace with your Azure credentials, if required.

   ![The Launch Workspace button is displayed on the Databricks Workspace Overview blade.](media/databricks-launch-workspace.png 'Launch Workspace')

2. In your browser's URL bar, append **#secrets/createScope** to your Azure Databricks base URL (for example, <https://eastus.azuredatabricks.net#secrets/createScope>).

3. Enter `key-vault-secrets` for the name of the secret scope.

4. Select **Creator** within the Manage Principal drop-down to specify only the creator (which is you) of the secret scope has the MANAGE permission.

   > MANAGE permission allows users to read and write to this secret scope, and, in the case of accounts on the Azure Databricks Premium Plan, to change permissions for the scope.

   > Your account must have the Azure Databricks Premium Plan for you to be able to select Creator. This is the recommended approach: grant MANAGE permission to the Creator when you create the secret scope, and then assign more granular access permissions after you have tested the scope.

5. Enter the **DNS Name** (for example, <https://woodgrove-vault.vault.azure.net/>) and **Resource ID** you copied earlier during the Key Vault creation step, for example: `/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/resourcegroups/hands-on-lab/providers/Microsoft.KeyVault/vaults/woodgrove-vault`.

   ![Create Secret Scope form](media/create-secret-scope.png 'Create Secret Scope')

6. Select **Create**.

After a moment, you will see a dialog verifying that the secret scope has been created.

## Exercise 2: Collecting streaming transaction data

Duration: 30 minutes

In this exercise, you will configure a payment transaction generator to write real-time streaming online payments to both Event Hubs and Azure Cosmos DB. By the end, you will have selected the best ingest option before continuing to the following exercise where you will process the generated data.

### Task 1: Configuring Event Hubs and the transaction generator

In this task, you will configure the payment transaction data generator project by completing TODO items in the source code and adding connection information for your Event Hub.

1. Open File Explorer on your machine or VM and navigate to the location you extracted the MCW repo .zip file to (C:\\CosmosMCW\\).

2. Open **TransactionGenerator.sln** in the `Hands-on lab\lab-files\TransactionGenerator` directory. This will open the solution in Visual Studio.

   ![Screenshot of the solution folder with the TransactionGenerator.sln file selected.](media/solution-folder.png 'File Explorer')

3. Double-click `appsettings.json` in the Solution Explorer to open it. This file contains the settings used by the console app to connect to your Azure services and to configure application behavior settings. The console app is programmed to either use values stored in this file, or within the machine's environment variables. This makes you capable of distributing the executable or containerizing it and passing in environment variables via the command line.

   ![appsettings.json is highlighted in the Visual Studio Solution Explorer.](media/solution-explorer.png 'Solution Explorer')

   The `appsettings.json` file contains the following:

   ```javascript
   {
       "EVENT_HUB_1_CONNECTION_STRING": "",
       "EVENT_HUB_2_CONNECTION_STRING": "",
       "EVENT_HUB_3_CONNECTION_STRING": "",

       "COSMOS_DB_ENDPOINT": "",
       "COSMOS_DB_AUTH_KEY": "",

       "SECONDS_TO_LEAD": "0",
       "SECONDS_TO_RUN": "600",
       "ONLY_WRITE_TO_COSMOS_DB": "false"
   }
   ```

   `SECONDS_TO_LEAD` is the amount of time to wait before sending payment transaction data. Default value is `0`.

   `SECONDS_TO_RUN` is the maximum amount of time to allow the generator to run before stopping transmission of data. The default value is `600`. Data will also stop transmitting after the included `Untagged_Transactions.csv` file's data has been sent.

   If the `ONLY_WRITE_TO_COSMOS_DB` property is set to `true`, no records will be sent to the Event Hubs instances. Default value is `false`.

4. Copy your Event Hub connection string value you saved during the steps you completed in the before the hands-on lab setup guide. Paste this value into the double-quotes located next to `EVENT_HUB_1_CONNECTION_STRING`.

   ![The Event Hub connection string is pasted in appsettings.json.](media/event-hub-connection-string.png 'appsettings.json')

5. Save the file.

6. Open `Program.cs` in the Visual Studio Solution Explorer.

7. In Visual Studio, select **View**, then **Task List** from the dropdown menu.

   ![In Visual Studio, select View then Task List.](media/select-task-list.png 'Select task list')

   This will display the TODO items within the code comments as a list of tasks you can double-click to jump to its location.

   ![Screenshot of the task list.](media/task-list.png 'Visual Studio Task List')

8. Go to **TODO 1** located in `Program.cs` by double-clicking the item in the Task List. Paste the following code under TODO 1, which uses the Event Hub client to send the event data, setting the partition key to `IpCountryCode`:

   ```csharp
   await eventHubClient.SendAsync(eventData: eventData,
       partitionKey: transaction.IpCountryCode).ConfigureAwait(false);
   ```

   Your completed code should look like the following:

   ![Screenshot of completed TODO 1 code.](media/completed-todo-1-code.png 'Completed code')

9. Paste the code below under **TODO 2** to increment the count of the number of Event Hub requests that succeeded:

   ```csharp
   _eventHubRequestsSucceededInBatch++;
   ```

10. Paste the code below under **TODO 3** to instantiate a new Event Hub client and add it to the `eventHubClients` collection:

    ```csharp
    EventHubClient.CreateFromConnectionString(
        arguments.EventHubConnectionString
    ),
    ```

11. Save your changes.

### Task 2: Ingesting streaming data into Cosmos DB

In this task, you will configure Cosmos DB's time-to-live (TTL) settings to On with no default. This will allow the data generator to expire, or delete, the ingested messages after any desired period of time by setting the TTL value (object property of `ttl`) on individual messages as they are sent.

Next you will pass in the Azure Cosmos DB URI and Key values to the data generator so it can connect to and send events to your collection.

1. Navigate to your Azure Cosmos DB account in the Azure portal, then select **Data Explorer** on the left-hand menu.

   ![Data Explorer is selected within the left-hand menu](media/cosmos-db-data-explorer-link.png 'Select Data Explorer')

2. Expand your **Woodgrove** database and your **transactions** collection, then select **Scale & Settings**.

3. Under Settings within the Scale & Settings blade, select the **On (no default)** option for Time to Live. This setting is required to allow documents added to the collection to be configured with their own TTL values.

   ![The Scale & Settings blade is shown with the On (no default) Time to Live option selected.](media/cosmos-db-ttl-settings.png 'Scale & Settings blade')

4. Select **Save** to apply your settings.

5. Open Visual Studio to go back to the TransactionGenerator project.

6. Open the `appsettings.json` file once more. Paste your Cosmos DB endpoint value next to `COSMOS_DB_ENDPOINT`, and the Cosmos DB authorization key next to `COSMOS_DB_AUTH_KEY`. You recorded these values during the Cosmos DB provisioning steps in the before the hands-on lab setup guide. For example:

   ![The Cosmos DB values have been added to appsettings.json](media/cosmos-db-values.png 'appsettings.json')

7. Open `Program.cs` and paste the code below under **TODO 4** to send the generated transaction data to Cosmos DB and store the returned `ResourceResponse` object into a new variable for statistics about RU/s used:

   ```csharp
   var response = await _cosmosDbClient.CreateDocumentAsync(collectionUri, transaction)
       .ConfigureAwait(false);
   ```

8. Paste the code below under **TODO 5** to append the number of RU/s consumed to the `_cosmosRUsPerBatch` variable:

   ```csharp
   _cosmosRUsPerBatch += response.RequestCharge;
   ```

9. Paste the code below under **TODO 6** to set the Cosmos DB connection policy:

   ```csharp
   var connectionPolicy = new ConnectionPolicy
   {
       ConnectionMode = ConnectionMode.Direct,
       ConnectionProtocol = Protocol.Tcp
   };
   ```

10. Save your changes.

11. Run the console app by clicking **Debug**, then **Start Debugging** in the top menu in Visual Studio, or press _F-5_ on your keyboard.

    ![Screenshot showing the Debug menu expanded in Visual Studio with the Start Debugging menu option highlighted.](media/debug-in-vs.png 'Debug')

12. The PaymentGenerator console window will open, and you should see it start to send data after a few seconds. You may close the window or press `Ctrl+C` or `Ctrl+Break` at any time to stop sending data to Event Hubs and Cosmos DB.

    ![Screenshot of the PaymentGenerator console window running.](media/payment-generator-console.png 'Payment Generator console window')

    The top of the output displays information about the Cosmos DB collection you created (transactions), the requested RU/s as well as estimated hourly and monthly cost. After every 1,000 records are requested to be sent, you will see output statistics so you can compare Event Hubs to Cosmos DB. Be on the lookout for the following:

    - Compare Event Hub to Cosmos DB statistics. They should have similar processing times and successful calls.
    - Inserted line shows successful inserts in this batch and throughput for writes/second with RU/s usage and estimated monthly ingestion rate added to Cosmos DB statistics.
    - Processing time: Shows whether the processing time for the past 1,000 requested inserts is faster or slower than the other service.
    - Total elapsed time: Running total of time taken to process all documents.
    - If this value continues to grow higher for Cosmos DB vs. Event Hubs, that is a good indicator that the Cosmos DB requests are being throttled. Consider increasing the RU/s for the container.
    - Succeeded shows number of accumulative successful inserts to the service.
    - Pending are items in the bulkhead queue. This amount will continue to grow if the service is unable to keep up with demand.
    - Accumulative failed requests that encountered an exception.
    
    > The obvious and recommended method for sending a lot of data is to do so in batches. This method can multiply the amount of data sent with each request by hundreds or thousands. However, the point of our exercise is not to maximize throughput and send as much data as possible, but to compare the relative performance between Event Hubs and Cosmos DB.

13. As an experiment, scale the number of requested RU/s for your Cosmos DB collection down to 750. After doing so, you should see increasingly slower transfer rates to Cosmos DB due to throttling. You will also see the pending queue growing at a higher rate. The reason for this is because when the number of writes (remember, writes _typically_ use 5 RU/s vs. just 1 RU/s for reads on 1 KB-sized documents) exceeds the allotted amount of RU/s, Cosmos DB sends a 429 response with a _retry_after_ header value to tell the consumer that it is resource-constrained. The SDK automatically handles this by waiting for the specified amount of time, then retrying. After you are done experimenting, set the RU/s back to 15,000.

### Task 3: Choosing between Cosmos DB and Event Hubs for ingestion

Woodgrove Bank has a number of requirements around ingesting payment data, including data retention of the hot data and geographic locations to which the data is replicated for high availability and global distribution of the data for processing. There are many similarities between Event Hubs and Cosmos DB that allow both to work well for data ingestion. However, these services have some significant differences in their overall feature set that you need to evaluate to choose the best option for this customer situation.

In this exercise, you will use the data generator to send data to both Event Hubs and Cosmos DB and compare the performance of the two. You will also configure the generator and the services to set message retention and to send data to various global regions.

1. Open Visual Studio and paste the code below under **TODO 7** (located within `Transaction.cs`) to set the time to live (TTL) value to 60 days (in seconds):

   ```csharp
   tx.TimeToLive = 60 * 60 * 24 * 60;
   ```

   This configures Cosmos DB to automatically delete the ingested messages after 60 days by setting the TTL value (`ttl` property) on individual messages as they are sent. This optimization helps save in storage costs while meeting Woodgrove Bank's requirement to keep the streaming data available for that amount of time so they can reprocess in Azure Databricks, or query the raw data within the collection as needed.

   > Setting the TTL for documents saved to Cosmos DB individually for any length of time desired (even beyond 7 days) is an advantage Cosmos DB has over Event Hubs when used for ingesting streaming data.

2. Save your changes.

3. Navigate to your Event Hubs namespace for this lab in the Azure portal. Select **Event Hubs** from the left-hand menu.

   ![Event Hubs is selected within the left-hand menu](media/select-event-hubs.png 'Select Event Hubs')

4. Select your **transactions** event hub.

5. Select **Properties** on the left-hand menu.

6. Drag the **Message Retention** slider all the way to the right to set the value to 7. This is as long as you can set message retention for Event Hubs messages using the portal UI. It is possible to contact Microsoft to set the value to as many as 4 weeks. Unfortunately, this does not meet Woodgrove Bank's requirements for retaining this hot data for 60 days.

   ![Screenshot displaying the event hub properties and the Message Retention value being set to 7.](media/event-hub-message-retention.png 'Properties')

7. Select **Save Changes**.

   > Woodgrove Bank wants to write all transaction data simultaneously to three different geographic locations: United States, Great Britain, and East Asia. All data should be able to be read from these locations with as little latency as possible. They require this for redundancy purposes as well as being able to better process the data in those regions.

8. Create two more Event Hubs namespaces and event hubs within to ingest transaction data. In the [Azure portal](https://portal.azure.com), select **+ Create a resource**, enter "event hubs" into the Search the Marketplace box, select **Event Hubs** from the results, and then select **Create**.

   ![Create a resource is highlighted in the left-hand navigation menu of the Azure portal, event hubs is entered into the Search the Marketplace box, and Event Hubs is highlighted in the results.](media/create-resource-event-hubs.png 'Create an Event Hubs namespace')

9. On the Create Namespace blade, enter the following:

   - **Name**: Enter a globally unique name (indicated by a green check mark).
   - **Pricing tier**: Select Standard.
   - **Enable Kafka**: Unchecked
   - **Make this namespace zone redundant**: Unchecked
   - **Subscription**: Select the subscription you are using for this hands-on lab.
   - **Resource group**: Choose the hands-on-lab-SUFFIX resource group.
   - **Location**: Select **UK South**. (If you already selected this for the first Event Hub namespace, select a US region)
   - **Throughput Units**: Set the slider all the way to the left, setting the value to 1.
   - **Enable Auto-Inflate**: Unchecked

   ![The Create Namespace blade is displayed, with the previously mentioned settings entered into the appropriate fields.](media/create-event-hubs-blade-uk.png 'Create Namespace')

10. Select **Create**.

11. Navigate to the newly provisioned Event Hubs namespace in the Azure portal, then select **Event Hubs** under Entities on the left-hand menu.

    ![Event Hubs is selected within the left-hand menu](media/select-event-hubs.png 'Select Event Hubs')

12. Select **+ Event Hub** in the top toolbar.

    ![Select the + Event Hub button in the top toolbar](media/add-event-hub-button.png 'Add Event Hub')

13. In the **Create Event Hub** blade, configure the following:

    - **Name**: Enter "transactions".
    - **Partition Count**: Move the slider to set the value to 10.
    - **Message Retention**: Set to 7.
    - **Capture**: Off

    ![The Create Event Hub blade is displayed, with the previously mentioned settings entered into the appropriate fields](media/create-event-hub-blade-7-day-retention.png 'Create Event Hub')

14. Select **Create**.

15. After the new Event Hub is created, select it then select **Shared access policies** under Settings in the left-hand menu.

    ![Shared access policies is selected within the left-hand menu](media/select-shared-access-policies.png 'Select Shared access policies')

16. Select **+ Add** in the top toolbar.

    ![Select the + Add button in the top toolbar](media/add-shared-access-policy.png 'Add Shared Access Policy')

17. In the **Add SAS Policy** blade, configure the following:

    - **Policy name**: Enter "Sender".
    - **Manage**: Unchecked
    - **Send**: Checked
    - **Listen**: Unchecked

    ![The Add SAS Plicy is displayed, with the previously mentioned settings entered into the appropriate fields](media/add-sas-policy-sender.png 'Add SAS Policy')

18. Select **Create**.

19. Select **+ Add** in the top toolbar to add another policy.

    ![Select the + Add button in the top toolbar](media/add-shared-access-policy.png 'Add Shared Access Policy')

20. In the **Add SAS Policy** blade, configure the following:

    - **Policy name**: Enter "Listener".
    - **Manage**: Unchecked
    - **Send**: Unchecked
    - **Listen**: Checked

    ![The Add SAS Plicy is displayed, with the previously mentioned settings entered into the appropriate fields](media/add-sas-policy-listener.png 'Add SAS Policy')

21. Select **Create**.

22. Select the **Sender** access policy.

23. Copy the **Connection string-primary key** value. Save this value for the Sender policy in Notepad or similar for later.

24. Follow the step above to copy the **Connection string-primary key** value for the Listener policy and save for later.

25. **Repeat steps 8 through 24 above** to create a new Event Hub namespace in the **East Asia** region.

    ![Create Event Hubs Namespace blade with the East Asia Location selection highlighted.](media/create-event-hubs-blade-asia.png 'Create Namespace')

26. Open Visual Studio to go back to the TransactionGenerator project.

27. Open the `appsettings.json` file once more. Paste your two new Event Hub connection strings into the values for `EVENT_HUB_2_CONNECTION_STRING` and `EVENT_HUB_3_CONNECTION_STRING`, respectively.

    ![The two new Event Hub connection strings are pasted into the appsettings.json file.](media/event-hub-connection-strings.png 'appsettings.json')

28. Save your changes.

29. Open `Program.cs` and paste the below code underneath **TODO 8** to add two new Event Hub clients to the `eventHubClients` collection, using the two new connection string values:

    ```csharp
    EventHubClient.CreateFromConnectionString(
        arguments.EventHub2ConnectionString
    ),
    EventHubClient.CreateFromConnectionString(
        arguments.EventHub3ConnectionString
    )
    ```

    ![Screenshot displaying completed code.](media/event-hub-clients-added.png 'Program.cs')

30. Now, we will add the two additional regions to Cosmos DB. Navigate to the Azure portal and select your Cosmos DB account you created for this lab.

31. Select **Replicate data globally** underneath Settings in the left-hand menu.

    ![Left-hand menu with the Replicate data globally link highlighted.](media/replicate-data-globally-link.png 'Replicate data globally link')

32. Within the Replicate data globally blade, select **+ Add region** above the listed regions in the Configure regions section.

    ![Screenshot with the Add region link highlighted.](media/add-region-link.png 'Add region link')

    > Notice that there are already two regions and they each have both reads and writes enabled. This is because you enabled the geo-redundancy and multi-region writes options when you provisioned Cosmos DB.

33. Select **East Asia** in the dropdown list, then select **OK** to add the region.

    ![East Asia is selected and highlighted in the location dropdown list.](media/add-region-east-asia.png 'East Asia')

34. Select **+ Add region** again, this time selecting **UK South** in the dropdown list. Select **OK** to add the new region.

    ![UK South is selected and highlighted in the location dropdown list.](media/add-region-uk-south.png 'UK South')

35. Notice that the two new regions are highlighted on the world map, and each have both reads and writes enabled. Congratulations! You completed all the steps to write to and read from multiple regions around the world with Cosmos DB! Finally, select **Save** to save your changes.

    ![Map showing newly added regions for Cosmos DB.](media/replicate-data-globally-map.png 'Cosmos DB region map')

    > **Note**: You may have to wait several minutes for the change to take effect. In the meantime, you can feel free to continue and run the transaction generator. Cosmos DB can still ingest data as regions are being added. There should be no performance impact during this time or after the provisioning is complete.

36. Open Visual Studio and debug the TransactionGenerator project. Let it run for at least 1 minute, or long enough to send 5,000 messages.

    ![The TransactionGenerator console shows event hubs overall running slower than Cosmos DB.](media/console-output-event-hubs-slower.png 'Console output')

    Results will vary depending on machine specifications and network speeds, but overall, it will likely take longer to send the data to the three Event Hub instances than to Cosmos DB. You may also notice the Event Hubs pending queue filling up quite a bit more. Also notice that you did not have to make any code changes to write to the additional Cosmos DB regions.

37. Open each of the three Event Hubs namespaces you have created for this lab. You should see an equal number of messages that were sent to each. The graph is shown on the bottom of the Overview blade. Select the **Messages** metric above the graph to view the number of messages received. The screenshot below is of the UK South Event Hub:

    ![The Messages metric is selected for the UK South Event Hubs namespace.](media/uk-south-metrics.png 'Event Hubs Overview blade')

38. View the data that was saved to Cosmos DB. Navigate to the Cosmos DB account for this lab in the Azure portal. Select **Data Explorer** on the left-hand menu. Expand the **Woodgrove** database and **transactions** collection, then select **Documents**. Select one of the documents from the list to view it. If you selected a more recently added document, notice that it contains a `ttl` value of 5,184,000 seconds, or 60 days. Also, there is a `collectionType` value of "Transaction". This allows consumers to query documents stored within the collection by the type. This is needed because a collection can contain any number of document types within, since it does not enforce any type of schema.

    ![Screenshot shows a document displayed within the Cosmos DB Data Explorer.](media/cosmos-db-document.png 'Cosmos DB Data Exporer')

Given the requirements provided by the customer, Cosmos DB is the best choice for ingesting data for this PoC. Cosmos DB allows for more flexible, and longer, TTL (message retention) than Event Hubs, which is capped at 7 days, or 4 weeks when you contact Microsoft to request the extra capacity. Another option for Event Hubs is to use Event Hubs Capture to simultaneously save ingested data to Blob Storage or Azure Data Lake Store for longer retention and cold storage. However, this will require additional development, including automatic clearing of the data after a period of time. In addition, Woodgrove Bank wanted to be able to easily query this data during the 60-day message retention period, from a database. This could also be accomplished through Azure Data Warehouse using Polybase to query the files, but that requires yet another service they may otherwise not need, as well as additional development, administration, and cost.

Finally, the requirement to synchronize/write the ingested data to multiple regions, which could grow at any time, makes Cosmos DB a more favorable choice. As you can see, there are more steps required to send data to additional regions using Event Hubs, since you have to provision new namespaces and Event Hub instances in each region. You would also have to account for all those instances on the consuming side, which we will not cover in this lab for sake of time. The ability to read/write to multiple regions by adding and removing them at will with no code or changes required is a great value that Cosmos DB adds. Plus, the fact that Cosmos DB will be used in this solution for serving batch-processed fraudulent data on a global scale means that Cosmos DB can be used to meet both the data ingest and delivery needs with no additional services, like Event Hubs, required.

We will continue the lab using Cosmos DB for data ingestion.

## Exercise 2: Understanding and preparing the transaction data at scale

Duration: 45 minutes

In this exercise, you will create connections from your Databricks workspace to ADLS Gen2 and Cosmos DB. Then, using Azure Databricks you will import and explore some of the historical raw transaction data provided by Woodgrove to gain a better understanding of the preparation that needs to be done prior to using the data for building and training a machine learning model. You will then use the connection to Cosmos DB from Databricks to read streaming transactions directly from the Cosmos DB Change Feed. Finally, you will write the incoming streaming transaction data into an Azure Databricks Delta table stored in your data lake.

### Task 1: Create a service principal for OAuth access to the ADLS Gen2 filesystem

As an added layer of security when accessing an ADLS Gen2 filesystem using Databricks you can use OAuth 2.0 for authentication. In this task, you will create an identity in Azure Active Directory (Azure AD) known as a service principal to facilitate the use of OAuth authentication.

> **IMPORTANT**: You must have permissions within your Azure subscription to create an App registration and service principal within Azure Active Directory to complete this task.

1. In the [Azure portal](https://portal.azure.com), select **Azure Active Directory** from the left-hand navigation menu, select **App registrations**, and then select **+ New application registration**.

   ![Register new app in Azure Active Directory](media/aad-app-registration.png 'Register new app in Azure Active Directory')

2. On the Create blade, enter the following:

   - **Name**: Enter a unique name, such as woodgrove-sp (this name must be unique within your Azure AD subscription, as indicated by a green check mark).
   - **Application type**: Select Web app / API.
   - **Sign-on URL**: Enter <https://woodgrove.com>.

   ![Create a new app registration](media/aad-app-create.png 'Create a new app registration')

3. Select **Create**.

4. To provide access your ADLS Gen2 account from Azure Databricks you will use secrets stored in your Azure Key Vault account to provide the credentials of your newly created service principal within Databricks. On the Registered app blade that appears, copy the **Application ID** and paste it into a text editor, such as Notepad, for use in an upcoming step to create a new secret in Key Vault.

   ![Copy the Registered App Application ID](media/registered-app-id.png 'Copy the Registered App Application ID')

5. Next, select **Settings** on the Registered app blade, and then select **Keys**.

   ![Open Keys blade for the Registered App](media/registered-app-settings-keys.png 'Open Keys blade for the Registered App')

6. On the Keys blade, you will create a new password by doing the following under Passwords:

   - **Description**: Enter a description, such as Databricks-ADLS.
   - **Expires**: Select a duration, such as **In 1 year**.

   ![Create new password](media/registered-app-create-key.png 'Create new password')

7. Select **Save**, and then copy the key displayed under **Value**, and paste it into a text editor, such as Notepad, for use in an upcoming step to create a new secret in Key Vault. 

>**Note**: This value will not be accessible once you navigate away from this screen, so make sure you copy it before leaving the Keys blade.

   ![Copy key value](media/registered-app-key-value.png 'Copy key value')

8. Navigate to your Azure Key Vault account in the Azure portal, then select **Secrets** under Settings on the left-hand menu. On the Secrets blade, select **+ Generate/Import** on the top toolbar.

   ![Secrets is highlighted on the left-hand menu, and Generate/Import is highlighted on the top toolbar of the Secrets blade.](media/key-vault-secrets.png 'Key Vault secrets blade')

9. On the Create a secret blade, enter the following:

   - **Upload options**: Select Manual.
   - **Name**: Enter "Woodgrove-SP-Client-ID".
   - **Value**: Paste the Application ID value you copied in an earlier step.

   ![The Create a secret blade is displayed, with the previously mentioned values entered into the appropriate fields.](media/key-vault-create-woodgrove-sp-client-id-secret.png 'Create a secret')

10. Select **Create**.

11. Select **+ Generate/Import** again on the top toolbar to create another secret.

12. On the Create a secret blade, enter the following:

    - **Upload options**: Select Manual.
    - **Name**: Enter "Woodgrove-SP-Client-Key".
    - **Value**: Paste the Password value you copied in an earlier step.

    ![The Create a secret blade is displayed, with the previously mentioned values entered into the appropriate fields.](media/key-vault-create-woodgrove-sp-client-key-secret.png 'Create a secret')

13. Select **Create**.

### Task 2: Grant ADLS Gen2 access permissions to the service principal

In this task, you will assign the required permissions to the service principal to grant access to your ADLS Gen2 account.

1. In the [Azure portal](https://portal.azure.com), navigate to the ADLS Gen2 account you created above, select **Access control (IAM)** from the left-hand menu, and then select **+ Add role assignment**.

   ![ADLS Gen2 Access Control blade](media/access-control.png 'ADLS Gen2 Access Control blade')

2. On the Add role assignment blade, set the following:

   - **Role**: Select **Storage Blob Data Contributor (Preview)** from the list.
   - **Assign access to**: Choose **Azure AD user, group, or service principal**.
   - **Select**: Enter the name of the service principal you created above, and then select it from the list.

   ![ADLS Gen2 Add role assignment](media/add-role-assignment.png 'ADLS Gen2 Add role assignment')

3. Select **Save**.

4. You will now see the service principal listed under **Role assignments** on the Access control (IAM) blade.

### Task 3: Retrieve your Azure AD tenant ID

To perform authentication using the service principal account in Databricks you will also need to provide your Azure AD Tenant ID.

1. To retrieve your tenant ID, select **Azure Active Directory** from the left-hand navigation menu in the Azure portal, then select **Properties**, and select the copy button next to **Directory ID** on the Directory Properties blade.

   ![Azure Active Directory is selected in the left-hand menu on the Azure portal, and the properties menu is selected. The Directory ID field is highlighted.](media/aad-tenant-id.png 'Retrieve Tenant ID')

2. Navigate to your Azure Key Vault account in the Azure portal, then select **Secrets** under Settings on the left-hand menu. On the Secrets blade, select **+ Generate/Import** on the top toolbar.

   ![Secrets is highlighted on the left-hand menu, and Generate/Import is highlighted on the top toolbar of the Secrets blade.](media/key-vault-secrets.png 'Key Vault secrets blade')

3. On the Create a secret blade, enter the following:

   - **Upload options**: Select Manual.
   - **Name**: Enter "Azure-Tenant-ID".
   - **Value**: Paste the Directory ID value you copied in an earlier step.

   ![The Create a secret blade is displayed, with the previously mentioned values entered into the appropriate fields.](media/key-vault-create-azure-tenant-id-secret.png 'Create a secret')

4. Select **Create**.

### Task 4: Install the Azure Cosmos DB Spark Connector and scikit-learn libraries in Databricks

In this task, you will install the [Azure Cosmos DB Spark Connector](https://github.com/Azure/azure-cosmosdb-spark) and scikit-learn libraries on your Databricks cluster. The Cosmos DB connector allows you to easily read from and write to Azure Cosmos DB via Apache Spark DataFrames.

1. Navigate to your Azure Databricks workspace in the [Azure portal](https://portal.azure.com/), and select **Launch Workspace** from the overview blade, signing into the workspace with your Azure credentials, if required.

   ![The Launch Workspace button is displayed on the Databricks Workspace Overview blade.](media/databricks-launch-workspace.png 'Launch Workspace')

2. Select **Workspace** from the left-hand menu, then select the drop down arrow next to **Shared** and select **Create** and **Library** from the context menus.

   ![The Workspace items is selected in the left-hand menu, and the shared workspace is highlighted. In the Shared workspace context menu, Create and Library are selected.](media/databrick-create-shared-library.png 'Create Shared Library')

3. On the Create Library page, select **Maven** under Library Source, and then select **Search Packages** next to the Coordinates text box.

   ![The Databricks Create Library dialog is displayed, with Maven selected under Library Source and the Search Packages link highlighted.](media/databricks-create-maven-library.png 'Create Library')

4. On the Search Packages dialog, select **Maven Central** from the source drop down, enter **azure-cosmosdb-spark** into the search box, and click **Select** next to Artifact Id `azure-cosmosdb-spark_2.4.0_2.11` release `1.3.5`.

   ![The Search Packages dialog is displayed, with Maven Central specified as the source and azure-cosmosdb-spark entered into the search box. The most recent version of the Cosmos DB Spark Connector is highlighted.](media/databricks-maven-search-packages.png)

5. Select **Create** to finish installing the library.

   ![The Create button is highlighted on the Create Library dialog.](media/databricks-create-library-cosmosdb-spark.png 'Create Library')

6. On the following screen, check the box for **Install automatically on all clusters**, and select **Confirm** when prompted.

   ![The Install automatically on all clusters box is checked and highlighted on the library dialog.](media/datbricks-install-library-on-all-clusters.png 'Install library on all clusters')

7. Select the Shared folder under your workspace again, and select **Create** and **Library** from the context menus.

8. In the Create Library dialog, select **PyPI** as the Library Source, and enter **scikit-learn==0.20.1** in the Package box, and then select **Create**

   ![The Create Library dialog is displayed, with PyPI highlighted under Library Source, and scikit-learn==0.20.1 entered into the Package text box.](media/databricks-create-library-scikit-learn.png 'Create Library')

9. On the following screen, **DO NOT** check to box for **Install automatically on all clusters**, and select **Confirm** when prompted. This library is only needed as a reference for the Job clusters. You will directly add this scikit-learn to the lab cluster in Exercise 3.

### Task 5: Explore historical transaction data with Azure Databricks and Spark

In this task, you will use an Azure Databricks notebook to download and explore historical transaction data.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

   ![In the Databricks workspace, Workspace is selected in the left-hand menu, Users is selected, and the user account is selected and highlighted.](media/databricks-user-workspace.png)

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 2** folder, and select the notebook named **1-Exploring-Historical-Transactions**.

   ![In the user's workspace, the 2-Exploring-Historical-Transactions notebook is selected under the Exercise 2 folder.](media/databricks-user-workspace-ex2-notebook1.png 'Notebooks in the user workspace')

3. In the **1-Exploring-Historical-Transactions** notebook, follow the instructions to complete the remaining steps of this task.

> **Note**: There will be a link at the bottom of each notebook in this exercise to move on to the notebook for the next task, so you will not need to jump back and forth between this document and the Databricks notebooks for this exercise.

### Task 6: Responding to streaming transactions using the Cosmos DB Change Feed and Spark Structured Streaming in Azure Databricks

In this task, you will use an Azure Databricks notebook to create a connection to your Cosmos DB instance from an Azure Databricks notebook, and query streaming data from the Cosmos DB Change Feed.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 2** folder, and select the notebook named **2-Cosmos-DB-Change-Feed**.

   ![In the user's workspace, the 3-Cosmos-DB-Change-Feed notebook is selected under the Exercise 2 folder.](media/databricks-user-workspace-ex2-notebook2.png 'Notebooks in the user workspace')

3. In the **2-Cosmos-DB-Change-Feed** notebook, follow the instructions to complete the remaining steps of this task.

## Exercise 3: Creating and evaluating fraud models

Duration: 45 minutes

In this exercise, you create and evaluate a fraud model that is used for real-time scoring of transactions as they occur at the web front-end. The goal is to block fraudulent transactions before they are processed. You will then create a model for detecting suspicious transactions, which gets executed during batch processing that will take place in Exercise 4. Finally, you will deploy the fraudulent transactions model and test it through HTTP REST calls, all within Databricks notebooks.

### Task 1: Install the AzureML and Scikit-Learn libraries in Databricks

In this task, you will install the required `AzureML` and `Scikit-Learn` libraries on your Databricks cluster. These libraries are used when training and deploying your machine learning models. It is important to install these in the order shown.

1. Navigate to your Azure Databricks workspace in the [Azure portal](https://portal.azure.com/), and select **Launch Workspace** from the overview blade, signing into the workspace with your Azure credentials, if required.

   ![The Launch Workspace button is displayed on the Databricks Workspace Overview blade.](media/databricks-launch-workspace.png 'Launch Workspace')

2. Select **Clusters** from the left-hand menu, then select your cluster in the list of interactive clusters.

   ![The cluster is listed and selected](media/databricks-select-cluster.png 'Select cluster')

3. Select the **Libraries** tab, which displays the list of libraries installed on the cluster. You should see the Azure Cosmos DB Spark connector installed. Select **Install New** above the list of libraries.

   ![The Libraries tab is selected, and the Install New button is highlighted](media/databricks-cluster-libraries.png 'Cluster')

4. In the dialog that appears, select **PyPI** as the **Library Source**. Enter `azureml-sdk[automl_databricks]` in the **Package** field, then select **Install**.

   ![The Install Library dialog is displayed, PyPI is selected as the Library Source, and the Package field is highlighted.](media/databricks-install-azureml.png 'Install Library')

5. Important: **Wait** until the status for the AzureML library shows **Installed**. This must be completed prior to installing Scikit-Learn.

   ![The Status of the AzureML library is Installed.](media/databricks-library-installed.png 'AzureML Library Installed')

6. Select **Install New** again.

7. In the dialog that appears, select **PyPi** as the **Library Source**. Enter `scikit-learn==0.20.1` in the **Package** field, then select **Install**.

   ![The Install Library dialog is displayed, PyPI is selected as the Library Source, and the Package field is highlighted.](media/databricks-install-scikit-learn.png 'Install Library')

8. After the Scikit-Learn library is installed, your list of libraries should look like the following:

   ![All three libraries are listed as installed.](media/databricks-libraries-installed.png 'Cluster Libraries')

### Task 2: Prepare and deploy scoring web service

In this task, you will use an Azure Databricks notebook to explore the transaction and account data. You will also do some data cleanup and create a feature engineering pipeline that applies these transformations each time data is passed to the model for scoring. Finally, you will train and deploy a machine learning model that detects fraudulent transactions.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

   ![In the Databricks workspace, Workspace is selected in the left-hand menu, Users is selected, and the user account is selected and highlighted.](media/databricks-user-workspace.png)

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 3** folder, and select the notebook named **1-Prepare-Scoring-Web-Service**.

   ![In the user's workspace, the 1-Prepare-Scoring-Web-Service notebook is selected under the Exercise 3 folder.](media/databricks-user-workspace-ex3-notebook1.png 'Notebooks in the user workspace')

3. In the **1-Prepare-Scoring-Web-Service** notebook, follow the instructions to complete the remaining steps of this task.

> **Note**: There will be a link at the bottom of each notebook in this exercise to move on to the notebook for the next task, so you will not need to jump back and forth between this document and the Databricks notebooks for this exercise.

### Task 3: Prepare batch scoring model

In this task, you will use an Azure Databricks notebook to prepare a model used to detect suspicious activity that will be used for batch scoring.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

   ![In the Databricks workspace, Workspace is selected in the left-hand menu, Users is selected, and the user account is selected and highlighted.](media/databricks-user-workspace.png)

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 3** folder, and select the notebook named **2-Prepare-Batch-Scoring-Model**.

   ![In the user's workspace, the 2-Prepare-Batch-Scoring-Model notebook is selected under the Exercise 3 folder.](media/databricks-user-workspace-ex3-notebook2.png 'Notebooks in the user workspace')

3. In the **2-Prepare-Batch-Scoring-Model** notebook, follow the instructions to complete the remaining steps of this task.

## Exercise 4: Scaling globally

When you set up Cosmos DB you enabled both geo-redundancy and multi-region writes, and in Exercise 1 you added more regions to your Cosmos DB instance.

![Map showing newly added regions for Cosmos DB.](media/replicate-data-globally-map.png 'Cosmos DB region map')

In this exercise, you will score the batch transaction data stored in Databricks Delta with your trained ML model, and write any transactions that are marked as "suspicious" to Cosmos DB via the Azure Cosmos DB Spark Connector. Cosmos with automatically distribute that data globally, using the [default consistency level](https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels). To learn more see [Global data distribution with Azure Cosmos DB - under the hood](https://docs.microsoft.com/en-us/azure/cosmos-db/global-dist-under-the-hood).

### Task 1: Distributing batch scored data globally using Cosmos DB

In this task, you will use an Azure Databricks notebook to batch stored the data stored in the `transactions` Databricks Delta table with your machine learning model. The scoring results will be written to a new `scored_transactions` Delta table, and any suspicious transactions will also be written back to Cosmos DB.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

   ![In the Databricks workspace, Workspace is selected in the left-hand menu, Users is selected, and the user account is selected and highlighted.](media/databricks-user-workspace.png)

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 4** folder, and select the notebook named **1-Distributing-Data-Globally**.

   ![In the user's workspace, the 1-Distributing-Data-Globally notebook is selected under the Exercise 2 folder.](media/databricks-user-workspace-ex4-notebook1.png 'Notebooks in the user workspace')

3. In the **1-Distributing-Data-Globally** notebook, follow the instructions to complete the remaining steps of this task.

### Task 2: Using an Azure Databricks job to batch score transactions on a schedule

In this task, you will create an Azure Databricks job, which will execute a notebook that performs batch scoring on transactions on an hourly schedule.

1. Navigate to your Databricks workspace, select **Jobs** from the left-hand menu, and then select **+ Create Job**.

   ![Jobs is highlighted in left-hand menu in Databricks, and the Create Job button is highlighted.](media/databricks-jobs.png 'Databricks Jobs')

2. On the untitled job screen, complete the following steps:

   - Enter a title, such as **Batch-Scoring-Job**.

   - Select the **Select Notebook** link next to Task, and on the Select Notebook dialog select **Users --> Your user account --> CosmosDbAdvancedAnalytics --> Exercise 4** and then select the `2-Batch-Scoring-Job` notebook and select **OK**.

     ![The Databricks Job Select Notebook dialog is displayed, with the 2-Batch-Scoring-Job notebook highlighted under CosmosDbAdvancedAnaltyics/Exercise 4.](media/databricks-job-select-notebook-ex4.png 'Select Notebook')

   - Select **Add** next to Dependent Libraries, navigate to the Shared folder, select the **azure-cosmosdb-spark** library and select **OK**.

   - Repeat the step above to add the **scikit-learn==0.20.1** library as well.

     ![The Add Dependent Library dialog is displayed with the azure-cosmosdb-spark and scikit-learn libraries highlighted within the Shared folder.](media/databricks-job-add-dependent-library.png 'Add Dependent Library')

   - Select **Edit** next to Cluster, and select the following:

     - **Databricks Runtime Version**: Runtime 5.1 (Scala 2.11, Spark 2.4.0)
     - **Python Version**: 3
     - **Worker Type**: Standard_D24_v2
     - **Workers**: 8
     - **Driver Type**: Same as worker
     - Expand Advanced and ensure that `spark.databricks.delta.preview.enabled true` is entered into the Spark Config box.

       ![The Configure Cluster dialog for the jbo is displayed, with the values specified above entered into the dialog.](media/databricks-job-cluster-config.png 'Configure Cluster')

   - Select **Confirm** to save the cluster configuration.

   - Select **Edit** next to Schedule, and on the Schedule Job dialog set the schedule to Every hour starting at a minute value that is close to the current time, so you can see it triggered in a reasonable amount of time. Select your time zone, and select **Confirm**.

   ![The Schedule Job dialog is displayed, with the schedule set to every hour starting at 00:55.](media/databricks-job-schedule-job.png 'Schedule Job dialog')

3. Your final job screen should look something like the following:

   ![Screen shot of the Transactions-Batch-Scoring job.](media/databricks-job-batch-scoring.png 'Transactions Batch Scoring job')

4. Select **< All Jobs** to return to the Jobs list when complete.

5. While waiting for your job to start, select **Workspace** from the left-hand menu, and navigate to the `2-Batch-Scoring-Job` notebook under the Exercise 4 folder.

6. Open the notebook, and take a few minutes to understand the steps that are being used to perform the batch scoring process. As you will see, they are almost identical to the steps you've gone through already in preparing and transforming the transaction data in the previous task.

7. You can monitor your job progress by selecting **Clusters** from the left-hand menu in Databricks, and then selecting **Job Run** for your Job Cluster. This will display the notebook, and you can view execution times and results within the notebook.

   ![The Databricks Clusters screen is displayed, with Job Run highlighted for the job cluster.](media/databricks-job-clusters-job-run.png 'Clusters dialog')

## Exercise 5: Reporting

Duration: 30 minutes

In this exercise, you create dashboards and reports in Power BI for business analysts to use, as well as within Azure Databricks for data scientists and analysts to query and visualize the data interactively.

### Task 1: Utilizing Power BI to summarize and visualize global fraud trends

In this task, you will use the JDBC URL for your Azure Databricks cluster to connect to from Power BI desktop. Then you will create reports and add them to a dashboard to summarize and visualize global fraud trends to gain more insight into the data.

1.  Navigate to your Azure Databricks workspace in the [Azure portal](https://portal.azure.com/), and select **Launch Workspace** from the overview blade, signing into the workspace with your Azure credentials, if required.

    ![The Launch Workspace button is displayed on the Databricks Workspace Overview blade.](media/databricks-launch-workspace.png 'Launch Workspace')

2.  Select **Clusters** from the left-hand menu, then select your cluster in the list of interactive clusters.

    ![The cluster is listed and selected](media/databricks-select-cluster.png 'Select cluster')

3.  Scroll down and expand the **Advanced Options** section, then select the **JDBC/ODBC** tab. Copy the first **JDBC URL** value.

    ![The cluster is displayed with the JDBC/ODBC tab selected, and the first JDBC URL value is highlighted.](media/databricks-jdbc.png 'JDBC/ODBC')

4.  Now, you need to modify the JDBC URL to construct the JDBC server address that you will use to set up your Spark cluster connection in Power BI Desktop.

    - In the JDBC URL:
      - Replace `jdbc:hive2` with `https`.
      - Remove everything in the path between the port number and `/sql`.
      - Remove the following string from the end of the URL: `;AuthMech=3;UID=token;PWD=<personal-access-token>`, retaining the components indicated by the highlights in the image below.

    ![Parsed Cluster JDBC URL](media/databricks-cluster-jdbc-url-parsed.png 'Parsed JDBC URL')

    - In our example, the server address would be: `https://eastus.azuredatabricks.net:443/sql/protocolv1/o/8433778235244215/0210-035431-quad242`

    - Copy your server address.

5.  Open Power BI Desktop, then select **Get data**.

    ![Select Get Data on the Power BI Desktop home screen.](media/power-bi-desktop-home.png 'Power BI Desktop')

6.  In the Get Data dialog, search for `spark`, then select **Spark** from the list of results.

    ![Search for Spark in the Get Data dialog, then select Spark from the list results.](media/power-bi-desktop-get-data.png 'Get Data')

7.  Enter the Databricks server address you created above into the Server field.

8.  Set the Protocol to HTTP.

9.  Select DirectQuery as the data connectivity mode, then select OK.

    ![The Spark connection dialog is displayed with the previously mentioned values.](media/power-bi-desktop-spark-connection.png 'Spark connection dialog')

    > Setting the data connectivity mode to DirectQuery lets you offload processing to Spark. This is ideal when you have a large volume of data or when you want near real-time analysis.

10. Before you can enter credentials on the next screen, you need to create an Access token in Azure Databricks. In your Databricks workspace, select the Account icon in the top right corner, then select User settings from the menu.

    ![Select User Settings from the Account menu.](media/databricks-user-settings.png 'User Settings menu')

11. On the User Settings page, select Generate New Token, enter "Power BI Desktop" in the comment, and select Generate.

    ![Enter Power BI Desktop as the comment then select Generate.](media/databricks-generate-token.png 'Generate New Token')

12. Copy the generated token, and save it as you will need it more than once below. **NOTE**: You will not be able to access the token in Databricks once you close the Generate token dialog, so be sure to save this value to a text editor or another location you can access during this lab.

13. Back in Power BI Desktop, enter "token" for the username, and paste the access token you copied from Databricks into the password field.

    ![Enter "token" as the user name and paste your generated token into the password field.](media/power-bi-desktop-spark-token.png 'Enter Spark credentials')

14. Select the `scored_transactions` and `percent_suspicious` tables, then select **Load**.

    ![Select the scored_transactions and percent_suspicious tables, then select Load.](media/power-bi-desktop-select-tables.png 'Select Tables')

15. After a few moments, you will be redirected to a blank report screen with the tables listed on the right-hand side. Select the **Donut chart** visualization on the right-hand menu under Visualizations and next to the list of tables and fields.

    ![Select Donut chart under the Visualizations menu on the right.](media/power-bi-donut-chart.png 'Donut Chart')

16. Expand the `scored_transactions` table, then drag `ipCountryCode` under **Legend**, and `transactionAmountUSD` under **Values**.

    ![Screenshot shows the donut chart settings.](media/power-bi-donut-chart-country-transaction.png 'Donut Chart settings')

17. Now select the **Format** tab for the donut chart and expand the **Legend** section underneath. Select **On** to turn on the legend, and select **Right** underneath **Position**. This turns on the legend for the chart and displays it to the right.

    ![Screenshot shows the donut chart format settings.](media/power-bi-donut-chart-country-transaction-format.png 'Donut Chart format')

18. Your donut chart should look similar to the following, displaying the US dollar amount of transactions by country code:

    ![Screenshot of the donut chart.](media/power-bi-donut-chart-country-transaction-display.png 'Donut Chart')

19. Select a blank area on the report to deselect the donut chart. Now select the **Treemap** visualization.

    ![The Treemap visualization is selected.](media/power-bi-treemap-visualization.png 'Treemap visualization')

20. Drag the `ipCountryCode` field from the `scored_transactions` table under **Group**, then drag `isSuspicious` under **Values**.

    ![Screenshot shows the treemap settings.](media/power-bi-treemap-country-suspicious-transactions.png 'Treemap settings')

21. The treemap should look similar to the following, displaying the number of suspicious transactions per country:

    ![Screenshot of the treemap.](media/power-bi-treemap-country-suspicious-transactions-display.png 'Treemap')

22. Select a blank area on the report to deselect the treemap. Now select the **Treemap** visualization once more to add a new treemap. Drag the `transactionAmountUSD` field from the `scored_transactions` table under **Group**, then drag `isSuspicious` under **Values**.

    ![Screenshot shows the treemap settings.](media/power-bi-treemap-suspicious-transactions.png 'Treemap settings')

23. The new treemap should look similar to the following, displaying the US dollar amounts that tend to have suspicious transactions, with the larger boxes representing higher suspicious transactions compared to smaller boxes:

    ![Screenshot of the treemap.](media/power-bi-treemap-suspicious-transactions-display.png 'Treemap')

24. Select a blank area on the report to deselect the treemap. Now select the **Donut chart** visualization. Drag the `localHour` field from the `scored_transactions` table under **Legend**, then drag `isSuspicious` under **Values**.

    ![Screenshot of the Donut chart settings.](media/power-bi-donut-chart-suspicious-hour.png 'Donut Chart settings')

25. The donut chart should look similar to the following, displaying which hours of the day tend to have a higher number of suspicious activity overall:

    ![Screenshot of the donut chart highlighted.](media/power-bi-donut-chart-suspicious-hour-display.png 'Donut Chart')

26. Select a blank area on the report to deselect the donut chart. Now select the **Map** visualization. Drag the `ipCountryCode` field from the `scored_transactions` table under **Location**, then drag `isSuspicious` under **Size**.

    ![Screenshot of the Map visualization settings.](media/power-bi-map.png 'Map settings')

27. The map should look similar to the following, showing circles of varying sizes on different regions of the map. The larger the circle, the more suspicious transactions there are in that region. You may also resize the charts to optimize your layout:

    ![Screenshot of the map visualization and the full report view.](media/power-bi-map-display.png 'Map')

28. Now add a new page to your report. Select the **+** button on the bottom-left next to **Page 1**. This will create a new blank report page to add a few more visualizations.

    ![Screenshot of the button you select to add a new page to the report.](media/power-bi-new-page.png 'New page button')

29. Select a blank area on the report, then select the **Donut chart** visualization. Drag the `cvvVerifyResult` field from the `scored_transactions` table under **Legend**, then drag `isSuspicious` under **Values**.

    ![Screenshot of the Donut chart settings.](media/power-bi-donut-chart-verify-result.png 'Donut Chart settings')

30. Now select the **Format** tab for the donut chart and expand the **Legend** section underneath. Select **On** to turn on the legend, and select **Right** underneath **Position**. This turns on the legend for the chart and displays it to the right.

    ![Screenshot shows the donut chart format settings.](media/power-bi-donut-chart-country-transaction-format.png 'Donut Chart format')

31. The donut chart should look similar to the following, displaying which CVV2 credit card verification codes correlate with the most suspicious transactions:

    ![Screenshot of the donut chart highlighted.](media/power-bi-donut-chart-verify-result-display.png 'Donut Chart')

    The CVV2 codes have the following meaning in the data set:

    | Code | Meaning                                                                                                                       |
    | ---- | ----------------------------------------------------------------------------------------------------------------------------- |
    | M    | CVV2 Match                                                                                                                    |
    | N    | CVV2 No Match                                                                                                                 |
    | P    | Not Processed                                                                                                                 |
    | S    | Issuer indicates that CVV2 data should be present on the card, but the merchant has indicated data is not present on the card |
    | U    | Issuer has not certified for CVV2 or Issuer has not provided Visa with the CVV2 encryption keys                               |

32. Select a blank area on the report, then select the **100% Stacked column chart** visualization. Drag the `isSuspicious` field from the `scored_transactions` table under **Axis**, then drag `digitalItemCount` under **Value**, and finally `physicalItemCount` under **Value** as well.

    ![Screenshot of the 100% Stacked Column Chart settings.](media/power-bi-stacked-column-chart.png '100% Stacked Column Chart settings')

33. The 100% stacked column chart should look similar to the following, displaying the percentage of the number of physical items and digital items purchased for transactions that were not suspicious (value of 0) in one column, and the percentage of the number of both types of items purchased with suspicious transactions (value of 1):

    ![Screenshot of the 100% stacked column chart.](media/power-bi-stacked-column-chart-display.png '100% Stacked Column Chart')

34. Select a blank area on the report, then select the **ArcGIS Maps for Power BI** visualization. If you are prompted to accept the terms for using this visualization, please do so now. Drag the `ipCountryCode` field from the `percent_suspicious` table under **Location**, then drag `SuspiciousTransactionCount` under **Color**.

    ![Screenshot of the ArcGIS Map settings.](media/power-bi-arcgis-map.png 'ArcGIS Map settings')

35. The ArcGIS Map should look similar to the following, displaying countries that have a higher number of suspicious transactions in darker colors than those with lower amounts:

    ![Screenshot of the ArcGIS Map.](media/power-bi-arcgis-map-display.png 'ArcGIS Map')

36. Select a blank area on the report, then select the **Donut chart** visualization. Drag the `ipCountryCode` field from the `percent_suspicious` table under **Legend**, then drag `PercentSuspicious` under **Values**.

    ![Screenshot of the donut chart settings.](media/power-bi-donut-chart-percent-suspicious.png 'Donut Chart settings')

37. The donut chart should look similar to the following, displaying the percent of suspicious transactions by country code:

    ![Screenshot of the donut chart.](media/power-bi-donut-chart-percent-suspicious-display.png 'Donut Chart')

You may save your chart to local disk. Once saved, you are able to upload the chart to the Power BI website, making it available online with all the charts and data connections intact.

### Task 2: Creating dashboards in Azure Databricks

In this task, you will use an Azure Databricks notebook to build a dashboard, for displaying visualization in Azure Databricks.

1. In your Databricks workspace, select **Workspace** from the left-hand menu, then select **Users** and your user account.

2. In your user workspace, select the **CosmosDbAdvancedAnalytics** folder, then select the **Exercise 5** folder, and select the notebook named **1-Databricks-Dashboards**.

   ![In the user's workspace, the 1-Databricks-Dashboards notebook is selected under the Exercise 2 folder.](media/databricks-user-workspace-ex5-notebook1.png 'Notebooks in the user workspace')

3. In the **1-Databricks-Dashboards** notebook, follow the instructions to complete the remaining steps of this task.

## After the hands-on lab

Duration: 10 mins

In this exercise, you will delete any Azure resources that were created in support of the lab. You should follow all steps provided after attending the Hands-on lab to ensure your account does not continue to be charged for lab resources.

### Task 1: Delete the resource group

1. Using the [Azure portal](https://portal.azure.com), navigate to the Resource group you used throughout this hands-on lab by selecting Resource groups in the left menu.
2. Search for the name of your research group, and select it from the list.
3. Select Delete in the command bar, and confirm the deletion by re-typing the Resource group name, and selecting Delete.

You should follow all steps provided _after_ attending the Hands-on lab.
