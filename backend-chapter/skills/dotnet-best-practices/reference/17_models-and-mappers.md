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

Positional `record` in `{Feature}/Transport/Requests|Responses|Models/`; use `[property: JsonPropertyName("…")]` when JSON names differ from property names.

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

Interface and implementation live under `Api/Mappers/v1/` (or unversioned `Api/Mappers/` when the host has a single API surface). **Do not** encode the API version in the type name.

```csharp
// Api/Mappers/v1/Interfaces/IFooWebhookMapper.cs
namespace {ServiceName}.Api.Mappers.v1.Interfaces;

public interface IFooWebhookMapper
{
    FooCreatedWebhook ToDomain(FooCreatedRequest request);
}

// Api/Mappers/v1/FooWebhookMapper.cs
namespace {ServiceName}.Api.Mappers.v1;

public sealed class FooWebhookMapper : IFooWebhookMapper
{
    public FooCreatedWebhook ToDomain(FooCreatedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new FooCreatedWebhook(
            Id: request.Id,
            Name: request.Name);
    }
}
```

Query/response mappers follow the same pattern — version in folder, not in class name:

```csharp
// Api/Mappers/Interfaces/IGetFooMapper.cs
namespace {ServiceName}.Api.Mappers.Interfaces;

public interface IGetFooMapper
{
    GetFooResponse Map(Foo domain);
}

// Api/Mappers/GetFooMapper.cs
namespace {ServiceName}.Api.Mappers;

public sealed class GetFooMapper : IGetFooMapper
{
    public GetFooResponse Map(Foo domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new GetFooResponse(
            Id: domain.Id,
            Name: domain.Name);
    }
}
```

**Rules:**

- Interface in `…/Interfaces/`; implementation in parent folder
- App service returns domain; Api maps to `Api.Models` before HTTP response
- Return `null` when required input is missing (caller decides early exit)
- One mapper, one output shape (SRP)
- Blank line before the method's final `return`
- Pure structural shared mapping → `abstract` base — not for business rules
- Pure model logic → `{Type}Extensions` — not `*Helper` classes
