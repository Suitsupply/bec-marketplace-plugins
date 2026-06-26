using Common.ServiceInfo.Extensions;
using Common.ServiceInfo.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Template.Api.Messaging;
using Template.Api.Messaging.Interfaces;
using Template.Api.Messaging.Settings;
using Template.Api.Messaging.Validators;
using Template.App.Example.Services.Persons;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Example.Services.Vehicles;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.Infra.Example.Decorators;
using Template.Infra.Extensions;
using Template.Infra.Validators;

namespace Template.Api;

public partial class Program
{
    protected Program() { }

    private static async Task Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;

                services.AddServiceInfo(config.GetSection(nameof(ServiceSettings)));

                services.AddScoped<IPersonsService, PersonsService>();
                services.Decorate<IPersonsService, PersonsServiceCachingDecorator>();

                services.AddScoped<IVehiclesService, VehiclesService>();
                services.Decorate<IVehiclesService, VehiclesServiceCachingDecorator>();

                services.AddInfrastructure(config);

                // TODO: Move IServiceBusRetryScheduler and ServiceBusRetryScheduler to a shared chapter package.
                services.AddOptions<MessageRetryOptions>()
                    .Bind(config.GetSection(nameof(MessageRetryOptions)))
                    .ValidateOnStart();
                services.AddSingleton<IValidateOptions<MessageRetryOptions>>(_ => new FluentValidateOptions<MessageRetryOptions>(new MessageRetryOptionsValidator()));
                services.AddSingleton<IServiceBusRetryScheduler, ServiceBusRetryScheduler>();
            })
            .Build();

        await host.RunAsync();
    }
}