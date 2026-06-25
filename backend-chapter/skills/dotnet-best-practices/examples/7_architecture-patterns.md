# Architecture patterns

> Example **7** — Clients, interfaces, extensions, layer boundaries, mappers, and DRY.

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

See [4_downstream-clients.md](../reference/4_downstream-clients.md).

## Interfaces — `Interfaces/` folders

```csharp
// ✗ wrong — interface beside implementation
// App/Services/Receivers/IFooReceiverService.cs
// App/Services/Receivers/FooReceiverService.cs

// ✓ correct — interface in Interfaces/ subfolder
// App/Services/Receivers/Interfaces/IReceiverService.cs
// App/Services/Receivers/FooReceiverService.cs
```

See [3_interfaces.md](../reference/3_interfaces.md).

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

See [9_extensions-vs-helpers.md](../reference/9_extensions-vs-helpers.md).

## DRY — abstract classes, factories, pipelines

```csharp
// ✗ wrong — duplicated receiver flow in every webhook type
public sealed class FooReceiverService { /* deserialize, backup, queue — copied */ }
public sealed class BarReceiverService { /* same 40 lines */ }

// ✓ correct — template method base; subclass only defines what differs
public sealed class FooReceiverService(…) : ReceiverServiceBase<FooWebhookRequest>(…)
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
public Task ProcessAsync(FooCreatedRequest request, CancellationToken ct);

// ✗ wrong — IClient exposes Infra wire DTO
public interface IFooClient { Task<FooOrderWireDto?> GetAsync(string id, CancellationToken ct); }

// ✓ correct — Api maps DTO → domain before App
var domain = webhookMapper.ToDomain(requestDto);
await receiverService.ProcessAsync(domain, cancellationToken);

// ✓ correct — Infra maps wire → domain before return
var wire = await response.Content.ReadFromJsonAsync<FooOrderWireDto>(ct);
return wire?.ToDomain();
```

See [2_layer-boundaries.md](../reference/2_layer-boundaries.md).

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

// ✓ correct — enrichment prepares envelope; App passes domain to client; Infra maps shape
// (in processor)
var envelope = new FooEnrichmentEnvelope(message);
await enrichmentPipeline.RunAsync(envelope, cancellationToken);
await publisher.PublishAsync(envelope, cancellationToken);

// (in Infra/Clients/Publisher/Mappers/FooOutboundMapper.cs)
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
// ✗ wrong — same line-mapping logic copied in three Infra mappers
// ✓ correct — abstract base for shared logic (in Infra)
public abstract class OutboundLineProductMapperBase { protected FooLine MapCore(...) { … } }
```
