using Microsoft.Extensions.Logging;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Models.Webhooks;
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.App.Services.Receivers;

public class FooCreatedReceiverService(
    ILogger<FooCreatedReceiverService> logger,
    IBlobStorageClient storageClient,
    IServiceBusClient serviceBusClient)
    : ReceiverServiceBase<FooCreatedWebhookRequest>(logger, storageClient, serviceBusClient),
      IFooCreatedReceiverService
{
    protected override EventType EventType => EventType.FooCreated;

    protected override string GetMessageId(FooCreatedWebhookRequest model) => BuildMessageId(model.Id);

    protected override string GetPath(FooCreatedWebhookRequest model) => BuildBlobPath(model.Id, model.Name);

    protected override IDictionary<string, string> GetTags(FooCreatedWebhookRequest model)
        => BuildBlobTags(model.Id, model.Name);
}
