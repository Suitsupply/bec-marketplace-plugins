# Named private methods — extract descriptive steps

> Reference **10** — Orchestration methods read as steps; each action gets a named private method.

When a block of code performs **one identifiable action**, extract it into a **private method** with a **descriptive name**. The public/orchestration method should read as a short sequence of steps — not a wall of implementation detail.

---

## Rule

| Situation | Do |
|-----------|-----|
| Multi-step action with a clear purpose (publish, backup, validate, build payload) | **Private method** — `PublishOutboundEventAsync`, `SendBackupAsync` |
| Logs belong to that action | Keep entry/exit logs **inside** the extracted method |
| Orchestration (`ProcessAsync`, `HandleAsync`, …) | Guards → entry log → steps → call named private methods |
| Method signatures | **Single line** — class, constructor, and method declarations stay on one line unless longer than **160 characters** (positional `record` parameters excepted) |

**If you can name the action, extract it.** The public method should read like a table of contents.

---

## Example — orchestration

```csharp
public async Task ProcessAsync(InboundEvent message, CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(message);

    logger.LogInformation("Processing {EntityId}.", message.EntityId);

    var context = await enrichmentPipeline.RunAsync(new WorkflowContext(message), cancellationToken);

    var outbound = mapper.Map(context);
    if (outbound is null)
    {
        logger.LogInformation("Skipping publish for {EntityId}; no payload.", message.EntityId);
        return;
    }

    var messageId = await PublishOutboundEventAsync(message, outbound, cancellationToken);
    await SendBackupAsync(message, outbound, messageId, cancellationToken);
}

private async Task<string> PublishOutboundEventAsync(InboundEvent message, OutboundPayload outbound, CancellationToken cancellationToken)
{
    logger.LogInformation("Publishing outbound event for {EntityId}.", message.EntityId);

    var messageId = await publisher.PublishAsync(outbound, cancellationToken);

    logger.LogInformation("Published for {EntityId}; messageId={MessageId}.", message.EntityId, messageId);
    return messageId;
}

private async Task SendBackupAsync(InboundEvent message, OutboundPayload outbound, string messageId, CancellationToken cancellationToken)
{
    logger.LogInformation("Sending backup for {EntityId}.", message.EntityId);

    var backup = new BackupMessage { Payload = outbound, MessageId = messageId };
    await backupClient.SendAsync(backup, cancellationToken);

    logger.LogInformation("Sent backup for {EntityId}.", message.EntityId);
}
```

The public method shows **what** happens; private methods show **how** each step is done.

Integration-service example with real names: `shopifyintegration` → `OrderUpdatedProcessorService`.

---

## Do / don't

```csharp
// ✗ wrong — publish + backup inlined; orchestration is hard to scan
public async Task ProcessAsync(…)
{
    …
    var messageId = await publisher.PublishAsync(outbound, cancellationToken);
    var backup = new BackupMessage { … };
    await backupClient.SendAsync(backup, cancellationToken);
}

// ✓ correct — named steps
var messageId = await PublishOutboundEventAsync(message, outbound, cancellationToken);
await SendBackupAsync(message, outbound, messageId, cancellationToken);
```

---

## Naming

| Pattern | Example |
|---------|---------|
| Verb + object + `Async` | `PublishOutboundEventAsync`, `SendBackupAsync` |
| Build / fetch / validate | `BuildOutboundPayloadAsync`, `FetchRelatedEntityAsync` |
| Avoid | `HandleAsync`, `DoWork`, `ProcessStep2` — name the **action**, not the step number |

Use `private` unless the logic is reused across services (then consider a pipeline step, base class, or extension).

---

## When inline is OK

- Single obvious line with no surrounding logs or setup (e.g. `ArgumentNullException.ThrowIfNull(x)`)
- Truly trivial glue where a method name would repeat the code (see [4_YAGNI.cs](principles/4_YAGNI.cs))

When in doubt, **extract** — readable orchestration beats a few extra lines.

---

## Checklist

- [ ] Public entry methods read as a short sequence of named steps
- [ ] Distinct actions (publish, backup, send, map-and-send) are private methods with descriptive names
- [ ] Action-specific logs live inside the extracted method
- [ ] Private async methods use the `Async` suffix
