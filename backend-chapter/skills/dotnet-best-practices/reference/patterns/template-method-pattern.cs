// Template Method pattern — base class defines the algorithm skeleton; subclasses override specific steps.
// Chapter: ReceiverServiceBase<TModel> — shared deserialize → backup → queue flow.

public abstract class ReceiverServiceBase<TModel>(ILogger logger, IBlobStorageClient storageClient, IServiceBusClient serviceBusClient)
    where TModel : class
{
    // Template method — fixed sequence, not overridable
    public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);

        var model = Deserialize(rawJson);
        ArgumentNullException.ThrowIfNull(model);

        LogReceiverEvent(model);
        await BackupAsync(rawJson, model, cancellationToken);
        await SendToQueueAsync(rawJson, model, cancellationToken);
    }

    // Hooks — subclasses supply event-specific behaviour
    protected abstract EventType EventType { get; }
    protected abstract string GetMessageId(TModel model);
    protected abstract string GetPath(TModel model);
    protected abstract IDictionary<string, string> GetTags(TModel model);

    private TModel Deserialize(string rawJson) =>
        System.Text.Json.JsonSerializer.Deserialize<TModel>(rawJson)!;

    private async Task BackupAsync(string rawJson, TModel model, CancellationToken cancellationToken)
    {
        await storageClient.UploadBlobAsync(EventType, GetPath(model), rawJson, GetTags(model), cancellationToken);
        logger.LogInformation("Backup saved for {EventType}", EventType);
    }

    private async Task SendToQueueAsync(string rawJson, TModel model, CancellationToken cancellationToken)
    {
        var messageId = GetMessageId(model);
        await serviceBusClient.SendToProcessorQueueAsync(EventType, rawJson, messageId, GetTags(model), cancellationToken);
    }

    private void LogReceiverEvent(TModel model) =>
        logger.LogInformation("Received {EventType}", EventType);
}

// Concrete implementation — only hooks, no duplicated flow
public sealed class OrderCreatedReceiverService(
    ILogger<OrderCreatedReceiverService> logger,
    IBlobStorageClient storageClient,
    IServiceBusClient serviceBusClient)
    : ReceiverServiceBase<OrderCreatedWebhookRequest>(logger, storageClient, serviceBusClient),
      IOrderCreatedReceiverService
{
    protected override EventType EventType => EventType.OrderCreated;

    protected override string GetMessageId(OrderCreatedWebhookRequest model) => model.Id;

    protected override string GetPath(OrderCreatedWebhookRequest model) =>
        $"orders/created/{model.Id}.json";

    protected override IDictionary<string, string> GetTags(OrderCreatedWebhookRequest model) =>
        new Dictionary<string, string> { ["orderId"] = model.Name };
}

// ✗ WRONG — copy-paste ProcessAsync in every receiver
// ✓ CORRECT — extend ReceiverServiceBase; override hooks only

// Mapper bases use the same idea: OutboundLineProductMapperBase with MapCore() + subclass specifics.

enum EventType { OrderCreated }
record OrderCreatedWebhookRequest(string Id, string Name);
interface IOrderCreatedReceiverService { }
interface ILogger<T> { void LogInformation(string message, params object[] args); }
interface IBlobStorageClient { Task UploadBlobAsync(EventType e, string path, string content, IDictionary<string, string> tags, CancellationToken ct); }
interface IServiceBusClient { Task SendToProcessorQueueAsync(EventType e, string body, string messageId, IDictionary<string, string> tags, CancellationToken ct); }
