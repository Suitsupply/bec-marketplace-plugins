using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Template.Api;
using Template.Api.Example.Functions.v1.Persons;
using Template.Api.Example.Functions.v1.Vehicles;
using Common.ServiceBusRetryScheduler.Interfaces;
using Template.App.Example.Clients.Interfaces;

namespace Template.ComponentTests.Support;

public class ApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ISwapiClient> SwapiClient { get; } = new();
    public Mock<IServiceBusRetryScheduler> RetryScheduler { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", AppContext.BaseDirectory);

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Functions:Worker:HostEndpoint"] = "http://127.0.0.1:9999",
                ["ServiceSettings:ServiceName"] = "Template",
                ["SwapiClientSettings:BaseUrl"] = "https://swapi.info/api/",
                ["CacheSettings:PersonEntryLifetime"] = "00:02:00",
                ["CacheSettings:VehicleEntryLifetime"] = "00:05:00",
                ["ServiceBusOptions:StoreServiceBus:UpdatePersonQueueName"] = "update-person",
                ["ServiceBusOptions:StoreServiceBus:FullyQualifiedNamespace"] = "test.servicebus.windows.net",
                ["MessageRetryOptions:MaxDeliveryCount"] = "3",
                ["MessageRetryOptions:RetryDelay"] = "00:00:05",
                ["MessageRetryOptions:BackoffMultiplier"] = "1",
            }));

        builder.ConfigureServices(services =>
        {
            var workerServices = services
                .Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType?.Assembly.GetName().Name?.StartsWith("Microsoft.Azure.Functions") == true)
                .ToList();
            foreach (var descriptor in workerServices)
            {
                services.Remove(descriptor);
            }

            RemoveAll<ISwapiClient>(services);
            RemoveAll<ServiceBusClient>(services);
            RemoveAll<IServiceBusRetryScheduler>(services);
            services.AddSingleton(SwapiClient.Object);
            services.AddSingleton(RetryScheduler.Object);

            services.AddControllers();
            services.AddScoped<GetPersonFunction>();
            services.AddScoped<UpdatePersonFunction>();
            services.AddScoped<GetVehicleFunction>();
            services.AddScoped<CreateVehicleFunction>();
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/api/person/{id:int}", async ctx =>
                {
                    var handler = ctx.RequestServices.GetRequiredService<GetPersonFunction>();
                    var id = int.Parse(ctx.Request.RouteValues["id"]?.ToString() ?? "0");
                    var result = await handler.GetPersonAsync(ctx.Request, id, ctx.RequestAborted);
                    await result.ExecuteResultAsync(new ActionContext(ctx, new RouteData(), new ActionDescriptor()));
                });

                endpoints.MapGet("/api/vehicles/{id:int}", async ctx =>
                {
                    var handler = ctx.RequestServices.GetRequiredService<GetVehicleFunction>();
                    var id = int.Parse(ctx.Request.RouteValues["id"]?.ToString() ?? "0");
                    var result = await handler.GetVehicleAsync(ctx.Request, id, ctx.RequestAborted);
                    await result.ExecuteResultAsync(new ActionContext(ctx, new RouteData(), new ActionDescriptor()));
                });

                endpoints.MapPost(
                    "/api/person/update/debug",
                    Route<UpdatePersonFunction>((fn, req, ct) => fn.UpdatePersonMessageDebugAsync(req, ct)));

                endpoints.MapPost(
                    "/api/vehicles",
                    Route<CreateVehicleFunction>((fn, req, ct) => fn.CreateVehicleAsync(req, ct)));
            });
        });
    }

    private static RequestDelegate Route<THandler>(Func<THandler, HttpRequest, CancellationToken, Task<IActionResult>> invoke)
        where THandler : class =>
        async ctx =>
        {
            var handler = ctx.RequestServices.GetRequiredService<THandler>();
            var result = await invoke(handler, ctx.Request, ctx.RequestAborted);
            await result.ExecuteResultAsync(new ActionContext(ctx, new RouteData(), new ActionDescriptor()));
        };

    private static void RemoveAll<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
