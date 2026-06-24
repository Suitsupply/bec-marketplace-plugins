// DRY — Don't Repeat Yourself.
// When the same sequence appears 3+ times (or twice with a clear variation point), extract — do not copy-paste.
// See: patterns/3_template-method-pattern.cs, patterns/1_factory-pattern.cs, patterns/2_strategy-pattern.cs

// ✗ WRONG — third receiver copying deserialize → backup → queue
public sealed class FooReceiverService
{
    public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken)
    {
        var model = JsonSerializer.Deserialize<FooWebhookRequest>(rawJson);
        await UploadBackupAsync(rawJson, model!, cancellationToken);
        await SendToProcessorQueueAsync(rawJson, cancellationToken);
    }
}

// ✓ CORRECT — shared algorithm in base; subclasses only define what differs (template method)
public sealed class FooReceiverService2(ILogger logger, IEventBlobStorageClient eventBlobStorageClient, IStoreServiceBusClient storeServiceBusClient) : ReceiverServiceBase<FooWebhookRequest>(logger, eventBlobStorageClient, storeServiceBusClient)
{
    protected override EventType EventType => EventType.FooCreated;
    protected override string BuildBlobPath(FooWebhookRequest model) => $"foo/{model.Id}";
}

// ✗ WRONG — same line-mapping logic pasted in three mappers
public sealed class ProductLineMapper { /* MapCore duplicated */ }
public sealed class ShippingLineMapper { /* MapCore duplicated */ }

// ✓ CORRECT — abstract base for shared logic
public abstract class OutboundLineMapperBase
{
    protected LineItem MapCore(OrderLine line) => new(line.Sku, line.Quantity, line.Price);
}

// ✗ WRONG — repeated payment branching in multiple processors
// ✓ CORRECT — factory + strategy handlers (see patterns/2_strategy-pattern.cs)

// ✓ CORRECT — extension methods for pure model logic (not *Helper classes)
public static class OrderExtensions
{
    public static decimal SumLineTotals(this IReadOnlyList<OrderLine> lines) =>
        lines.Sum(l => l.PresentmentAmount);
}

// Minimal duplication (2–3 lines) does not need a framework — see 4_YAGNI.cs.