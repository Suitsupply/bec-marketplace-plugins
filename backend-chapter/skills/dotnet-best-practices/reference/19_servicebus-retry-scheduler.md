# Service Bus retry / dead-letter scheduler

> Reference **19** — delayed-retry + dead-letter for Service Bus processors (`IServiceBusRetryScheduler`).

**Optional — Azure Functions Service Bus processors only.** Use when a queue-triggered function needs *delayed* retries with a bounded attempt count before dead-lettering. Referenced from [error handling](../SKILL.md#4-error-handling-and-logging) and [15_azure-functions.md](15_azure-functions.md).

> **Chapter-package candidate.** `IServiceBusRetryScheduler` + `ServiceBusRetryScheduler` (and `MessageRetryOptions` / `RetryOutcome`) are generic, service-agnostic plumbing. Today each service copies them into `Api/Messaging/`; they are intended to graduate into a **shared Suitsupply chapter package** (alongside `ServiceInfo` — see [Chapter common packages](../SKILL.md#chapter-common-packages)). Until then, copy this reference implementation and leave a `// TODO: move to shared chapter package` marker; do not fork the behaviour per service.

## Why

Azure Service Bus has **no native delayed retry**. The broker's `DeliveryCount` increments on immediate redelivery, but you cannot say "retry in 30s". The scheduler works around this by **completing the original message and enqueuing a scheduled copy**. Because a re-sent message is a brand-new message whose broker `DeliveryCount` resets to 0, the attempt count is carried forward in an application property.

## Contract

```csharp
public interface IServiceBusRetryScheduler
{
    Task<RetryOutcome> RescheduleOrDeadLetterAsync(
        ServiceBusMessageActions messageActions,
        ServiceBusReceivedMessage message,
        string queueName,
        Exception exception,
        CancellationToken cancellationToken = default);
}

public enum RetryOutcome { Rescheduled, DeadLettered }
```

The scheduler **does not log domain context** — it returns `RetryOutcome` so the calling function logs with ids available at its layer (`{MessageId}`).

## Behaviour

1. Resolve the carried attempt count: read the `DeliveryCount` application property; fall back to the broker's native `DeliveryCount` when the property is missing or not an `int`.
2. If `deliveryCount >= MaxDeliveryCount` → `LogError`, `DeadLetterMessageAsync`, return `DeadLettered`.
3. Otherwise build a scheduled copy (`ScheduledEnqueueTime = now + delay`), set its `DeliveryCount` property to `deliveryCount + 1`, send it via a cached `ServiceBusSender` per queue, then `CompleteMessageAsync` the original → return `Rescheduled`.
4. Delay = `RetryDelay × BackoffMultiplier^(attempt − 1)`.

## Settings — `MessageRetryOptions`

`Api/Messaging/Settings/MessageRetryOptions.cs` (`[ExcludeFromCodeCoverage]`; validator unit-tested via FluentValidation + `ValidateOnStart()` — see [12_configuration-validation.md](12_configuration-validation.md)):

| Key | Default | Rule |
|-----|---------|------|
| `MaxDeliveryCount` | 3 | `> 0` and `<= 10` |
| `RetryDelay` | `00:00:30` | `> 0` |
| `BackoffMultiplier` | 1 | `>= 1` (1 = fixed delay, 2 = double each attempt) |

## Layout

| File | Role |
|------|------|
| `Api/Messaging/Interfaces/IServiceBusRetryScheduler.cs` | Contract |
| `Api/Messaging/ServiceBusRetryScheduler.cs` | Implementation — `IAsyncDisposable`; caches one `ServiceBusSender` per queue |
| `Api/Messaging/RetryOutcome.cs` | Result enum |
| `Api/Messaging/Settings/MessageRetryOptions.cs` | Options record |
| `Api/Messaging/Validators/MessageRetryOptionsValidator.cs` | `internal sealed AbstractValidator<MessageRetryOptions>` |

Registered as a **singleton** in `Program.cs` (it owns long-lived senders). The implementation has real logic, so it is **unit-tested** and **not** `[ExcludeFromCodeCoverage]`.

## Calling from a processor

```csharp
try
{
    var domain = FooMapper.ToDomain(requestDto);
    await fooService.ProcessAsync(domain, cancellationToken);
    await messageActions.CompleteMessageAsync(message, cancellationToken);
}
catch (Exception ex)
{
    var outcome = await retryScheduler.RescheduleOrDeadLetterAsync(
        messageActions, message, options.Value.StoreServiceBus.FooQueueName, ex, cancellationToken);

    if (outcome == RetryOutcome.DeadLettered)
        logger.LogError(ex, "Foo message {MessageId} dead-lettered.", message.MessageId);
    else
        logger.LogWarning(ex, "Foo processing failed for {MessageId}, rescheduled for retry.", message.MessageId);
}
```

This is the **one** broad `catch (Exception)` allowed at the Api boundary — it routes to the scheduler instead of rethrowing (the message is always settled). See [7_exception-handling.md](7_exception-handling.md).

## Testing

- **Unit** (`{ServiceName}.UnitTests`): mock `ServiceBusClient`/`ServiceBusSender`/`ServiceBusMessageActions`; build messages with `ServiceBusModelFactory.ServiceBusReceivedMessage`. Cover reschedule-below-max, dead-letter-at-max, the property-vs-broker delivery-count fallback, and sender disposal.
- **Component** (`{ServiceName}.ComponentTests`): mock `IServiceBusRetryScheduler` on `ApplicationFactory` and assert the function calls it; exercise the processor through its `_Debug` HTTP twin (see [15_azure-functions.md](15_azure-functions.md)).

> Chapter intent: this scheduler is a candidate for a **shared chapter package**. Until then, copy the `Api/Messaging/` folder from the template and bind `MessageRetryOptions`.
