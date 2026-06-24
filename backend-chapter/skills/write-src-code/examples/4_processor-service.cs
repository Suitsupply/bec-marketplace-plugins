using System.Text.Json;
using Microsoft.Extensions.Logging;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Enrichment;
using {ServiceName}.App.Models.Enrichment;
using {ServiceName}.App.Models.Webhooks;
using {ServiceName}.App.Services.Processors.Interfaces;

namespace {ServiceName}.App.Services.Processors;

public class FooProcessorService(
    ILogger<FooProcessorService> logger,
    FooEnrichmentPipeline enrichmentPipeline,
    IOutboundPublisher outboundPublisher,
    IStoreServiceBusClient storeServiceBusClient)
    : IFooProcessorService
{
    public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);

        var message = JsonSerializer.Deserialize<FooWebhookRequest>(rawJson);
        ArgumentNullException.ThrowIfNull(message);

        logger.LogInformation("Processing foo created for order {OrderId} ({OrderName}).", message.Id, message.Name);

        var envelope = new EnrichmentEnvelope<FooWebhookRequest> { Source = message };
        await enrichmentPipeline.RunAsync(envelope, cancellationToken);

        var messageId = await PublishEventAsync(message, envelope, cancellationToken);
        await PublishBackupAsync(message, envelope, messageId, cancellationToken);
    }

    private async Task<string> PublishEventAsync(FooWebhookRequest message, EnrichmentEnvelope<FooWebhookRequest> envelope, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing outbound event for order {OrderId}.", message.Id);

        var messageId = await outboundPublisher.PublishAsync(envelope, cancellationToken);

        logger.LogInformation("Published outbound event for order {OrderId}; messageId={MessageId}.", message.Id, messageId);

        return messageId;
    }

    private async Task PublishBackupAsync(FooWebhookRequest message, EnrichmentEnvelope<FooWebhookRequest> envelope, string messageId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending backup for order {OrderId}; messageId={MessageId}.", message.Id, messageId);

        await storeServiceBusClient.SendBackupAsync(envelope, messageId, cancellationToken);

        logger.LogInformation("Sent backup for order {OrderId}.", message.Id);
    }
}
