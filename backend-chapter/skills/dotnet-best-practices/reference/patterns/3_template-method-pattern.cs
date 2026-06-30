// Template Method pattern — base class defines the algorithm skeleton; subclasses override specific steps.
// Chapter: ReceiverServiceBase<TModel> — shared backup → queue flow; domain arrives from Api after mapping.

public abstract class ReceiverServiceBase<TModel>(ILogger logger, IEventBlobStorageClient eventBlobStorageClient, IStoreServiceBusClient storeServiceBusClient)
    where TModel : class
{
    // Template method — fixed sequence, not overridable
    public async Task ProcessAsync(TModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var rawJson = JsonSerializer.Serialize(model);

        LogReceiverEvent(model);
        await BackupAsync(rawJson, model, cancellationToken);
        await SendToQueueAsync(rawJson, model, cancellationToken);
    }

    // Hooks — subclasses supply event-specific behaviour
    protected abstract EventType EventType { get; }
    protected abstract string GetMessageId(TModel model);
    protected abstract string GetPath(TModel model);
    protected abstract IDictionary<string, string> GetTags(TModel model);

    private async Task BackupAsync(string rawJson, TModel model, CancellationToken cancellationToken)
    {
        await eventBlobStorageClient.UploadBlobAsync(EventType, GetPath(model), rawJson, GetTags(model), cancellationToken);
        logger.LogInformation("Backup saved for {EventType}", EventType);
    }

    private async Task SendToQueueAsync(string rawJson, TModel model, CancellationToken cancellationToken)
    {
        var messageId = GetMessageId(model);
        await storeServiceBusClient.SendToProcessorQueueAsync(EventType, rawJson, messageId, GetTags(model), cancellationToken);
    }

    private void LogReceiverEvent(TModel model) =>
        logger.LogInformation("Received {EventType}", EventType);
}

// Concrete implementation — only hooks, no duplicated flow
public sealed class OrderCreatedReceiverService(ILogger<OrderCreatedReceiverService> logger, IEventBlobStorageClient eventBlobStorageClient, IStoreServiceBusClient storeServiceBusClient) : ReceiverServiceBase<OrderCreatedWebhook>(logger, eventBlobStorageClient, storeServiceBusClient), IOrderCreatedReceiverService
{
    protected override EventType EventType => EventType.OrderCreated;

    protected override string GetMessageId(OrderCreatedWebhook model) => model.Id.ToString();

    protected override string GetPath(OrderCreatedWebhook model) =>
        $"orders/created/{model.Id}.json";

    protected override IDictionary<string, string> GetTags(OrderCreatedWebhook model) =>
        new Dictionary<string, string> { ["orderId"] = model.Name ?? string.Empty };
}

// ✗ WRONG — copy-paste ProcessAsync in every receiver
// ✓ CORRECT — extend ReceiverServiceBase; override hooks only

// Mapper bases use the same idea: OutboundLineProductMapperBase with MapCore() + subclass specifics.
