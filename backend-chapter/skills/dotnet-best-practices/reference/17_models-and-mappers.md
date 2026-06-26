# Models and mappers

> Reference **17** — Record shapes and Api/Infra mapper templates.

**Chapter rules:** [2_layer-boundaries.md](2_layer-boundaries.md), [3_interfaces.md](3_interfaces.md). This document is the **copy-paste template** for new models and mappers.

Mappers translate shape at **layer boundaries only** — no business logic, no client calls, no classification. **Api** and **Infra** map; **App does not** (`App/Mappers/` is not used).

| Location | Maps |
|----------|------|
| `Api/Mappers/` | Domain ↔ `Api.Models` HTTP contracts |
| `Infra/Clients/{Name}/Mappers/` | Domain ↔ wire request/response/publish DTO (or inline in client) |

Integration outbound publish (App enriches → Infra maps): [16_enrichment-and-mappers.md](16_enrichment-and-mappers.md).

---

## `App.Models` — domain records

Positional `record`; **one parameter per line**. Domain models typically **do not** use `JsonPropertyName`.

```csharp
// App.Models/{Feature}/Models/OrderLine.cs
namespace {ServiceName}.App.Models.Order.Models;

public record OrderLine(
    string Sku,
    int Quantity,
    decimal PresentmentAmount);
```

Webhook / inbound event domain shapes: `App.Models/{Feature}/Models/Webhooks/` — see [6_webhook-model.cs](../examples/production/6_webhook-model.cs).

---

## `Api.Models` — HTTP transport contracts

Positional `record` in `v1/{Feature}/Requests|Responses|Models/`; use `[property: JsonPropertyName("…")]` when JSON names differ from property names.

```csharp
public record FooCreatedRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);
```

One-parameter-per-line applies to **all** positional records:

```csharp
public record MoneyAmount(
    string? Amount,
    string? CurrencyCode);
```

---

## Settings records

Non-positional `record` with `init` properties in `Infra/.../Settings/` or `Api/.../Settings/`. Every settings record needs a validator + `ValidateOnStart()` — [12_configuration-validation.md](12_configuration-validation.md).

```csharp
[ExcludeFromCodeCoverage]
public record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
    public string ClientId { get; init; } = default!;
}
```

---

## Workflow context (integration only)

Feature-specific context record populated by an enrichment pipeline before outbound publish — [16_enrichment-and-mappers.md](16_enrichment-and-mappers.md).

---

## Value objects / domain results

Use a positional `record` for small, pure value results:

```csharp
public record ShippingChargeResult(decimal Amount, string TaxCode);
```

---

## Api mapper template

Boundary mappers are **pure, stateless, dependency-free** transformations — so make them a **`static class` with `static` methods**. No interface, no DI registration; call them directly (`FooMapper.ToDomain(request)`). Mappers live under `Api/Mappers/v1/` (or unversioned `Api/Mappers/` when the host has a single API surface). **Do not** encode the API version in the type name.

```csharp
// Api/Mappers/v1/FooMapper.cs
namespace {ServiceName}.Api.Mappers.v1;

public static class FooMapper
{
    public static FooCreatedWebhook ToDomain(FooCreatedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new FooCreatedWebhook(
            Id: request.Id,
            Name: request.Name);
    }

    public static GetFooResponse ToDto(Foo domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new GetFooResponse(
            Id: domain.Id,
            Name: domain.Name);
    }
}
```

**When to use an interface + instance instead:** only when a mapper genuinely needs injected collaborators (rare for a boundary mapper — e.g. a clock or a lookup service). Then make it an instance `sealed class` implementing an `I*` contract in `…/Interfaces/` and register it (`AddTransient`). Default to `static`.

**Rules:**

- `static class` + `static` methods by default — no interface, no DI registration; only inject when the mapper has real dependencies
- App service returns domain; Api maps to `Api.Models` before HTTP response
- Throw `ArgumentNullException` on null input — mappers translate shape, they don't decide flow
- One mapper, one output shape (SRP)
- Blank line before the method's final `return`
- Pure structural shared mapping → shared `static` helper — not for business rules
- Pure model logic → `{Type}Extensions` — not `*Helper` classes
