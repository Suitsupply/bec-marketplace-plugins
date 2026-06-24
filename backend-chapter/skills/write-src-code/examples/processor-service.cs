using System.Text.Json;
using Microsoft.Extensions.Logging;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Enrichment;
using {ServiceName}.App.Mappers;
using {ServiceName}.App.Models.Enrichment;
using {ServiceName}.App.Models.Outbound;
using {ServiceName}.App.Models.Webhooks;
using {ServiceName}.App.Services.Processors.Interfaces;

namespace {ServiceName}.App.Services.Processors;

public class FooProcessorService(
    ILogger<FooProcessorService> logger,
    FooEnrichmentPipeline enrichmentPipeline,
    IOutboundFooMapper outboundFooMapper,
    IOutboundPublisher outboundPublisher,
    IServiceBusClient serviceBusClient)
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

        var payload = outboundFooMapper.Map(envelope);
        if (payload is null) return;

        var messageId = await PublishEventAsync(message, payload, cancellationToken);
        await PublishBackupAsync(message, payload, messageId, cancellationToken);
    }

    private async Task<string> PublishEventAsync(FooWebhookRequest message, OutboundFooPayload payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing outbound event for order {OrderId}.", message.Id);

        var messageId = await outboundPublisher.PublishAsync(payload, cancellationToken);

        logger.LogInformation("Published outbound event for order {OrderId}; messageId={MessageId}.", message.Id, messageId);

        return messageId;
    }

    private async Task PublishBackupAsync(FooWebhookRequest message, OutboundFooPayload payload, string messageId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending backup for order {OrderId}; messageId={MessageId}.", message.Id, messageId);

        await serviceBusClient.SendBackupAsync(payload, messageId, cancellationToken);

        logger.LogInformation("Sent backup for order {OrderId}.", message.Id);
    }
}
