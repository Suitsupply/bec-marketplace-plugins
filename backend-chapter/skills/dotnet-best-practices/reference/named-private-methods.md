# Named private methods — extract descriptive steps

When a block of code performs **one identifiable action**, extract it into a **private method** with a **descriptive name**. The public/orchestration method should read as a short sequence of steps — not a wall of implementation detail.

Reference: `OrderUpdatedProcessorService` in ShopifyIntegration.

---

## Rule

| Situation | Do |
|-----------|-----|
| Multi-step action with a clear purpose (publish, backup, validate, build payload) | **Private method** — `PublishEventToMaoAsync`, `PublishBackupToServiceBusAsync` |
| Logs belong to that action | Keep entry/exit logs **inside** the extracted method |
| Orchestration (`ProcessAsync`) | Guards → entry log → pipeline steps → call named private methods |
| Method signatures | **Single line** — class, constructor, and method declarations stay on one line unless longer than **160 characters** (positional `record` parameters excepted) |

**If you can name the action, extract it.** `ProcessAsync` should read like a table of contents.

---

## Example — processor orchestration

```csharp
public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(rawJson);

    var message = JsonSerializer.Deserialize<OrderUpdatedWebhookRequest>(rawJson);
    ArgumentNullException.ThrowIfNull(message);

    logger.LogInformation("Processing order updated for order {OrderId}.", message.Id);

    var envelope = await enrichmentPipeline.RunAsync(new OrderUpdatedEnrichmentEnvelope(message), cancellationToken);

    var maoPayload = mapper.Map(envelope);
    if (maoPayload is null)
    {
        logger.LogInformation("Skipping MAO update for order {OrderId}; no payload.", message.Id);
        return;
    }
    maoPayload.OrgId = envelope.Order!.ResolveOrgId(envelope.StoreLocation);

    var messageId = await PublishEventToMaoAsync(message, maoPayload, cancellationToken);
    await PublishBackupToServiceBusAsync(message, maoPayload, messageId, cancellationToken);
}

private async Task<string> PublishEventToMaoAsync(OrderUpdatedWebhookRequest message, UpdateNotesMaoModel maoPayload, CancellationToken cancellationToken)
{
    logger.LogInformation("Publishing to MAO for order {OrderId}.", message.Id);

    var messageId = await publisher.PublishAsync(maoPayload, cancellationToken);

    logger.LogInformation("Published to MAO for order {OrderId}; messageId={MessageId}.", message.Id, messageId);

    return messageId;
}

private async Task PublishBackupToServiceBusAsync(OrderUpdatedWebhookRequest message, UpdateNotesMaoModel maoPayload, string messageId, CancellationToken cancellationToken)
{
    logger.LogInformation("Sending backup for order {OrderId}.", message.Id);

    var backup = new MaoEventMessage { … };
    await serviceBusClient.SendMaoBackupAsync(backup, cancellationToken);

    logger.LogInformation("Sent backup for order {OrderId}.", message.Id);
}
```

`ProcessAsync` shows **what** happens; private methods show **how** each step is done.

---

## Do / don't

```csharp
// ✗ wrong — publish + backup inlined; ProcessAsync is hard to scan
public async Task ProcessAsync(…)
{
    …
    logger.LogInformation("Sending to MAO…");
    var messageId = await publisher.PublishAsync(maoPayload, cancellationToken);
    logger.LogInformation("Sent…");
    var backup = new MaoEventMessage { … };
    await serviceBusClient.SendMaoBackupAsync(backup, cancellationToken);
    logger.LogInformation("Backup sent…");
}

// ✓ correct — named steps
var messageId = await PublishEventToMaoAsync(message, maoPayload, cancellationToken);
await PublishBackupToServiceBusAsync(message, maoPayload, messageId, cancellationToken);
```

---

## Naming

| Pattern | Example |
|---------|---------|
| Verb + object + `Async` | `PublishEventToMaoAsync`, `PublishBackupToServiceBusAsync` |
| Build / fetch / validate | `BuildOutboundPayloadAsync`, `FetchOrderAsync` |
| Avoid | `HandleAsync`, `DoWork`, `ProcessStep2` — name the **action**, not the step number |

Use `private` unless the logic is reused across services (then consider enrichment step, base class, or extension).

---

## When inline is OK

- Single obvious line with no surrounding logs or setup (e.g. `ArgumentNullException.ThrowIfNull(x)`)
- Truly trivial glue where a method name would repeat the code (see [4_YAGNI.cs](principles/4_YAGNI.cs))

When in doubt, **extract** — readable orchestration beats a few extra lines.

---

## Checklist

- [ ] `ProcessAsync` / public entry methods read as a short sequence of named steps
- [ ] Distinct actions (publish, backup, send, map-and-send) are private methods with descriptive names
- [ ] Action-specific logs live inside the extracted method
- [ ] Private async methods use the `Async` suffix
