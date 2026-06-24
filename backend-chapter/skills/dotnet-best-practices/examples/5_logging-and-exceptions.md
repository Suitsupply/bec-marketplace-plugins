# Logging and exception handling

> Example **5** — Structured logging and bubble-up exception handling.

## Error handling / logging

```csharp
// ✗ wrong — timestamp only; no useful correlation for Application Insights
logger.LogInformation("{Function} executed at {Time}", nameof(FooReceiver), DateTime.UtcNow);

// ✓ correct — Api HTTP entry (before body read): Function name is enough at this boundary
logger.LogInformation("{Function} invoked.", nameof(FooReceiver));

// ✓ correct — App entry (after deserialize): business ids
logger.LogInformation("Processing order created for order {OrderId} ({OrderName}).", orderId, orderName);
```

```csharp
// ✗ wrong — logs message only, loses stack
catch (Exception ex)
{
    logger.LogError("Failed: " + ex.Message);
}

// ✓ correct — structured logging with exception + same ids as entry log
catch (Exception ex)
{
    logger.LogError(ex, "Failed to process order {OrderId} in {Processor}", orderId, nameof(FooProcessor));
}
```

```csharp
// ✗ wrong — noisy info on every pipeline step
logger.LogInformation("Fetching order…");
logger.LogInformation("Enrichment completed.");
logger.LogInformation("Mapping to outbound…");

// ✓ correct — entry log + errors/warnings only when needed
logger.LogInformation("Processing order created for order {OrderId}.", orderId);
// … work …
logger.LogWarning("Order {OrderId} skipped — no outbound payload.", orderId);
```

```csharp
// ✗ wrong — multi-line log message
logger.LogInformation(
    "Processing order " + orderId +
    " for customer " + customerId);

// ✓ correct — single structured line
logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", orderId, customerId);
```

## Exception handling — bubble vs recover

```csharp
// ✗ wrong — catch, LogError, and rethrow in App (Api logs unrecoverable failures)
public async Task ProcessAsync(Envelope envelope, CancellationToken ct)
{
    try { await pipeline.RunAsync(envelope, ct); }
    catch (Exception ex) { logger.LogError(ex, "Failed"); throw; }
}

// ✓ correct — unrecoverable: let exception bubble to Api Function
public async Task ProcessAsync(Envelope envelope, CancellationToken ct)
{
    await pipeline.RunAsync(envelope, ct);
}

// ✓ correct — recoverable: optional lookup with fallback
try
{
    envelope.CustomerTier = await loyaltyClient.GetTierAsync(customerId, ct);
}
catch (HttpRequestException ex)
{
    logger.LogWarning(ex, "Loyalty lookup failed for {CustomerId}; using default tier.", customerId);
    envelope.CustomerTier = CustomerTier.Standard;
}

// ✓ correct — Api Function logs unrecoverable failure (rethrow, HTTP 500, or retry scheduler)
catch (Exception ex)
{
    logger.LogError(ex, "{Function} message {MessageId} failed.", nameof(FooProcessor), messageId);
    throw;
}
```

See [7_exception-handling.md](../reference/7_exception-handling.md), [8_observability-logging.md](../reference/8_observability-logging.md).
