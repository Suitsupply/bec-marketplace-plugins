# Layer boundaries — DTO vs domain model

**The App layer works exclusively with domain models** (`App.Models`). Wire/transport shapes (DTOs) stop at the **Api** and **Infra** boundaries and are converted before App sees them.

Full reference for dependency separation. See also [principles/5_SeparationOfConcerns.cs](principles/5_SeparationOfConcerns.cs).

---

## Model types

| Type | Location | Purpose | Used by |
|------|----------|---------|---------|
| **Public API DTO** | `{ServiceName}.Api.Models` | HTTP request/response contracts (NuGet) | Api ↔ external callers |
| **Infra wire DTO** | `Infra/Clients/{Name}/Models/` | External API JSON/XML shapes | Infra only — **never** returned through App interfaces |
| **Domain model** | `{ServiceName}.App.Models` | Business entities, webhooks, envelopes, value objects | **App** (and passed *through* Api/Infra after conversion) |

---

## Data flow

### Inbound HTTP (Api → App)

```
External caller
  → HTTP body (JSON)
  → Api deserializes to request DTO (Api.Models or Api-local shape)
  → Api mapper converts DTO → domain model (App.Models)
  → App service / receiver / processor (domain only)
```

### Outbound HTTP (App → Api → caller)

```
App service returns domain model (App.Models)
  → Api mapper converts domain → response DTO (Api.Models)
  → HTTP response
```

Example: `GetOrderFunction` calls `IGetOrderService` (returns `Order` domain), then `IGetOrderMapper` maps `Order` → `GetOrderResponse` (Api.Models).

### Infra client (App → Infra → external API)

```
App calls IClient interface (domain types in signature)
  → Infra client calls external API
  → Infra deserializes to wire DTO (Infra/Clients/.../Models/)
  → Infra maps wire DTO → domain model (App.Models)
  → returns domain to App
```

**App client interfaces must never expose Infra wire DTOs** in parameters or return types.

### Outbound publish (App → external system)

After enrichment, **App mappers** translate domain/envelope → outbound publish shape (e.g. MAO model). That is an App-layer mapping to an external contract — not an Infra wire DTO leak.

---

## Layer responsibilities

| Layer | Receives | Converts | Passes to App |
|-------|----------|----------|---------------|
| **Api** | HTTP DTOs (`Api.Models`, request bodies) | Api `Mappers/` — DTO ↔ domain | **Domain models only** |
| **App** | Domain models | Business logic, enrichment | N/A — core layer |
| **Infra** | Wire DTOs from external APIs | Infra client mapping — wire DTO → domain | **Domain models only** (via `IClient` return types) |

---

## Folder conventions

```
{ServiceName}.Api/
  Mappers/                    # Boundary mappers: domain ↔ Api.Models DTO
  Functions/                  # Deserialize/map inbound; call App with domain

{ServiceName}.Api.Models/       # Public HTTP contracts: {Feature}/Requests/, Responses/, Models/

{ServiceName}.App/
  Clients/
    Interfaces/               # One I* per downstream component — domain types only
  Services/                   # Works with App.Models only
  Mappers/                    # Domain/envelope → outbound publish shapes (no business logic)

{ServiceName}.App.Models/       # Domain models — App's language

{ServiceName}.Infra/
  Clients/{Name}/             # One folder per downstream — see downstream-clients.md
    Models/                   # Wire DTOs — internal to Infra
    {Name}Client.cs           # Maps wire DTO → domain before return
    Mappers/                  # Optional: dedicated wire-DTO → domain mappers
```

---

## Api boundary example

```csharp
// Api/Functions/Receivers/FooCreatedReceiver.cs
public sealed class FooCreatedReceiver(IFooCreatedReceiverService service, IFooWebhookMapper webhookMapper)
{
    public async Task<IActionResult> Run(HttpRequest request, CancellationToken cancellationToken)
    {
        var requestDto = await request.ReadFromJsonAsync<FooCreatedRequestDto>(cancellationToken);
        ArgumentNullException.ThrowIfNull(requestDto);

        var domain = webhookMapper.ToDomain(requestDto);   // DTO → domain at Api boundary
        await service.ProcessAsync(domain, cancellationToken);
        return new AcceptedResult();
    }
}

// App/Services/Receivers/FooCreatedReceiverService.cs — domain only
public Task ProcessAsync(FooCreatedWebhook domain, CancellationToken cancellationToken) { … }
```

For Service Bus / queue processors that today accept `rawJson`, prefer moving deserialization + DTO→domain mapping to **Api** (Function) so App receives domain models.

---

## Infra boundary example

```csharp
// App/Clients/Interfaces/IFooClient.cs — domain types only
public interface IFooClient
{
    Task<FooOrder?> GetOrderAsync(string id, CancellationToken cancellationToken);
}

// Infra/Clients/FooClient/FooClient.cs
internal sealed class FooClient(HttpClient httpClient) : IFooClient
{
    public async Task<FooOrder?> GetOrderAsync(string id, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"orders/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        var wireDto = await response.Content.ReadFromJsonAsync<FooOrderWireDto>(cancellationToken);
        return wireDto?.ToDomain();   // wire DTO → App.Models domain before return
    }
}

// Infra/Clients/FooClient/Models/FooOrderWireDto.cs — never referenced from App
internal sealed record FooOrderWireDto { … }
```

---

## Dependency rules (strict)

| Project | May reference | Must NOT reference |
|---------|---------------|-------------------|
| **App** | `App.Models` | `Api`, `Api.Models`, `Infra`, Infra wire DTOs |
| **App.Models** | Minimal shared NuGets | `Api`, `Infra` |
| **Infra** | `App`, `App.Models`, own `Infra/.../Models` | `Api`, `Api.Models` |
| **Api** | `App`, `Infra`, `Api.Models`, `App.Models` (for mapping) | — |
| **Api.Models** | Standalone | `App`, `Infra` |

**Blocking in code review:** App service or enrichment step taking `Api.Models` or Infra wire DTO as parameter; `IClient` interface returning Infra `Models/` type.

---

## Mapper types (do not confuse)

| Mapper location | Converts | Contains business logic? |
|-----------------|----------|--------------------------|
| `Api/Mappers/` | Domain ↔ `Api.Models` request/response DTO | No — boundary translation only |
| `Infra/.../Mappers/` or client inline | Wire DTO → domain | No — boundary translation only |
| `App/Mappers/` | Enriched domain/envelope → outbound publish shape | No — see [enrichment-and-mappers.md](../../write-src-code/reference/enrichment-and-mappers.md) |

Business logic runs in **App services, enrichment steps, validators** — before App outbound mappers and after domain is assembled from Api/Infra boundaries.
