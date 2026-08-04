using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;
using Aspose.Words;
using Azure.Core;
#if !DEBUG
using Azure.Identity;
#endif
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using IPO.Common.API;
using IPO.Common.Infrastructure;
using IPO.CTC.HealthChecks;
using IPO.Dictionary.Data;
using IPO.Dictionary.Data.Interfaces;
using IPO.Dictionary.Gateways;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using spirePdf = Spire;

namespace IPO.Dictionary.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Helper = new IPOStartupHelper("IPO.Dictionary.API", "version");
        }

        public IConfiguration Configuration { get; }

        public IPOStartupHelper Helper { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Helper.AddIPOServicesConfiguration(services, mvcBuilderAction: x => x.AddJsonOptions(options =>
                                                        options.JsonSerializerOptions.Converters.Add(
                                                            new JsonStringEnumConverter())));

            AddHealthChecks(services);

            services.AddSingleton(typeof(ILogger), typeof(Logger<Startup>));
            services.AddSwaggerGen(c =>
            {
                c.ExampleFilters();
                c.SchemaGeneratorOptions.CustomTypeMappings.Add(typeof(IFormFile)
                    , () => new OpenApiSchema()
                    {
                        Type = "file",
                        Format = "binary"
                    });
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dictionary", Version = "v1" });
                c.EnableAnnotations();
            });

            services.AddIPOErrorAwareScoped<IDictionarySearchDbRepository, DictionarySearchDbRepository>("E001");
            AddDatabase(services);
            AddDictionarySearchValidators(services);
            AddDictionarySearchProcessors(services);
            AddDictionarySearchServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRewriter(new RewriteOptions().Add(RewriteRules.RewriteAlwaysOn));

            MigrateDatabase(app);
            SeedDatabase(app, env.ContentRootPath);
            Helper.UseIPOConfigurations(app, env);

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        protected virtual void AddHealthChecks(IServiceCollection services)
        {
            services.AddHealthChecks().AddTypeActivatedCheck<SQLHealthCheck>(
                name: "Dictionary Database Health Check",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { HealthTags.Ready },
                args: new object[]
                {
                    Configuration, "DictionarySearchDbConnection"
                }
            );

            services.AddHealthChecks().AddTypeActivatedCheck<AzureBlobStorageConnectionHealthCheck>(
               name: "Dictionary BLOB Store Health Check",
               failureStatus: HealthStatus.Unhealthy,
               tags: new[] { HealthTags.Ready },
               args: new object[]
               {
                   Configuration,
                   "BlobStorageConnectionString"
               }
            );

#if DEBUG

            services.AddHealthChecks().AddTypeActivatedCheck<ServiceBusTopicHealthCheck>(
               name: "Dictionary Service Bus Health Check",
               failureStatus: HealthStatus.Unhealthy,
               tags: new[] { HealthTags.Ready },
               args: new object[]
               {
                   Configuration,
                   "ServiceBusConnectionString",
                   "ServiceBusTopicName"
               }
            );

#else
//here too
            var credential = new DefaultAzureCredential();

            services.AddHealthChecks().AddTypeActivatedCheck<ServiceBusTopicHealthCheck>(
               name: "Dictionary Service Bus Health Check",
               failureStatus: HealthStatus.Unhealthy,
               tags: new[] { HealthTags.Ready },
               args: new object[]
               {
                   Configuration,
                   "ServiceBusConnectionString:fullyQualifiedNamespace",
                   "ServiceBusTopicName",
                   credential
               }
            );

#endif
        }

        protected virtual void AddSwaggerExamplesForType<T>(IServiceCollection services)
        {
            services.AddSwaggerExamplesFromAssemblyOf<T>();
        }

        protected virtual void AddDatabase(IServiceCollection services)
        {
            var dbConnection = Configuration["DictionarySearchDbConnection"];

            services.AddIPOErrorAwareDbContext<IDictionarySearchDbContext, DictionarySearchDbContext>("E002", options =>
                options.UseSqlServer(dbConnection!));
        }

        protected virtual void AddDictionarySearchProcessors(IServiceCollection services)
        {
            services.AddIPOErrorAwareScoped<IDictionarySearchProcessor, DictionarySearchProcessor>("E004");
            services.AddIPOErrorAwareScoped<IDocumentProcessor, PdfProcessor>("E004-1");
            services.AddIPOErrorAwareScoped<IDocumentProcessor, DocxProcessor>("E004-2");
            services.AddIPOErrorAwareScoped<IDocumentProcessor, OdtProcessor>("E004-3");
        }

        protected virtual void AddDictionarySearchValidators(IServiceCollection services)
        {
            services.AddIPOErrorAwareScoped<IDictionarySearchValidator, DictionarySearchValidator>("E003");
        }

        protected virtual void AddDictionarySearchServices(IServiceCollection services)
        {
            AddSwaggerExamplesForType<DictionarySearchResultSwaggerExamples>(services);


            var validationSettings = new ValidationSettings()
            {
                AcceptedFileExtensions = Configuration["ValidationSettings:AcceptedFileExtensions"]!.ToUpperInvariant().Split(','),
                AcceptedFileMimeTypes = Configuration["ValidationSettings:AcceptedFileMimeTypes"]!.ToUpperInvariant().Split(','),
                SizeLimit = long.Parse(Configuration["ValidationSettings:SizeLimit"]!),
                PdfLibraryLicenseKey = Configuration["ValidationSettings:PdfLibraryLicenseKey"],
                WordLibraryLicenseKey = Configuration["ValidationSettings:WordLibraryLicenseKey"]!.Replace(",", "").Replace(Environment.NewLine, "").Trim()
            };
            Validator.ValidateObject(validationSettings, new ValidationContext(validationSettings), validateAllProperties: true);
            var settings = new Settings()
            {
                MaximumOperationTime = int.Parse(Configuration["MaximumOperationTime"]!),
                ValidationSettings = validationSettings
            };
            Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
            services.AddScoped<Settings>(x => settings);


            AddAsposeLicense(settings.ValidationSettings.WordLibraryLicenseKey);
            AddSpireLicense(settings.ValidationSettings.PdfLibraryLicenseKey!);

            Error.Add<DictionarySearchTopicGateway>("E005");
            Error.Add<DictionarySearchBlobStorageGateway>("E006");

            services.AddIPOErrorAwareScoped<IDictionarySearchManagementService>(x =>
            {
                var loggerFactory = x.GetService<ILoggerFactory>();
                var logger = loggerFactory!.CreateLogger<DictionarySearchManagementService>();

                var dictionarySearchValidationService = x.GetService<IDictionarySearchValidator>();
                var dictionarySearchProcessingService = x.GetService<IDictionarySearchProcessor>();
                var dictionarySearchRepositoryService = x.GetService<IDictionarySearchDbRepository>();

                ServiceBusSender serviceBusSender;
                ServiceBusClient serviceBusClient;
#if DEBUG
                serviceBusClient = new ServiceBusClient(Configuration["ServiceBusConnectionString"],
                    new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
#else
                var credential = new DefaultAzureCredential();
                var serviceBusConnectionString = Configuration["ServiceBusConnectionString:fullyQualifiedNamespace"];
                serviceBusClient = new ServiceBusClient(serviceBusConnectionString, credential);
#endif
                serviceBusSender = serviceBusClient.CreateSender(Configuration["ServiceBusTopicName"]);

                var dictionarySearchTopicGateway = IPOStartupExtensions.CreateProxy<IDictionarySearchTopicGateway, DictionarySearchTopicGateway>(
                    () => new DictionarySearchTopicGateway(serviceBusSender), "E005");

                var dictionarySearchBlobStorageGateway = IPOStartupExtensions.CreateProxy<IDictionarySearchBlobStorageGateway, DictionarySearchBlobStorageGateway>(
                    () => new DictionarySearchBlobStorageGateway(new BlobServiceClient(Configuration["BlobStorageConnectionString"])
                                                                                   .GetBlobContainerClient(Configuration["BlobStorageContainerName"])), "E006");

                return new DictionarySearchManagementService(dictionarySearchBlobStorageGateway,
                                                            dictionarySearchTopicGateway,
                                                            dictionarySearchProcessingService!,
                                                            dictionarySearchValidationService!,
                                                            dictionarySearchRepositoryService!,
                                                            logger);
            }, Error.Create<DictionarySearchManagementService>("E007"));

        }

        protected virtual void AddAsposeLicense(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey) || licenseKey.Equals("test", StringComparison.InvariantCultureIgnoreCase))
                return;

            var license = new License();
            license.SetLicense(new MemoryStream(Convert.FromBase64String(licenseKey)));
        }

        protected virtual void AddSpireLicense(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey) || licenseKey.Equals("test", StringComparison.InvariantCultureIgnoreCase))
                return;

            spirePdf.Pdf.License.LicenseProvider.SetLicense(new MemoryStream(Convert.FromBase64String(licenseKey)));

        }

        protected virtual void MigrateDatabase(IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DictionarySearchDbContext>();
            dbContext!.Database.Migrate();
        }

        protected virtual void SeedDatabase(IApplicationBuilder app, string contentRootPath)
        {
            var scope = app.ApplicationServices.CreateScope();
            var dbRepository = scope.ServiceProvider.GetService<DictionarySearchDbRepository>();
            dbRepository!.SeedDictionarySearchData(dbRepository.GetDictionarySeedDataDirectory(contentRootPath));
        }
    }
}