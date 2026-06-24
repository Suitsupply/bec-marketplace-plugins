// Composition over inheritance — prefer injecting collaborators over deep class hierarchies.
// Chapter: primary-constructor DI, shallow inheritance (base + hooks), handlers composed via factory.

// ✓ CORRECT — processor composes pipeline, mapper, publisher (no mega base class)
public sealed class FooProcessorService(
    IFooEnrichmentPipeline enrichmentPipeline,
    IOutboundFooMapper mapper,
    IMaoPublisher publisher,
    IServiceBusClient serviceBusClient)
{
    public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken)
    {
        var envelope = Deserialize(rawJson);
        await enrichmentPipeline.RunAsync(envelope, cancellationToken);
        var payload = mapper.Map(envelope);
        if (payload is null) return;
        await publisher.PublishAsync(payload, cancellationToken);
    }

    private static EnrichmentEnvelope<FooWebhookRequest> Deserialize(string rawJson) => default!;
}

// ✓ ACCEPTABLE inheritance — thin template with fixed algorithm + few abstract hooks
// ReceiverServiceBase: one level, subclasses only override path/tags/messageId.
public sealed class BarCreatedReceiverService(/* deps */)
    : ReceiverServiceBase<BarWebhookRequest>(/* logger, storage, bus */)
{
    protected override string BuildBlobPath(BarWebhookRequest model) => $"bar/{model.Id}";
}

// ✗ WRONG — deep hierarchy
// ProcessorServiceBase → TransactionProcessorBase → KlarnaProcessorBase → KlarnaCaptureProcessor

// ✗ WRONG — inheritance to reuse one helper method
public abstract class MapperBase
{
    protected string FormatSku(string sku) => sku.Trim().ToUpperInvariant();
}
public sealed class FooMapper : MapperBase { }  // prefer static helper or composition

// ✓ CORRECT — static helper or injected service for shared pure logic
public static class SkuFormatter
{
    public static string Format(string sku) => sku.Trim().ToUpperInvariant();
}

// Decorator is composition: wrap inner implementation (see patterns/decorator-pattern.cs)

interface IFooEnrichmentPipeline { Task RunAsync(EnrichmentEnvelope<FooWebhookRequest> e, CancellationToken ct); }
interface IOutboundFooMapper { FooMaoModel? Map(EnrichmentEnvelope<FooWebhookRequest> e); }
interface IMaoPublisher { Task PublishAsync(FooMaoModel payload, CancellationToken ct); }
interface IServiceBusClient { }
record EnrichmentEnvelope<T>(T Source);
record FooWebhookRequest;
record BarWebhookRequest(string Id);
record FooMaoModel;
abstract class ReceiverServiceBase<T>(ILogger logger, IBlobStorageClient storage, IServiceBusClient bus)
{
    protected abstract string BuildBlobPath(T model);
}
interface ILogger { }
interface IBlobStorageClient { }
