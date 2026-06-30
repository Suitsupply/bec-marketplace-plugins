using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace Template.Api.Messaging.Interfaces;

public interface IServiceBusRetryScheduler
{
    /// <summary>
    /// Reschedules a failed message for a delayed redelivery onto <paramref name="queueName"/>, or dead-letters it
    /// once the carried delivery count reaches the configured maximum.
    /// Returns the outcome so the caller can emit domain-specific logs.
    /// </summary>
    Task<RetryOutcome> RescheduleOrDeadLetterAsync(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message, string queueName,
        Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reschedules a failed message for a delayed redelivery onto <paramref name="queueName"/>, dead-letters it
    /// immediately when <paramref name="exception"/> matches any type in <paramref name="immediateDeadLetterExceptionTypes"/>,
    /// or dead-letters it once the carried delivery count reaches the configured maximum.
    /// Returns the outcome so the caller can emit domain-specific logs.
    /// </summary>
    Task<RetryOutcome> RescheduleOrDeadLetterAsync(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message, string queueName,
        Exception exception, IReadOnlyList<Type> immediateDeadLetterExceptionTypes, CancellationToken cancellationToken = default);
}