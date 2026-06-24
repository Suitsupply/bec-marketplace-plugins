# Observability and logging

Structured logging for traceability in Application Insights / Log Analytics. Use **`ILogger` with named template properties** — portable across all services. Single-line messages; no `$"..."` interpolation.

**Principles (all services):**

1. **One entry log per layer** — first meaningful action after guards.
2. **Log identifiers available at that boundary** — do not invent ids you do not have yet.
3. **Same properties on errors** as on the entry log for that layer.
4. **Always pass `ex`** to `LogError` / `LogWarning` when logging exceptions.

Prefix strings (`Receiver -`, `Processor -`, `{EventType}`) are **ShopifyIntegration conventions** — optional for other repos. See [ShopifyIntegration example](#optional-shopifyintegration-convention) below.

---

## Entry logs by layer

| Layer | When | Log these identifiers (when available) |
|-------|------|----------------------------------------|
| **Api HTTP trigger** | After guards, **before** body read | `{Function}`; optional `{Flow}` / route feature name |
| **Api Service Bus trigger** | First line in `Run` | `{Function}`, `{MessageId}` |
| **App inbound handler** | After deserialize / domain model built | Business ids — `{OrderId}`, `{CustomerId}`, `{TransactionId}`, … |
| **App flow handler** | Start of `HandleAsync` | Same business ids as the parent flow |
| **Api query** | After route/query params resolved | The id being queried — `{OrderId}`, `{ResourceId}`, … |

**HTTP triggers** usually cannot log `{OrderId}` until App deserializes the body. **Queue triggers** log `{MessageId}` at the Function; App logs business ids after parse.

```csharp
// Api/Functions/Receivers/FooCreatedReceiver.cs — before body read
logger.LogInformation("{Function} invoked.", nameof(FooCreatedReceiver));

try
{
    var rawJson = await request.Body.ReadStreamAsString();
    await fooCreatedReceiverService.ProcessAsync(rawJson, cancellationToken);
    return new AcceptedResult();
}
catch (Exception ex)
{
    logger.LogError(ex, "{Function} failed.", nameof(FooCreatedReceiver));
    return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
}
```

```csharp
// App/Services/Processors/FooProcessorService.cs — after deserialize
logger.LogInformation("Processing foo created for order {OrderId} ({OrderName}).", message.Id, message.Name);
```

```csharp
// Api/Functions/Processors/FooProcessor.cs — Service Bus
logger.LogInformation("{Function} message {MessageId} received.", nameof(FooProcessor), message.MessageId);
```

**Avoid** logs that only add a timestamp or generic text without useful correlation properties:

```csharp
// ✗ low trace value
logger.LogInformation("{Function} executed at {Time}", nameof(FooCreatedReceiver), DateTime.UtcNow);
```

---

## Errors — always log at Api boundary

| Situation | Level | Include |
|-----------|-------|---------|
| Unexpected exception | `LogError` | `ex` + same ids as entry log for that layer |
| Service Bus dead-letter | `LogError` | `ex`, `{MessageId}` |
| Service Bus rescheduled | `LogWarning` | `ex`, `{MessageId}` |
| Expected business skip | `LogInformation` or `LogWarning` | Why skipped + business ids |
| Non-critical lookup failed | `LogWarning` | Enrichment fallback — do not fail |

App and Infra let exceptions bubble — do not catch-and-log in business code. See [exception-handling.md](exception-handling.md).

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "{Function} message {MessageId} failed.", nameof(FooProcessor), message.MessageId);
    throw; // or HTTP 500 / retry scheduler
}
```

---

## Informational logs — use sparingly

Beyond the **entry** line, add `LogInformation` only for troubleshooting, dashboards, or expected branches operators must see. Action-specific logs (publish sent, backup sent) belong in **named private methods** when needed — see [named-private-methods.md](named-private-methods.md).

---

## Format rules

- **Single line** per log call — `{NamedProperties}` in the template
- **No** `$"..."` interpolation (CA1873)
- **Stable property names** across a flow so Application Insights queries work (`OrderId`, not `id` in one place and `order_id` in another)
- Pick a **consistent message shape within the service** — document it in the repo README or a short team convention; do not copy another service's prefix literals unless you adopt that convention wholesale

---

## Azure Functions summary

| Trigger | Entry log (generic) | Failure |
|---------|---------------------|---------|
| HTTP | `{Function} invoked.` | `LogError(ex, …)` → HTTP 500 |
| Service Bus | `{Function} message {MessageId} received.` | Retry scheduler — `LogError` / `LogWarning` |
| `_Debug` HTTP | `LogWarning` | `LogError` → HTTP 500 |

Examples: [processor-function.cs](../../write-src-code/examples/processor-function.cs), [receiver-function.cs](../../write-src-code/examples/receiver-function.cs).

---

## Optional: ShopifyIntegration convention

[ShopifyIntegration](https://github.com/Suitsupply/shopifyintegration) uses fixed prefixes and an `EventType` enum for webhook flows:

| Pattern | Example |
|---------|---------|
| HTTP receiver | `"Receiver - {EventType} - {Function} invoked."` |
| App processor | `"Processor - {EventType} - order {OrderId} ({OrderName}) - Processing."` |
| Service Bus Function | `"Processor - {EventType} - message {MessageId} received."` |

Teams may add `ReceiverLoggingExtensions` / `ProcessorLoggingExtensions` in `App/Extensions/Logging/` to centralize these prefixes. **Not required** for new services.

---

## Checklist

- [ ] Api HTTP entry logs `{Function}` (and optional flow name) **before** body read
- [ ] Api Service Bus entry logs `{MessageId}` before calling App
- [ ] App handler entry logs **business ids** after deserialize (e.g. `{OrderId}`)
- [ ] Api `Functions/` only catch unexpected exceptions — `LogError(ex, …)` with the same correlation properties as entry
- [ ] No timestamp-only entry logs
- [ ] All log calls single-line structured templates — no `$"..."` interpolation
