// YAGNI — You Aren't Gonna Need It.
// Do not build abstractions, configuration, or extension points for hypothetical future requirements.
// Extract when duplication is real (see 2_DRY.cs), not when you "might" need flexibility later.

// ✗ WRONG — plugin system for two webhook types
public interface IWebhookPluginRegistry
{
    void Register<T>(string eventType) where T : IWebhookHandler;
    IWebhookHandler Resolve(string eventType);
}

// ✓ CORRECT — one receiver service per webhook; add a third when it exists
public sealed class OrderCreatedReceiverService : ReceiverServiceBase<OrderCreatedWebhookRequest> { }
public sealed class OrderUpdatedReceiverService : ReceiverServiceBase<OrderUpdatedWebhookRequest> { }

// ✗ WRONG — abstract factory hierarchy before a second client variant exists
public abstract class ClientFactoryBase { }
public sealed class FooClientFactory : ClientFactoryBase { }

// ✓ CORRECT — register the one client you need today
// services.AddHttpClient<IFooClient, FooClient>();

// ✗ WRONG — optional settings for features not in scope
public record FeatureFlagsSettings
{
    public bool EnableExperimentalRefundFlow { get; init; }  // no ticket, no requirement
    public bool UseNewMapperV2 { get; init; }
}

// ✓ CORRECT — settings only for behaviour that exists and is configured per environment
public record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
    public string ClientId { get; init; } = default!;
}

// Rule of thumb: refactor to DRY/patterns when you feel the pain twice with a stable variation point.