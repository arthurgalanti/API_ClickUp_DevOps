using System.IO;
using System.Security.Authentication;
using apiClickupDevops.Models.Mapper;
using apiClickupDevops.Services;
using AutoMapper;
using ClickupDevops;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

[assembly: FunctionsStartup(typeof(Startup))]

namespace ClickupDevops
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(config["COSMOSDB_STRING"]));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

            builder.Services.AddSingleton((s) => new MongoClient(settings));
            builder.Services.AddScoped<RelationService>();
            builder.Services.AddScoped<DevopsService>();
            builder.Services.AddScoped<ClickupService>();

            var mapperConfig = new MapperConfiguration(cfg =>{
                cfg.AddProfile<CardMapper>();
            });

            builder.Services.AddSingleton(mapperConfig.CreateMapper());
        }
    }
}