# Dictionary Microservice

# About
The Dictionary Microservice provides a set of API endpoints to enable internal IPO customers to upload a file, and check whether that file contains any words from a predefined set of words contained in a dictionary. The result is a boolean value and the resulting matched word if found (first match only).

# Installation guide
### System Requirements
- IDE capable of running .NET 10 or above i.e. Visual Studio

### Prerequisites
- Azure Storage Account and Blob Container.
- Azure Messaging Service Bus, Topic and Subscriptions.
- SQL database.
- Aspose licence key (optional - can use test key).
- Spire licence key (optional - can use test key).

### Installation instructions
1. Clone the repository to your local machine.

2. Open the 'IPO.Dictionary.sln' solution file in Visual Studio.

3. In the Web API project add a local development settings file called 'appsettings.Development.json'. Copy and input the contents of the below Configuration file. Update the 'appsettings.Development.json' file with your local setup values for the following;

&emsp;&emsp;&emsp;'DictionarySearchDbConnection' = SQL 'DictionarySearchServiceRecords' database connectionString.

&emsp;&emsp;&emsp;'BlobStorageConnectionString' = Azure storage connectionString.

&emsp;&emsp;&emsp;'ServiceBusConnectionString' = Azure service bus connectionString.

&emsp;&emsp;&emsp;'PdfLibraryLicenseKey' = Spire licence key in base64.

&emsp;&emsp;&emsp;'WordLibraryLicenseKey' = Aspose licence key in base64.

4. In the WebJob project add a local development settings file called 'appsettings.Development.json'. Copy and input the contents of the below Configuration file. Set any values specific to your setup. Update the file with your local setup values for the following;

&emsp;&emsp;&emsp;'DictionarySearchDbConnection' = SQL 'DictionarySearchServiceRecords' database connectionString.

&emsp;&emsp;&emsp;'BlobStorageConnectionString' = Azure storage connectionString.

&emsp;&emsp;&emsp;'ServiceBusConnectionString' = Azure service bus connectionString.

&emsp;&emsp;&emsp;'PdfLibraryLicenseKey' = Spire licence key in base64.

&emsp;&emsp;&emsp;'WordLibraryLicenseKey' = Aspose licence key in base64.

5. In visual studio set multiple startup projects; to include the WebApi and WebJob.

6. Build the solution and run in Debug mode. 
**Note** on first run the entity framework will create the 'DictionarySearchServiceRecords' SQL database and tables.

7. A console window will open for the webjob. The swagger page will launch in your default browser ready to test the endpoints. A second command window will launch for the Web API, in which you will see the Console output.

8. From the swagger page you can test the endpoints. Any data changes you make can be reviewed in your SQL database and Blob storage container.

## Configuration files
- When deployed to Azure and not local development, Keys within ConnectionStrings need to be configured within the Azure Configuration section.

IPO.Dictionary.API
```JSON
{
	  "IpoLogLevel": "Error",
	  "AllowedHosts": "*",
	  "DictionarySearchDbConnection": "<Your SQL DictionarySearchServiceRecords ConnectionString>",
	  "BlobStorageConnectionString": "<Your Blob storage ConnectionString>",
	  "BlobStorageContainerName": "dictionaryservice",
	  "ServiceBusConnectionString": "<Your Service Bus ConnectionString>",
	  "ServiceBusTopicName": "dictionarysearch",
	  "MaximumOperationTime": 2400,
	  "ValidationSettings": {
    	"AcceptedFileExtensions": ".ODT,.DOCX,.PDF",
    	"AcceptedFileMimeTypes": "application/vnd.oasis.opendocument.text,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/pdf",
    	"SizeLimit": "1073741824",
    	"PdfLibraryLicenseKey": "<your licence key>",
    	"WordLibraryLicenseKey": "<your licence key>"
  	  }
}
```

IPO.Dictionary.WebJob
```JSON
{
	  "IsEncrypted": false,
	
	  "AzureWebJobsStorage": "UseDevelopmentStorage=true",
	  "FUNCTIONS_WORKER_RUNTIME": "dotnet",
	
	  "DictionarySearchDbConnection": "<Your SQL DictionarySearchServiceRecords ConnectionString>",
	  "BlobStorageConnectionString": "<Your Blob storage ConnectionString>",
	  "BlobStorageContainerName": "dictionaryservice",
	  "ServiceBusConnectionString": "<Your Service Bus ConnectionString>",
	  "ServiceBusTopicName": "dictionarysearch",
  	  "ServiceBusSubscriptionName": "SearchSubscription",
  	  "MaximumOperationTime": 2400,
  	  "ValidationSettings": {
	    "AcceptedFileExtensions": ".ODT,.DOCX,.PDF",
	    "AcceptedFileMimeTypes": "application/vnd.oasis.opendocument.text,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/pdf",
	    "SizeLimit": "1073741824",
	    "PdfLibraryLicenseKey": "<your licence key>",
	    "WordLibraryLicenseKey": "<your licence key>"
  	  }
}
```