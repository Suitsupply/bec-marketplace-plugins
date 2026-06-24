# Enrichment and Mappers

## Layer context

**App works with domain models** (`App.Models`) only. Api converts HTTP DTOs → domain before calling App; Infra converts wire DTOs → domain before returning from clients. See [../../dotnet-best-practices/reference/layer-boundaries.md](../../dotnet-best-practices/reference/layer-boundaries.md).

## Flow — business logic before mapping

```
deserialize → guard → enrich (business logic) → map (shape translation only) → publish
```

**Mappers contain NO business logic.** They translate an already-prepared model into an outbound contract — field copy, formatting, nesting. All decisions, lookups, classification, and validation happen **before** `Map()` is called.

| Layer | Responsibility |
|-------|----------------|
| **Enrichment steps / pipeline** | Fetch data, apply business rules, populate envelope |
| **Validators / flow handlers** | Validate enriched state, orchestrate when to map/publish |
| **Mapper** | Pure structural translation: enriched model → outbound DTO |

## Preparing data for the mapper

Use one of two approaches so the mapper receives everything it needs without doing work:

### 1. Envelope model (most common)

`EnrichmentEnvelope<TSource>` or a **feature-specific envelope** (e.g. `OrderCreatedEnrichmentEnvelope`) holds the webhook/source plus enriched fields set by the pipeline:

```csharp
// App.Models/Enrichment/OrderCreatedEnrichmentEnvelope.cs
public record OrderCreatedEnrichmentEnvelope(OrderCreatedWebhookRequest Source)
{
    public Order? Order { get; set; }
    public StoreLocation? StoreLocation { get; set; }
    public IReadOnlyList<ClassifiedOrderLine> ClassifiedLines { get; set; } = [];
    public IReadOnlyList<string> AddressFieldErrors { get; set; } = [];
    public string? MaoOrderIdOverride { get; init; }
    public bool InvalidShippingAddressHold { get; set; }  // set by enrichment step, not mapper
}
```

Pipeline/steps populate the envelope. Mapper reads populated fields only.

### 2. Domain model

When mapping needs a dedicated aggregate, build it in enrichment or a domain service, then pass to the mapper:

```csharp
// Built by enrichment / domain service — all business rules applied here
public record OrderPublishContext(
    Order Order,
    StoreLocation StoreLocation,
    IReadOnlyList<ClassifiedOrderLine> Lines,
    bool HoldForInvalidAddress);

// Mapper — translation only
public CreateOrderMaoModel Map(OrderPublishContext context) => new()
{
    OrderId = context.Order.ResolveMaoOrderId(),
    OrgId = context.StoreLocation.OrgId,
    IsOnHold = context.HoldForInvalidAddress,
    // … field mapping only
};
```

Prefer an envelope when enrichment fields map 1:1 to the webhook flow; prefer a domain model when the publish shape is a distinct aggregate.

## Design — single responsibility

- **Pipeline** orchestrates step order only — no business rules inside the pipeline class
- **Each step** = one enrichment concern (fetch order, classify lines, validate address, …)
- **Each mapper** = one outbound shape; **no** client calls, **no** branching business rules

## Enrichment pipeline

- Location: `src/{ServiceName}.App/Enrichment/`
- `sealed` pipeline class wires `RunAsync(EnrichmentEnvelope<T>, CancellationToken)`
- Steps in `Enrichment/Steps/` — strongly typed params; business logic lives here
- Non-critical lookups: `LogWarning` + graceful fallback
- Unexpected errors: **throw** — bubble to Api; do not catch in App/Infra
- No per-step `LogInformation` — see [observability-logging.md](../../dotnet-best-practices/reference/observability-logging.md)

## Mappers

- Location: `src/{ServiceName}.App/Mappers/`
- Interface in `Mappers/…/Interfaces/`; implementation in parent `Mappers/…/` folder
- Input: fully enriched envelope or domain model — **never** raw webhook JSON alone
- Output: outbound DTO, or `null` when required enriched data is missing (precondition guard — processor skips publish)
- Allowed in mapper: null guards, field mapping, formatting via extension methods, delegating to line mappers
- **Not allowed in mapper:** HTTP/client calls, fetching missing data, business branching (e.g. "if Klarna then…"), classification, validation rules

Shared **structural** line mapping (no business rules) → `abstract` base. Model validation/extraction/sums → `{Type}Extensions`. Business classification → enrichment step.

## Generic envelope template

```csharp
public record EnrichmentEnvelope<TSource>
{
    public required TSource Source { get; init; }
    public Order? Order { get; set; }
}
```

Extend with feature-specific envelopes when the flow accumulates more enriched state.
