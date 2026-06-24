# Good vs Bad — Cross-Cutting C# Patterns

## Async

```csharp
// ✗ wrong — blocks the thread
var result = service.ProcessAsync(id).Result;

// ✓ correct
var result = await service.ProcessAsync(id, cancellationToken);
```

```csharp
// ✗ wrong — CancellationToken not forwarded
public async Task RunAsync(CancellationToken cancellationToken)
{
    await inner.ProcessAsync(); // drops token
}

// ✓ correct
public async Task RunAsync(CancellationToken cancellationToken)
{
    await inner.ProcessAsync(cancellationToken);
}
```

## Nullability

```csharp
// ✗ wrong — manual null check
if (request == null) throw new ArgumentNullException(nameof(request));

// ✓ correct
ArgumentNullException.ThrowIfNull(request);
```

```csharp
// ✗ wrong — unnecessary null-forgiving
var name = order!.Name; // no prior guard

// ✓ correct — guard first, then use
ArgumentNullException.ThrowIfNull(order);
var name = order.Name;
```

## Dependency injection

```csharp
// ✗ wrong — service locator in business code
var client = serviceProvider.GetRequiredService<IFooClient>();

// ✓ correct — constructor injection
public sealed class FooService(IFooClient fooClient)
{
    public Task ProcessAsync(CancellationToken ct) => fooClient.GetAsync(ct);
}
```

## Configuration validation (fail early)

```csharp
// ✗ wrong — binds config without startup validation; fails at runtime on first use
services.Configure<FooSettings>(config.GetSection(nameof(FooSettings)));

// ✓ correct — FluentValidation + ValidateOnStart; host refuses to start
services.AddOptions<FooSettings>()
    .Bind(config.GetSection(nameof(FooSettings)))
    .ValidateOnStart();
services.AddSingleton<IValidateOptions<FooSettings>>(
    _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));
```

## Code coverage exclusions

```csharp
// ✓ correct — settings record has no logic; exclude from coverage
[ExcludeFromCodeCoverage]
internal sealed record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
}

// ✓ correct — Infra client implementation (covered by component/integration tests)
[ExcludeFromCodeCoverage]
internal sealed class FooClient(HttpClient httpClient) : IFooClient { ... }

// ✗ wrong — App validator has rules to test; must not be excluded
[ExcludeFromCodeCoverage]
internal sealed class FooSettingsValidator : AbstractValidator<FooSettings> { ... }
```

## Error handling / logging

```csharp
// ✗ wrong — timestamp only; no useful correlation for Application Insights
logger.LogInformation("{Function} executed at {Time}", nameof(FooCreatedReceiver), DateTime.UtcNow);

// ✓ correct — Api HTTP entry (before body read): Function name is enough at this boundary
logger.LogInformation("{Function} invoked.", nameof(FooCreatedReceiver));

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

See [named-private-methods.md](../reference/named-private-methods.md).

## Downstream clients — one per component

```csharp
// ✗ wrong — god-client for multiple downstreams
public interface IIntegrationClient
{
    Task<Order?> GetShopifyOrderAsync(string id, CancellationToken ct);
    Task PublishToMaoAsync(MaoEventMessage msg, CancellationToken ct);
}

// ✓ correct — separate client per downstream; step/service composes
public class FetchOrderStep(
    IShopifyGraphQLClient shopifyClient,
    IOrderHistoryClient orderHistoryClient) { … }
```

See [downstream-clients.md](../reference/downstream-clients.md).

## Interfaces — `Interfaces/` folders

```csharp
// ✗ wrong — interface beside implementation
// App/Services/Receivers/IFooCreatedReceiverService.cs
// App/Services/Receivers/FooCreatedReceiverService.cs

// ✓ correct — interface in Interfaces/ subfolder
// App/Services/Receivers/Interfaces/IReceiverService.cs
// App/Services/Receivers/FooCreatedReceiverService.cs
```

See [interfaces.md](../reference/interfaces.md).

## Extensions — not Helper classes

```csharp
// ✗ wrong — grab-bag Helper
public static class OrderHelper
{
    public static string? GetAlternateId(Order order) => …;
    public static decimal SumLines(IReadOnlyList<OrderLine> lines) => …;
}

// ✓ correct — extensions on the model
public static class OrderExtensions
{
    public static string? ResolveAlternateOrderId(this Order order) { … }
    public static decimal SumLineTotals(this IReadOnlyList<OrderLine> lines) { … }
}

var id = order.ResolveAlternateOrderId();
var total = lines.SumLineTotals();
```

See [extensions-vs-helpers.md](../reference/extensions-vs-helpers.md).

## Exception handling — bubble to Api

```csharp
// ✗ wrong — catch and log in App service
public async Task ProcessAsync(Envelope envelope, CancellationToken ct)
{
    try { await pipeline.RunAsync(envelope, ct); }
    catch (Exception ex) { logger.LogError(ex, "Failed"); throw; }
}

// ✓ correct — no catch in App; exception bubbles to Api Function
public async Task ProcessAsync(Envelope envelope, CancellationToken ct)
{
    await pipeline.RunAsync(envelope, ct);
}

// ✓ correct — Api Function logs and rethrow (or HTTP 500 / retry scheduler)
catch (Exception ex)
{
    logger.LogError(ex, "{Function} message {MessageId} failed.", nameof(FooProcessor), messageId);
    throw;
}
```

See [exception-handling.md](../reference/exception-handling.md).

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

```csharp
// ✗ wrong — restates the obvious
// Get the order by id
var order = await client.GetOrderAsync(orderId, cancellationToken);

