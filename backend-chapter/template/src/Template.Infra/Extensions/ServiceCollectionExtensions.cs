using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Template.App.Example.Clients.Interfaces;
using Template.Infra.Example.Clients.Swapi;
using Template.Infra.Example.Clients.Swapi.Settings;
using Template.Infra.Example.Clients.Swapi.Validators;
using Template.Infra.Example.Settings;
using Template.Infra.Example.Settings.Validators;
using Template.Infra.Validators;
using AzureServiceBusClient = Azure.Messaging.ServiceBus.ServiceBusClient;

namespace Template.Infra.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();

        services
            .AddOptions<CacheSettings>()
            .Bind(config.GetSection(nameof(CacheSettings)))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CacheSettings>>(_ => new FluentValidateOptions<CacheSettings>(new CacheSettingsValidator()));

        services
            .AddOptions<SwapiClientSettings>()
            .Bind(config.GetSection(nameof(SwapiClientSettings)))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SwapiClientSettings>>(_ => new FluentValidateOptions<SwapiClientSettings>(new SwapiClientSettingsValidator()));

        services
            .AddOptions<ServiceBusOptions>()
            .Bind(config.GetSection(nameof(ServiceBusOptions)))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ServiceBusOptions>>(_ => new FluentValidateOptions<ServiceBusOptions>(new ServiceBusOptionsValidator()));

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            return new AzureServiceBusClient(opts.StoreServiceBus.FullyQualifiedNamespace, new DefaultAzureCredential());
        });

        services.AddHttpClient<ISwapiClient, SwapiClient>();

        return services;
    }
}