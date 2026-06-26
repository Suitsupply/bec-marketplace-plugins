using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.Api.Messaging.Interfaces;
using Template.Api.Messaging.Settings;

namespace Template.Api.Messaging;

// TODO: Move to a shared chapter package together with IServiceBusRetryScheduler.
public sealed class ServiceBusRetryScheduler(ServiceBusClient serviceBusClient, IOptions<MessageRetryOptions> options, ILogger<ServiceBusRetryScheduler> logger)
    : IServiceBusRetryScheduler, IAsyncDisposable
{
    // Azure Service Bus has no native delayed-retry. A re-sent message is a brand-new message whose broker
    // DeliveryCount resets to 0, so the attempt count is carried forward in this application property.
    internal const string DeliveryCountPropertyName = "DeliveryCount";

    private readonly MessageRetryOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public async Task<RetryOutcome> RescheduleOrDeadLetterAsync(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message,
        string queueName, Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageActions);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(exception);

        var deliveryCount = ResolveDeliveryCount(message);
        if (deliveryCount >= _options.MaxDeliveryCount)
        {
            logger.LogError(exception, "Dead-lettering message {MessageId} after {DeliveryCount} of {MaxDeliveryCount} delivery attempts. Reason: {Reason}", message.MessageId, deliveryCount, _options.MaxDeliveryCount, exception.Message);
            await messageActions.DeadLetterMessageAsync(message, null, exception.Message, exception.ToString(), cancellationToken);
            return RetryOutcome.DeadLettered;
        }

        var retryMessage = CreateCopyRetryMessage(message, deliveryCount);
        await SendScheduledCopyAndCompleteOriginal(messageActions, message, queueName, retryMessage, cancellationToken);

        logger.LogInformation("Rescheduled message {MessageId} as {RetryMessageId} for delivery attempt {DeliveryCount} of {MaxDeliveryCount} at {ScheduledEnqueueTime}.", message.MessageId, retryMessage.MessageId, retryMessage.ApplicationProperties[DeliveryCountPropertyName], _options.MaxDeliveryCount, retryMessage.ScheduledEnqueueTime);
        return RetryOutcome.Rescheduled;
    }

    private static int ResolveDeliveryCount(ServiceBusReceivedMessage message)
    {
        var keyExists = message.ApplicationProperties.TryGetValue(DeliveryCountPropertyName, out var value);
        var deliveryCount = keyExists && value is int count ? count : message.DeliveryCount;

        return deliveryCount;
    }

    private TimeSpan ComputeDelay(int deliveryCount)
    {
        return TimeSpan.FromSeconds(_options.RetryDelay.TotalSeconds * Math.Pow(_options.BackoffMultiplier, deliveryCount - 1));
    }

    private ServiceBusMessage CreateCopyRetryMessage(ServiceBusReceivedMessage message, int deliveryCount)
    {
        var delay = ComputeDelay(deliveryCount);
        var retryMessage = new ServiceBusMessage(message)
        {
            ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(delay)
        };
        retryMessage.ApplicationProperties[DeliveryCountPropertyName] = deliveryCount + 1;

        return retryMessage;
    }

    private async Task SendScheduledCopyAndCompleteOriginal(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message,
        string queueName, ServiceBusMessage retryMessage, CancellationToken cancellationToken)
    {
        var sender = _senders.GetOrAdd(queueName, serviceBusClient.CreateSender);
        await sender.SendMessageAsync(retryMessage, cancellationToken);
        await messageActions.CompleteMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
            await sender.DisposeAsync();
    }
}