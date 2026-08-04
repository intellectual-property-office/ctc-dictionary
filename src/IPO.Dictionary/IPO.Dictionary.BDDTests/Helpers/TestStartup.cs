using IPO.Dictionary.API;
using IPO.Dictionary.BDDTests.DictionarySearch;
using IPO.Dictionary.Data;
using IPO.Dictionary.Data.Interfaces;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace IPO.Dictionary.BDDTests.Helpers
{
    public class TestStartup : Startup
    {
        public TestStartup(IConfiguration configuration) : base(configuration)
        { }

        protected override void AddDatabase(IServiceCollection services)
        {
            var dictionarySearchDbContext = new DictionarySearchDbContext(new DbContextOptionsBuilder<DictionarySearchDbContext>()
                         .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                         .Options);

            services.AddSingleton<DictionarySearchDbContext>(x => dictionarySearchDbContext);
            services.AddSingleton<IDictionarySearchDbContext>(x => dictionarySearchDbContext);
        }
        protected override void AddDictionarySearchProcessors(IServiceCollection services)
        {
            services.AddScoped<IDictionarySearchProcessor, MockedDictionarySearchProcessor>();
        }
        protected override void AddDictionarySearchValidators(IServiceCollection services)
        {
            services.AddScoped<IDictionarySearchValidator, MockedDictionarySearchValidator>();
        }
        protected override void AddDictionarySearchServices(IServiceCollection services)
        {
            services.AddScoped<IDictionarySearchTopicGateway, MockedDictionarySearchTopicGateway>();
            services.AddScoped<IDictionarySearchBlobStorageGateway, MockedDictionarySearchBlobStorageGateway>();
            services.AddScoped<IDictionarySearchManagementService, DictionarySearchManagementService>();
        }
        protected override void MigrateDatabase(IApplicationBuilder app)
        { }

        protected override void SeedDatabase(IApplicationBuilder app, string contentRootPath)
        { 
        }

        public static TestServer GetTestServer()
        {
            var hostBuilder = new HostBuilder()
          .ConfigureWebHost(webHost =>
          {
              webHost
                 .UseTestServer()
                 .UseStartup<TestStartup>();
          });
            var host = hostBuilder.Start();
            return host.GetTestServer();
        }
    }
}
