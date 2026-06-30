using Common.ServiceBusRetryScheduler.Extensions;
using Common.ServiceBusRetryScheduler.Settings;
using Common.ServiceInfo.Extensions;
using Common.ServiceInfo.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Template.App.Example.Services.Persons;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Example.Services.Vehicles;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.Infra.Example.Decorators;
using Template.Infra.Extensions;

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

                services.AddServiceBusRetryScheduler(config.GetSection(nameof(MessageRetryOptions)));
            })
            .Build();

        await host.RunAsync();
    }
}