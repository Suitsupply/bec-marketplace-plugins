# Exception handling — bubble up, log at Api

> Reference **7** — Unrecoverable exceptions propagate to Api; recoverable failures use a defined fallback in App/Infra.

**Unrecoverable** exceptions are not caught in App or Infra — they propagate to the **Api** layer (`Functions/`), where they are logged and surfaced.

**Recoverable** failures may be caught in App or Infra when there is a defined recovery path (fallback value, skip an optional step, return `null`). Catch the **specific** exception type — not `catch (Exception)` unless wrapping a third-party API into a domain exception.

---

## Layer rules

| Layer | Unrecoverable exceptions | Recoverable failures |
|-------|-------------------------|-------------------|
| **App** | **Let bubble** — no `try/catch` + `LogError` + rethrow | Catch **specific** exceptions; apply fallback; `LogWarning` when useful |
| **Infra** | **Let bubble** — client throws on hard failure | Rare — e.g. map `404` to `null`, retry policy inside client |
| **Api** (`Functions/`) | **Catch boundary** — `LogError(ex, …)` then rethrow, HTTP `500`, or retry scheduler | N/A — recovery belongs in App/Infra |

Expected business outcomes (return `null`, early `return`, `Result<T>`) stay in App — not exceptions.

---

## Api layer patterns

### HTTP receivers / queries

Catch at the Function, log, return `500` (do not leak stack traces in production):

```csharp
try
{
    await receiverService.ProcessAsync(domain, cancellationToken);
    return new AcceptedResult();
}
catch (Exception ex)
{
    logger.LogError(ex, "{Function} failed.", nameof(FooReceiver));
    return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
}
```

### Service Bus processors (with `IServiceBusRetryScheduler`)

Catch at the Function, delegate to scheduler, log outcome. **No rethrow** — scheduler reschedules or dead-letters the message (failure is recorded; message handling is complete):

```csharp
try
{
    await processorService.ProcessAsync(message.Body.ToString(), cancellationToken);
    await messageActions.CompleteMessageAsync(message, cancellationToken);
}
catch (Exception ex)
{
    var outcome = await retryScheduler.RescheduleOrDeadLetterAsync(messageActions, message, queueName, ex, cancellationToken);
    if (outcome == RetryOutcome.DeadLettered)
        logger.LogError(ex, "{Function} message {MessageId} dead-lettered.", nameof(FooProcessor), message.MessageId);
    else
        logger.LogWarning(ex, "{Function} message {MessageId} rescheduled.", nameof(FooProcessor), message.MessageId);
}
```

### Service Bus / generic — log and rethrow

When no retry scheduler handles the message, log at Api and **rethrow** so the host can retry or fail the invocation:

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "{Function} message {MessageId} failed.", nameof(FooProcessor), message.MessageId);
    throw;
}
```

---

## App layer — bubble vs recover

```csharp
// ✗ wrong — catch + LogError + rethrow in App (Api should log unrecoverable failures)
public async Task ProcessAsync(FooWebhookRequest message, CancellationToken cancellationToken)
{
    try
    {
        await enrichmentPipeline.RunAsync(envelope, cancellationToken);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Enrichment failed");
        throw;
    }
}

// ✓ correct — unrecoverable: let exception bubble to Api Function
public async Task ProcessAsync(FooWebhookRequest message, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(message);
    logger.LogInformation("Processing order updated for order {OrderId}.", message.Id);
    await enrichmentPipeline.RunAsync(envelope, cancellationToken);
}

// ✓ correct — recoverable: optional lookup with fallback
try
{
    envelope.CustomerTier = await loyaltyClient.GetTierAsync(order.CustomerId, cancellationToken);
}
catch (HttpRequestException ex)
{
    logger.LogWarning(ex, "Loyalty lookup failed for customer {CustomerId}; using default tier.", order.CustomerId);
    envelope.CustomerTier = CustomerTier.Standard;
}
```

---

## Checklist

- [ ] No `catch (Exception)` + `LogError` + rethrow in App or Infra
- [ ] Recoverable failures use a **specific** catch and a defined fallback — not a broad catch
- [ ] Api `Functions/` have the outer `try/catch` with `LogError(ex, …)` for unrecoverable failures
- [ ] Never swallow — log + rethrow, log + HTTP 500, or log + retry scheduler
