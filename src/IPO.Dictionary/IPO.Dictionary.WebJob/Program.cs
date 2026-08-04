using Aspose.Words;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using IPO.Configuration;
using IPO.Dictionary.Data;
using IPO.Dictionary.Data.Interfaces;
using IPO.Dictionary.Gateways;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using spirePdf = Spire;

var builder = new HostBuilder();


builder.ConfigureLogging((context, b) =>
{
    b.Services.AddLogging();
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddConsole();
    b.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
    b.AddFilter("Azure.Messaging.ServiceBus", LogLevel.Warning);
});

builder.ConfigureAppConfiguration(configBuilder =>
{
    // Add IPO Azure Configuration - only added when Azure App Config is set
    configBuilder.AddIPOAzureAppConfigWithManagedIdentity();

    // Add Template replacement configuration provider to replace templated values
    configBuilder.AddTemplateConfiguration();
});

builder.ConfigureWebJobs((h, b) =>
{
#if DEBUG
    b.AddServiceBus(x => { x.TransportType = ServiceBusTransportType.AmqpWebSockets; });
#else
    b.AddServiceBus();
#endif

    b.Services.AddApplicationInsightsTelemetryWorkerService();

    b.AddAzureStorageQueues();

    var dbConnection = h.Configuration["DictionarySearchDbConnection"];
    b.Services.AddDbContext<IDictionarySearchDbContext, DictionarySearchDbContext>(options =>
        options.UseSqlServer(dbConnection!));

    b.Services.AddScoped<IDictionarySearchDbRepository, DictionarySearchDbRepository>();

    var validationSettings = new ValidationSettings()
    {
        AcceptedFileExtensions = h.Configuration["ValidationSettings:AcceptedFileExtensions"]!.ToUpperInvariant().Split(','),
        AcceptedFileMimeTypes = h.Configuration["ValidationSettings:AcceptedFileMimeTypes"]!.ToUpperInvariant().Split(','),
        SizeLimit = long.Parse(h.Configuration["ValidationSettings:SizeLimit"]!),
        PdfLibraryLicenseKey = h.Configuration["ValidationSettings:PdfLibraryLicenseKey"],
        WordLibraryLicenseKey = h.Configuration["ValidationSettings:WordLibraryLicenseKey"]!.Replace(",", "").Replace(Environment.NewLine, "").Trim()
    };
    Validator.ValidateObject(validationSettings, new ValidationContext(validationSettings), validateAllProperties: true);
    var settings = new Settings()
    {
        MaximumOperationTime = int.Parse(h.Configuration["MaximumOperationTime"]!),
        ValidationSettings = validationSettings
    };
    Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
    b.Services.AddScoped<Settings>(x => settings);

    if (!string.IsNullOrEmpty(settings.ValidationSettings.WordLibraryLicenseKey) && !settings.ValidationSettings.WordLibraryLicenseKey.Equals("test", StringComparison.InvariantCultureIgnoreCase))
    {
        var license = new License();
        license.SetLicense(new MemoryStream(Convert.FromBase64String(settings.ValidationSettings.WordLibraryLicenseKey)));
    }

    if (!string.IsNullOrEmpty(settings.ValidationSettings.PdfLibraryLicenseKey) && !settings.ValidationSettings.PdfLibraryLicenseKey.Equals("test", StringComparison.InvariantCultureIgnoreCase))
    {
        spirePdf.Pdf.License.LicenseProvider.SetLicense(new MemoryStream(Convert.FromBase64String(settings.ValidationSettings.PdfLibraryLicenseKey)));
    }

    b.Services.AddScoped<IDictionarySearchProcessor, DictionarySearchProcessor>();
    b.Services.AddScoped<IDocumentProcessor, PdfProcessor>();
    b.Services.AddScoped<IDocumentProcessor, DocxProcessor>();
    b.Services.AddScoped<IDocumentProcessor, OdtProcessor>();

    b.Services.AddScoped<IDictionarySearchManagementService>(x =>
    {
        var loggerFactory = x.GetService<ILoggerFactory>();
        var logger = loggerFactory!.CreateLogger<DictionarySearchManagementService>();
        var dictionarySearchProcessingService = x.GetService<IDictionarySearchProcessor>();
        var dictionarySearchRepositoryService = x.GetService<IDictionarySearchDbRepository>();

        ServiceBusSender serviceBusSender;
        ServiceBusClient serviceBusClient;
#if DEBUG
        serviceBusClient = new ServiceBusClient(h.Configuration["ServiceBusConnectionString"],
            new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
#else
           var credential = new Azure.Identity.DefaultAzureCredential();
                var serviceBusConnectionString = h.Configuration["ServiceBusConnectionString:fullyQualifiedNamespace"];
                serviceBusClient = new ServiceBusClient(serviceBusConnectionString, credential);
#endif
        serviceBusSender = serviceBusClient.CreateSender(h.Configuration["ServiceBusTopicName"]);

        var dictionarySearchTopicGateway = new DictionarySearchTopicGateway(serviceBusSender);

        var dictionarySearchBlobStorageGateway = new DictionarySearchBlobStorageGateway(new BlobServiceClient(h.Configuration["BlobStorageConnectionString"])
                                                                                        .GetBlobContainerClient(h.Configuration["BlobStorageContainerName"]));

        return new DictionarySearchManagementService(dictionarySearchBlobStorageGateway,
                                                    dictionarySearchTopicGateway,
                                                    dictionarySearchProcessingService!,
                                                    null!,
                                                    dictionarySearchRepositoryService!,
                                                    logger);
    });

});


#if DEBUG
builder.ConfigureAppConfiguration(options => options.AddJsonFile("appsettings.Development.json"));
#endif
builder.ConfigureAppConfiguration(configBuilder =>
{
    // Add IPO Azure Configuration - only added when Azure App Config is set
    configBuilder.AddIPOAzureAppConfigWithManagedIdentity();

    // Add Template replacement configuration provider to replace templated values
    configBuilder.AddTemplateConfiguration();
});
var host = builder.Build();
using (host)
{
    await host.RunAsync();
}

