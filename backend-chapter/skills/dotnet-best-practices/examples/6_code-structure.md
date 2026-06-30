# Code structure and readability

> Example **6** — Named private methods, blank lines, comments, and single responsibility.

## Named private methods — extract descriptive steps

```csharp
// ✗ wrong — publish + backup inlined in ProcessAsync
var messageId = await publisher.PublishAsync(payload, cancellationToken);
logger.LogInformation("Sent…");
var backup = new MaoEventMessage { … };
await serviceBusClient.SendMaoBackupAsync(backup, cancellationToken);

// ✓ correct — ProcessAsync orchestrates; private methods own each action
var messageId = await PublishEventToMaoAsync(message, maoPayload, cancellationToken);
await PublishBackupToServiceBusAsync(message, maoPayload, messageId, cancellationToken);
```

See [10_named-private-methods.md](../reference/10_named-private-methods.md).

## Blank line before final return

```csharp
// ✗ wrong — no blank line before final return
public FooMaoModel Map(Envelope envelope)
{
    var result = Build(envelope);
    return result;
}

// ✓ correct — blank line before last return
public FooMaoModel Map(Envelope envelope)
{
    var result = Build(envelope);

    return result;
}

// ✗ wrong — no blank line before log/return block
public string Publish(Envelope envelope)
{
    var messageId = await publisher.PublishAsync(payload, cancellationToken);
    logger.LogInformation("Published {MessageId} for order {OrderId}.", messageId, envelope.OrderId);
    return messageId;
}

// ✓ correct — blank line before log; no blank line between log and return (exempt)
public string Publish(Envelope envelope)
{
    var messageId = await publisher.PublishAsync(payload, cancellationToken);

    logger.LogInformation("Published {MessageId} for order {OrderId}.", messageId, envelope.OrderId);
    return messageId;
}
```

## Comments

Default: **no comment** when the code and names already explain intent. Add comments or XML docs only when the reader cannot infer *why* or *how* from the code alone.

```csharp
// ✗ wrong — restates the obvious
// Get the order by id
var order = await client.GetOrderAsync(orderId, cancellationToken);

// ✓ correct — no comment; method name carries meaning
var order = await client.GetOrderAsync(orderId, cancellationToken);

// ✓ correct — inline comment for non-obvious business rule
// Refunds within 14 days skip restocking fee per policy XYZ-2024
if (order.DaysSincePurchase <= 14) { ... }
```

```csharp
// ✗ wrong — XML noise on obvious property
/// <summary>Gets or sets the order identifier.</summary>
public required string OrderId { get; init; }

// ✓ correct — no XML doc; name is self-explanatory
public required string OrderId { get; init; }
```

### When comments and XML docs are fine

| Situation | Use |
|-----------|-----|
| Non-obvious business rule or policy reference | Short `//` comment at the decision point |
| Documented assumption or external constraint | `//` — e.g. partner API quirk, timezone rule |
| Complex behaviour the type name alone cannot explain | `///` on the class or public method — state *what* it does, *how* it resolves, and link to detailed docs |
| Published `Api.Models` contract consumed via NuGet | `///` on public types/members consumers call from other repos |

```csharp
/// <summary>
/// Corrects OrderItemId on return products (from PredictSpring POS log) so they reference the correct MAO order line id,
/// since exchange flows may send a line id from an earlier order.
/// Resolution per product: keep current id if it already matches the MAO order line id, otherwise match by SGTIN (EPC),
/// then by SKU on lines with remaining returnable quantity.
/// See docs/PredictSpring_Exchange_LineCorrections.md for details and examples.
/// </summary>
public sealed class ReturnedProductOrderItemIdCorrectionService(IMaoOrderClient maoOrderClient)
{
    public void Correct(ReturnedProduct product, MaoOrderLines orderLines)
    {
        // … match by existing id, then SGTIN, then SKU — see linked doc
    }
}
```

```csharp
// ✓ correct — documents external behaviour the code works around
// Shopify returns presentment amounts as strings; MAO expects invariant-culture decimals.
var amount = decimal.Parse(line.Price!, CultureInfo.InvariantCulture);
```

Still **avoid** `///` boilerplate on every private helper — reserve XML for types and methods where a future reader genuinely needs the business context and resolution steps spelled out.

## Method and class size (single responsibility)

```csharp
// ✗ wrong — class does receive + enrich + map + publish + backup
public sealed class OrderProcessor
{
    public async Task RunAsync(...) { /* 200 lines, 5 concerns */ }
}

// ✓ correct — orchestrator delegates; each type one job
public sealed class OrderProcessorService(IEnrichmentPipeline pipeline, IOutboundMapper mapper, …)
{
    public async Task ProcessAsync(Envelope envelope, CancellationToken ct)
    {
        var enriched = await pipeline.RunAsync(envelope, ct);
        var payload = mapper.Map(enriched);
        if (payload is null) return;
        await publisher.PublishAsync(payload, ct);
    }
}
```

```csharp
// ✗ wrong — vague name, method does too much
public async Task Handle(Order order) { /* validate, enrich, map, publish, backup */ }

// ✓ correct — small methods, names describe each step
public async Task ProcessOrderAsync(Order order, CancellationToken cancellationToken)
{
    ValidateOrder(order);
    var enriched = await EnrichOrderAsync(order, cancellationToken);
    await PublishAndBackupAsync(enriched, cancellationToken);
}
```
