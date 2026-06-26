// > Example **3** — `ApplicationFactory`, `Hooks`, and `ScenarioContextKeys`
// Pattern from shopifyintegration ComponentTests/Support — generic placeholders below.

// --- Support/ApplicationFactory.cs ---

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
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
using {ServiceName}.Api;
using {ServiceName}.Api.Functions.Person;
using {ServiceName}.Api.Messaging;
using {ServiceName}.App.Clients.Interfaces;

namespace {ServiceName}.ComponentTests.Support;

public class ApplicationFactory : WebApplicationFactory<Program>
{
    // Public mocks — step definitions access these via Factory from FeatureContext
    public Mock<IEventBlobStorageClient> BlobStorageClient { get; } = new();
    public Mock<IStoreServiceBusClient> ServiceBusClient { get; } = new();
    public Mock<IFooClient> FooClient { get; } = new();
    public Mock<IBarApiClient> BarApiClient { get; } = new();
    public Mock<IOutboundPublisher> OutboundPublisher { get; } = new();
    public Mock<IServiceBusRetryScheduler> RetryScheduler { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY", AppContext.BaseDirectory);

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Functions:Worker:HostEndpoint"] = "http://127.0.0.1:9999",
                ["ServiceSettings:ServiceName"] = "{ServiceName}",
                ["MessageRetryOptions:MaxDeliveryCount"] = "3",
                ["MessageRetryOptions:RetryDelay"] = "00:00:05",
                ["MessageRetryOptions:BackoffMultiplier"] = "1",
                ["ServiceBusOptions:StoreServiceBus:FooCreatedQueueName"] = "foo-created",
                ["ServiceBusOptions:StoreServiceBus:FullyQualifiedNamespace"] = "test.servicebus.windows.net",
                ["FooClientSettings:BaseUrl"] = "https://localhost",
                ["FooClientSettings:ClientId"] = "test-client-id",
                ["BarApiSettings:BaseUrl"] = "https://localhost",
                ["OutboundPublisherSettings:TopicId"] = "projects/test/topics/outbound-foo",
                // ... copy remaining keys from appsettings — every binding your host needs at startup
            }));

        builder.ConfigureServices(services =>
        {
            // Prevent the Functions gRPC worker from connecting to a non-existent host
            var workerServices = services
                .Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType?.Assembly.GetName().Name?.StartsWith("Microsoft.Azure.Functions") == true)
                .ToList();
            foreach (var descriptor in workerServices)
            {
                services.Remove(descriptor);
            }

            // Remove Azure SDK singletons and real Infra client implementations
            RemoveAll<BlobServiceClient>(services);
            RemoveAll<ServiceBusClient>(services);
            RemoveAll<ServiceBusAdministrationClient>(services);
            RemoveAll<IEventBlobStorageClient>(services);
            RemoveAll<IStoreServiceBusClient>(services);
            RemoveAll<IFooClient>(services);
            RemoveAll<IBarApiClient>(services);
            RemoveAll<IOutboundPublisher>(services);
            RemoveAll<IServiceBusRetryScheduler>(services);

            services.AddSingleton(BlobStorageClient.Object);
            services.AddSingleton(ServiceBusClient.Object);
            services.AddSingleton(FooClient.Object);
            services.AddSingleton(BarApiClient.Object);
            services.AddSingleton(OutboundPublisher.Object);
            services.AddSingleton(RetryScheduler.Object);

            // Api/Infra mappers stay registered from Program — do not mock I*Mapper

            // Needed so IActionResult returns are executed correctly
            services.AddControllers();

            // Register function classes so ConfigureWebHost route handlers can resolve them
            services.AddScoped<FooCreatedReceiver>();
            services.AddScoped<BarUpdatedReceiver>();
            services.AddScoped<FooCreatedProcessor>();
            services.AddScoped<OutboundEventsBackupReceiver>();
            services.AddScoped<GetFooFunction>();
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Replace the Functions middleware pipeline (requires x-ms-invocation-id)
        // with plain ASP.NET Core routing that invokes function methods directly.
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // HTTP receivers
                endpoints.MapPost("/api/foo/created", Route<FooCreatedReceiver>((fn, req, ct) => fn.ProcessWebhookAsync(req, ct)));
                endpoints.MapPost("/api/bar/updated", Route<BarUpdatedReceiver>((fn, req, ct) => fn.ProcessWebhookAsync(req, ct)));

                // Processor debug routes (programmatic component scenarios)
                endpoints.MapPost("/api/foo/created/process/debug", Route<FooCreatedProcessor>((fn, req, ct) => fn.ProcessDebugAsync(req, ct)));

                // Query endpoints
                endpoints.MapGet("/api/foo/{resourceId}", async ctx =>
                {
                    var handler = ctx.RequestServices.GetRequiredService<GetFooFunction>();
                    var resourceId = ctx.Request.RouteValues["resourceId"]?.ToString() ?? string.Empty;
                    var result = await handler.GetAsync(ctx.Request, resourceId, ctx.RequestAborted);
                    await result.ExecuteResultAsync(new ActionContext(ctx, new RouteData(), new ActionDescriptor()));
                });
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

// --- Support/Hooks.cs ---

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Reqnroll;
using {ServiceName}.Api.Messaging;
using {ServiceName}.App.Models.Outbound;

namespace {ServiceName}.ComponentTests.Support;

[Binding]
public sealed class Hooks(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        featureContext.Set(new ApplicationFactory(), ScenarioContextKeys.Factory);
    }

    [AfterFeature]
    public static void AfterFeature(FeatureContext featureContext)
    {
        if (featureContext.TryGetValue(ScenarioContextKeys.Factory, out ApplicationFactory factory))
        {
            factory.Dispose();
        }
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var factory = featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

        factory.BlobStorageClient.Reset();
        factory.ServiceBusClient.Reset();
        factory.FooClient.Reset();
        factory.BarApiClient.Reset();
        factory.OutboundPublisher.Reset();
        factory.RetryScheduler.Reset();

        factory.RetryScheduler
            .Setup(s => s.RescheduleOrDeadLetterAsync(
                It.IsAny<ServiceBusMessageActions>(),
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RetryOutcome.Rescheduled);

        scenarioContext.Set(factory.CreateClient(), ScenarioContextKeys.HttpClient);
        scenarioContext.Set(new List<OutboundFooEvent>(), ScenarioContextKeys.CapturedOutboundEvents);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (scenarioContext.TryGetValue(ScenarioContextKeys.HttpClient, out HttpClient client))
        {
            client.Dispose();
        }
    }
}

// --- Support/ScenarioContextKeys.cs ---

namespace {ServiceName}.ComponentTests.Support;

internal static class ScenarioContextKeys
{
    internal const string Factory = "Factory";
    internal const string HttpClient = "HttpClient";
    internal const string Response = "Response";
    internal const string RequestBody = "RequestBody";
    internal const string CurrentResourceId = "CurrentResourceId";
    internal const string CapturedOutboundEvents = "CapturedOutboundEvents";
    internal const string ServiceBusMessageActions = "ServiceBusMessageActions";
    internal const string ThrownException = "ThrownException";
}

// --- Adding a new mock (checklist) ---
// 1. public Mock<INewClient> NewClient { get; } = new(); on ApplicationFactory
// 2. RemoveAll<INewClient>(services) + services.AddSingleton(NewClient.Object) in ConfigureServices
// 3. factory.NewClient.Reset() in Hooks.BeforeScenario
// 4. MapPost/MapGet in ConfigureWebHost when a new HTTP function is added
