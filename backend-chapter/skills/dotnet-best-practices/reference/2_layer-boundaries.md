# Layer boundaries — DTO vs domain model

> Reference **2** — App uses domain models only; Api and Infra convert wire DTOs at their edges.

**The App layer works exclusively with domain models** (`App.Models`). Wire/transport shapes (DTOs) stop at the **Api** and **Infra** boundaries and are converted before App sees them.

Full reference for dependency separation. See also [principles/5_SeparationOfConcerns.cs](principles/5_SeparationOfConcerns.cs).

---

## Model types

| Type | Location | Purpose | Used by |
|------|----------|---------|---------|
| **Public API DTO** | `{ServiceName}.Api.Models` | HTTP request/response contracts (NuGet) | Api ↔ external callers |
| **Infra wire DTO** | `Infra/Clients/{Name}/Models/` | External API JSON/XML shapes | Infra only — **never** returned through App interfaces |
| **Domain model** | `{ServiceName}.App.Models` | Business entities, value objects, feature types | **App** (and passed *through* Api/Infra after conversion) |

---

## Data flow

Summary (same three flows as **dotnet-best-practices** hub):

```
Inbound:  HTTP → Api (map: request DTO → domain) → App
Response: App → Api (map: domain → response DTO) → HTTP
Outgoing: App → Infra (map: domain → wire request DTO) → external API
          → Infra (map: wire response DTO → domain) → App
```

| Term | Location | Used at |
|------|----------|---------|
| **Request / response DTO** | `Api.Models` | HTTP boundary (Api only) |
| **Wire request / response DTO** | `Infra/Clients/{Name}/Models/` | External API boundary (Infra only) |
| **Domain model** | `App.Models` | App — and passed through Api/Infra after conversion |

### Inbound HTTP (Api → App)

```
Inbound: HTTP → Api (map: request DTO → domain) → App
```

Detail:

```
External caller
  → HTTP body (JSON)
  → Api deserializes to request DTO (Api.Models)
  → Api mapper converts request DTO → domain model (App.Models)
  → App service / receiver / processor (domain only)
```

### Outbound HTTP (App → Api)

```
Response: App → Api (map: domain → response DTO) → HTTP
```

Detail:

```
App service returns domain model (App.Models)
  → Api mapper converts domain → response DTO (Api.Models)
  → HTTP response
```

Example: `GetOrderFunction` calls `IGetOrderService` (returns `Order` domain), then the static `GetOrderMapper.ToDto` maps `Order` → `GetOrderResponse` (Api.Models).

### Outbound call (App → Infra → external API)

```
Outgoing: App → Infra (map: domain → wire request DTO) → external API
          → Infra (map: wire response DTO → domain) → App
```

Detail:

```
App calls IClient interface (domain types in signature)
  → Infra maps domain → wire request DTO
  → Infra client calls external API
  → Infra deserializes wire response DTO (Infra/Clients/.../Models/)
  → Infra maps wire response DTO → domain model (App.Models)
  → returns domain to App
```

**App client interfaces must never expose wire DTOs** in parameters or return types.

### Outbound publish (App → Infra → external system)

After enrichment (integration services), App calls a client interface with **domain types only**. **Infra** maps domain → outbound wire/publish shape and sends it — same boundary rule as HTTP clients.

```
App enrichment / service (domain only)
  → IClient.PublishAsync(domain, …)
  → Infra maps domain → wire/publish DTO
  → Infra publishes to external system
```

---

## Layer responsibilities

| Layer | Receives | Converts | Passes to App |
|-------|----------|----------|---------------|
| **Api** | Request/response DTOs (`Api.Models`) | Api `Mappers/` — request/response DTO ↔ domain | **Domain models only** |
| **App** | Domain models | Business logic, enrichment | N/A — core layer |
| **Infra** | Domain from App; wire DTOs from external APIs | Infra client mapping — domain → wire request; wire response → domain | **Domain models only** (via `IClient` return types) |

---

## Folder conventions

```
{ServiceName}.Api/
  Mappers/                    # Boundary mappers: domain ↔ Api.Models DTO
  Functions/                  # Deserialize/map inbound; call App with domain

{ServiceName}.Api.Models/       # Public HTTP contracts: v1/{Feature}/Requests/, Responses/, Models/

{ServiceName}.App/
  Clients/
    Interfaces/               # One I* per downstream component — domain types only
  Services/                   # Works with App.Models only
  Enrichment/                 # Integration only — business logic before publish

{ServiceName}.App.Models/       # Domain models: {Feature}/Models/

{ServiceName}.Infra/
  Clients/{Name}/             # One folder per downstream — see downstream-clients.md
    Models/                   # Wire DTOs — internal to Infra
    {Name}Client.cs           # Maps domain → wire request; wire response → domain
    Mappers/                  # Optional: dedicated wire-DTO → domain mappers
```

---

## Api boundary example

```csharp
// Api/Functions/Person/FooReceiver.cs
public sealed class FooReceiver(IFooReceiverService service)
{
    public async Task<IActionResult> Run(HttpRequest request, CancellationToken cancellationToken)
    {
        var requestDto = await request.ReadFromJsonAsync<FooCreatedRequest>(cancellationToken);
        ArgumentNullException.ThrowIfNull(requestDto);

        var domain = FooMapper.ToDomain(requestDto);   // DTO → domain at Api boundary (static mapper)
        await service.ProcessAsync(domain, cancellationToken);
        return new AcceptedResult();
    }
}

// App/Services/PersonServices.cs — domain only
public Task ProcessAsync(FooCreatedWebhook domain, CancellationToken cancellationToken) { … }
```

For Service Bus / queue processors, deserialize and map at the **processor Function** before calling App — same boundary rule as HTTP receivers.

```csharp
// Api/Functions/Person/FooProcessor.cs — Service Bus body → domain at Api boundary
var domain = JsonSerializer.Deserialize<FooCreatedWebhook>(message.Body.ToString());
ArgumentNullException.ThrowIfNull(domain);
await processorService.ProcessAsync(domain, cancellationToken);

// App/Services/PersonServices.cs — domain only
public Task ProcessAsync(FooCreatedWebhook message, CancellationToken cancellationToken) { … }
```

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
| `Infra/Clients/{Name}/Mappers/` or client inline | Domain ↔ wire request/response/publish DTO | No — boundary translation only |

Business logic runs in **App services, enrichment steps, validators** — before Infra maps domain to wire shapes and after domain is assembled from Api/Infra boundaries.
