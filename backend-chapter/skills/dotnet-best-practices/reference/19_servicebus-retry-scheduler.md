# Service Bus retry / dead-letter scheduler

> Reference **19** — delayed-retry + dead-letter for Service Bus processors (`IServiceBusRetryScheduler`).

**Optional — Azure Functions Service Bus processors only.** Use when a queue-triggered function needs *delayed* retries with a bounded attempt count before dead-lettering. Referenced from [error handling](../SKILL.md#4-error-handling-and-logging) and [15_azure-functions.md](15_azure-functions.md).

> **Shared chapter package.** `IServiceBusRetryScheduler`, `ServiceBusRetryScheduler`, `MessageRetryOptions`, and `RetryOutcome` live in **`Suitsupply.Common.ServiceBusRetryScheduler`** (see [Chapter common packages](../SKILL.md#chapter-common-packages)). Do **not** copy them into `Api/Messaging/` — add the NuGet package to the Api project and register via `AddServiceBusRetryScheduler`.

## Why

Azure Service Bus has **no native delayed retry**. The broker's `DeliveryCount` increments on immediate redelivery, but you cannot say "retry in 30s". The scheduler works around this by **completing the original message and enqueuing a scheduled copy**. Because a re-sent message is a brand-new message whose broker `DeliveryCount` resets to 0, the attempt count is carried forward in an application property.

## Package and registration

**NuGet (Api project):** `Suitsupply.Common.ServiceBusRetryScheduler`

```csharp
using Common.ServiceBusRetryScheduler.Extensions;
using Common.ServiceBusRetryScheduler.Settings;

services.AddServiceBusRetryScheduler(config.GetSection(nameof(MessageRetryOptions)));
```

`AddServiceBusRetryScheduler` binds `MessageRetryOptions`, validates via FluentValidation (`ValidateOnStart`), and registers `IServiceBusRetryScheduler` as a singleton (it owns long-lived senders).

## Contract

```csharp
using Common.ServiceBusRetryScheduler;
using Common.ServiceBusRetryScheduler.Interfaces;

// IServiceBusRetryScheduler.RescheduleOrDeadLetterAsync(...) → RetryOutcome
public enum RetryOutcome { Rescheduled, DeadLettered }
```

The scheduler **does not log domain context** — it returns `RetryOutcome` so the calling function logs with ids available at its layer (`{MessageId}`).

## Behaviour

1. Resolve the carried attempt count: read the `DeliveryCount` application property; fall back to the broker's native `DeliveryCount` when the property is missing or not an `int`.
2. If `deliveryCount >= MaxDeliveryCount` → `LogError`, `DeadLetterMessageAsync`, return `DeadLettered`.
3. Otherwise build a scheduled copy (`ScheduledEnqueueTime = now + delay`), set its `DeliveryCount` property to `deliveryCount + 1`, send it via a cached `ServiceBusSender` per queue, then `CompleteMessageAsync` the original → return `Rescheduled`.
4. Delay = `RetryDelay × BackoffMultiplier^(attempt − 1)`.

## Settings — `MessageRetryOptions`

Namespace: `Common.ServiceBusRetryScheduler.Settings` (`[ExcludeFromCodeCoverage]`; validator unit-tested in the common package via FluentValidation + `ValidateOnStart()` — see [12_configuration-validation.md](12_configuration-validation.md)):

| Key | Default | Rule |
|-----|---------|------|
| `MaxDeliveryCount` | 3 | `> 0` and `<= 10` |
| `RetryDelay` | `00:00:30` | `> 0` |
| `BackoffMultiplier` | 1 | `>= 1` (1 = fixed delay, 2 = double each attempt) |

Bind in `local.settings.json` / app settings as `MessageRetryOptions__MaxDeliveryCount`, etc.

## Calling from a processor

```csharp
using Common.ServiceBusRetryScheduler;
using Common.ServiceBusRetryScheduler.Interfaces;

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

- **Unit** (`{ServiceName}.UnitTests`): mock `IServiceBusRetryScheduler` on processor/receiver tests; scheduler behaviour is covered in `Suitsupply.Common.ServiceBusRetryScheduler` unit tests.
- **Component** (`{ServiceName}.ComponentTests`): mock `IServiceBusRetryScheduler` on `ApplicationFactory` and assert the function calls it; exercise the processor through its `_Debug` HTTP twin (see [15_azure-functions.md](15_azure-functions.md)).
