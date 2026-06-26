# Enrichment and outbound publish (integration services — optional)

> Reference **16** — Enrichment pipelines and Infra-side outbound mapping for integration microservices.

**Not chapter guidelines.** For event-driven integration services that enrich data before publishing outbound events. Query APIs, Web Apps, and simple CRUD services do not need enrichment pipelines.

**Chapter rules:** [2_layer-boundaries.md](2_layer-boundaries.md) (layer mapping, no `App/Mappers/`). This document covers integration-specific enrichment and outbound publish only.

See [14_integration-service-patterns.md](14_integration-service-patterns.md). Reference repo: `shopifyintegration` (legacy `App/Mappers/` — target state is Infra mapping).

## Flow — business logic before mapping

```
deserialize (Api) → guard → enrich (App business logic) → publish via IClient (domain) → map (Infra) → external system
```

**Mappers contain NO business logic.** They translate domain into a wire/publish contract — field copy, formatting, nesting. All decisions, lookups, classification, and validation happen **before** the Infra client maps.

| Layer | Responsibility |
|-------|----------------|
| **Enrichment steps / pipeline** | Fetch data, apply business rules, populate workflow context |
| **Validators / flow handlers** | Validate enriched state, orchestrate when to publish |
| **Infra client / mapper** | Pure structural translation: domain → wire/publish DTO; then send |

## Preparing data for publish

Use one of two approaches so the Infra mapper receives everything it needs without doing work:

### 1. Workflow context / envelope (common in integrations)

`WorkflowEnvelope<TSource>` or a **feature-specific context record** holds the inbound event plus enriched fields set by the pipeline:

```csharp
// App.Models/{Feature}/Models/FooWorkflowContext.cs
public record FooWorkflowContext(FooInboundEvent Source)
{
    public RelatedEntity? Related { get; set; }
    public IReadOnlyList<LineItem> ClassifiedLines { get; set; } = [];
    public bool HoldForReview { get; set; }  // set by enrichment step, not mapper
}
```

Pipeline/steps populate the context. App passes the populated context to `IOutboundPublisher.PublishAsync(context, …)`. Infra maps context → wire DTO internally.

### 2. Domain model

When publishing needs a dedicated aggregate, build it in enrichment or a domain service, then pass to the client:

```csharp
public record PublishContext(
    Order Order,
    Location Location,
    IReadOnlyList<LineItem> Lines,
    bool HoldForReview);

// App — after enrichment
await outboundPublisher.PublishAsync(context, cancellationToken);

// Infra/Clients/OutboundPublisher/OutboundPublisher.cs — maps inside client
public async Task<string> PublishAsync(PublishContext context, CancellationToken cancellationToken)
{
    var wireDto = mapper.Map(context);   // domain → wire DTO
    return await publisher.PublishMessageAsync(wireDto, …);
}
```

## Design — single responsibility

- **Pipeline** orchestrates step order only — no business rules inside the pipeline class
- **Each step** = one enrichment concern
- **Each Infra mapper** = one outbound wire shape; **no** client calls, **no** branching business rules

## Enrichment pipeline (when used)

- Location: `src/{ServiceName}.App/Enrichment/`
- `sealed` pipeline class wires `RunAsync` on a context record
- Steps in `Enrichment/Steps/` — strongly typed params; business logic lives here
- Non-critical lookups: `LogWarning` + graceful fallback
- Unrecoverable errors: **throw** — bubble to Api. Recoverable optional steps: catch specific exceptions, apply fallback, `LogWarning` if useful

## Outbound mapping (Infra)

- Location: `Infra/Clients/{Publisher}/Mappers/` (or inline in the client when trivial)
- `static class` by default (stateless, dependency-free) — call it directly, no interface, no DI registration; add an `I*` contract in `Mappers/Interfaces/` only when the mapper needs injected collaborators
- Input: domain model or enriched context from App — **never** raw inbound JSON
- Output: wire/publish DTO in `Infra/Clients/{Publisher}/Models/`
- **Not allowed in mapper:** HTTP/client calls, fetching missing data, business branching, classification, validation rules

For **HTTP response mapping** (non-integration), use `Api/Mappers/` — see [17_models-and-mappers.md](17_models-and-mappers.md).

Shared **structural** line mapping (no business rules) → `abstract` base in Infra. Model validation/extraction/sums → `{Type}Extensions` in App. Business classification → enrichment step.