// ✓ correct — no comment; method name carries meaning
var order = await client.GetOrderAsync(orderId, cancellationToken);

// ✓ correct — comment documents non-obvious business rule
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

## DRY — abstract classes, factories, pipelines

```csharp
// ✗ wrong — duplicated receiver flow in every webhook type
public sealed class FooReceiverService { /* deserialize, backup, queue — copied */ }
public sealed class BarReceiverService { /* same 40 lines */ }

// ✓ correct — template method base; subclass only defines what differs
public sealed class FooCreatedReceiverService(…) : ReceiverServiceBase<FooWebhookRequest>(…)
{
    protected override EventType EventType => EventType.FooCreated;
    protected override string BuildBlobPath(FooWebhookRequest model) => …;
}
```

```csharp
// ✗ wrong — switch grows with every payment scenario
public async Task ProcessAsync(Transaction tx)
{
    if (tx.IsKlarna && tx.IsAuth) { /* 30 lines */ }
    else if (tx.IsKlarna && tx.IsCapture) { /* 30 lines */ }
    // …
}

// ✓ correct — factory picks handler; one class per scenario (strategy)
var handler = flowHandlerFactory.Resolve(tx);
await handler.HandleAsync(tx, cancellationToken);
```

## Layer boundaries (DTO vs domain)

```csharp
// ✗ wrong — App service accepts Api HTTP DTO
public Task ProcessAsync(FooCreatedRequestDto dto, CancellationToken ct);

// ✗ wrong — IClient exposes Infra wire DTO
public interface IFooClient { Task<FooOrderWireDto?> GetAsync(string id, CancellationToken ct); }

// ✓ correct — Api maps DTO → domain before App
var domain = webhookMapper.ToDomain(requestDto);
await receiverService.ProcessAsync(domain, cancellationToken);

// ✓ correct — Infra maps wire → domain before return
var wire = await response.Content.ReadFromJsonAsync<FooOrderWireDto>(ct);
return wire?.ToDomain();
```

See [layer-boundaries.md](../reference/layer-boundaries.md).

## Mappers — no business logic

```csharp
// ✗ wrong — mapper fetches data and applies business rules
public FooMaoModel? Map(FooWebhookRequest source)
{
    var order = await shopifyClient.GetOrderAsync(source.Id);  // fetch in mapper
    if (order.Total > 1000 && order.IsKlarna())               // business rule in mapper
        return BuildHoldOrder(order);
    return BuildNormalOrder(order);
}

// ✓ correct — enrichment prepares envelope; mapper translates shape only
// (in processor)
var envelope = new FooEnrichmentEnvelope(message);
await enrichmentPipeline.RunAsync(envelope, cancellationToken);
var payload = mapper.Map(envelope);

// (in mapper)
public FooMaoModel? Map(FooEnrichmentEnvelope envelope)
{
    ArgumentNullException.ThrowIfNull(envelope);
    if (envelope.Order is null) return null;
    return new FooMaoModel
    {
        OrderId = envelope.ResolvedMaoOrderId,
        IsOnHold = envelope.HoldForInvalidAddress,
        Lines = envelope.ClassifiedLines.Select(lineMapper.Map).ToList(),
    };
}
```

```csharp
// ✗ wrong — same line-mapping logic copied in three mappers
// ✓ correct — abstract base for shared logic
public abstract class OutboundLineProductMapperBase { protected FooLine MapCore(...) { … } }
```

## Immutability

```csharp
// ✗ wrong — mutates parameter
void Enrich(MyModel model) { model.Field = "value"; }

// ✓ correct — return new object (record with expression)
FooWebhookRequest Enrich(FooWebhookRequest request) => request with { Status = "processed" };
```

## Readability over optimisation

```csharp
// ✗ wrong — premature micro-optimisation hurts readability
var sb = new StringBuilder();
for (var i = 0; i < ids.Length; i++) sb.Append(ids[i].ToString("X"));
return sb.ToString();

// ✓ correct — clear unless profiling shows this path is hot
return string.Join(',', ids);
```

## LINQ formatting

```csharp
// ✓ correct — source on new line, one operator per line
var raw =
    metafields?.Edges?
        .Select(e => e.Node)
        .OfType<Metafield>()
        .FirstOrDefault(n => n.Namespace == AdyenNamespace && n.Key == TransactionDetailsKey)
        ?.Value;

// ✗ wrong — long chain on one line
var raw = metafields?.Edges?.Select(e => e.Node).OfType<Metafield>().FirstOrDefault(n => n.Namespace == ns && n.Key == key)?.Value;

// ✗ wrong — multiple operators on one line
var raw = metafields?.Edges?.Select(e => e.Node).OfType<Metafield>()
    .FirstOrDefault(n => n.Namespace == ns && n.Key == key)?.Value;
```

## Naming

```csharp
// ✗ wrong — no Async suffix on async method
public Task Process(CancellationToken ct) => ...

// ✓ correct
public Task ProcessAsync(CancellationToken cancellationToken) => ...
```

## Performance

```csharp
// ✗ wrong — multiple enumerations
if (items.Any() && items.Count() > 1) { ... }

// ✓ correct — materialize once when needed twice
var list = items.ToList();
if (list.Count > 1) { ... }
```
