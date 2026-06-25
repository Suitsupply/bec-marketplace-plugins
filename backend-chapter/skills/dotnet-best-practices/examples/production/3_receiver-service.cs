using Microsoft.Extensions.Logging;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Models.Foo.Models.Webhooks;
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.App.Services.Receivers;

public class FooReceiverService(
    ILogger<FooReceiverService> logger,
    IEventBlobStorageClient eventBlobStorageClient,
    IStoreServiceBusClient storeServiceBusClient)
    : ReceiverServiceBase<FooCreatedWebhook>(logger, eventBlobStorageClient, storeServiceBusClient),
      IFooReceiverService
{
    protected override EventType EventType => EventType.FooCreated;

    protected override string GetMessageId(FooCreatedWebhook model) => BuildMessageId(model.Id);

    protected override string GetPath(FooCreatedWebhook model) => BuildBlobPath(model.Id, model.Name);

    protected override IDictionary<string, string> GetTags(FooCreatedWebhook model)
        => BuildBlobTags(model.Id, model.Name);
}
