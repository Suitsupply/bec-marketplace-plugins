# Exception handling — bubble up, log at Api

Unexpected exceptions are **not** caught in App or Infra. They propagate to the **Api** layer (`Functions/`), where they are logged and surfaced — never swallowed.

---

## Layer rules

| Layer | Unexpected exceptions |
|-------|----------------------|
| **App** | **Do not catch** — services, enrichment steps, mappers, validators throw; no `try/catch` + log in business code |
| **Infra** | **Do not catch** — client methods throw on failure; let callers (App) handle via bubble to Api |
| **Api** (`Functions/`) | **Catch boundary** — `LogError(ex, …)` then **rethrow** or map to HTTP / retry scheduler |

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
    logger.LogError(ex, "{Function} failed.", nameof(FooCreatedReceiver));
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

## App layer — do not catch

```csharp
// ✗ wrong — catch + log in App service
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

// ✓ correct — let exception bubble to Api Function
public async Task ProcessAsync(FooWebhookRequest message, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(message);
    logger.LogInformation("Processing order updated for order {OrderId}.", message.Id);
    await enrichmentPipeline.RunAsync(envelope, cancellationToken);
    // …
}
```

Enrichment steps: `LogWarning` + fallback for **non-critical** lookups only — unexpected failures **throw** without catch.

---

## Checklist

- [ ] No `catch (Exception)` in App services, steps, mappers, or Infra clients (unless wrapping a third-party API with a domain exception — rare)
- [ ] Api `Functions/` have the outer `try/catch` with `LogError(ex, …)`
- [ ] Never swallow — log + rethrow, log + HTTP 500, or log + retry scheduler
