# Interfaces — always in `Interfaces/` folders

> Reference **3** — Every `I*` contract lives in an `Interfaces/` subfolder next to its implementation.

Every interface (`I*`) lives in an **`Interfaces/`** subfolder next to its implementations. **Never** place interface and implementation in the same folder.

Namespace mirrors the folder: `{ServiceName}.<Layer>.<Feature>.Interfaces`.

---

## Rule

| Do | Don't |
|----|-------|
| `Services/Interfaces/IGetPersonService.cs` | `Services/IGetPersonService.cs` next to `GetPersonService.cs` |
| `Clients/Interfaces/IFooClient.cs` | `App/Clients/IFooClient.cs` without `Interfaces/` |

> **Mappers are an exception:** boundary mappers are stateless and dependency-free, so they are `static class`es with **no interface** — see [17_models-and-mappers.md](17_models-and-mappers.md). Only when a mapper needs injected collaborators does it get an `I*` contract in `Mappers/Interfaces/`.

**Implementations** sit in the **parent** folder (sibling of `Interfaces/`):

```
Services/
├── Interfaces/
│   ├── IGetPersonService.cs
│   └── IPersonRequestedProcessorService.cs
└── PersonServices.cs
```

---

## Locations by layer

| Contract | Path | Namespace example |
|----------|------|-----------------|
| External client | `App/Clients/Interfaces/` | `{ServiceName}.App.Clients.Interfaces` |
| App Services | `App/Services/Interfaces/` | `{ServiceName}.App.Services.Interfaces` |
| Api boundary mapper (only if injected) | `Api/Mappers/Interfaces/` | `{ServiceName}.Api.Mappers.Interfaces` |
| Infra boundary mapper (only if injected) | `Infra/Clients/{Name}/Mappers/Interfaces/` | `{ServiceName}.Infra.Clients.{Name}.Mappers.Interfaces` |

**Infra** does not define App-facing interfaces — it **implements** interfaces from `App/Clients/Interfaces/`.

---

## File conventions

| Pattern | Detail |
|---------|--------|
| **One interface per file** | Default — `IFooClient.cs`, `IGetOrderService.cs` |
| **Marker interfaces** | Add to the base file — e.g. `IReceiverService.cs` contains `IReceiverService` plus `IFooReceiverService : IReceiverService` |
| **Shared enums** | Co-locate in `Clients/Interfaces/` when tied to client contracts — e.g. `EventType.cs` |
| **Registration** | DI binds `I*` from `Interfaces/` to implementation in parent folder |

```csharp
// App/Services/Interfaces/IGetPersonService.cs
namespace {ServiceName}.App.Services.Interfaces;

public interface IGetPersonService
{
    Task<Person> GetPersonAsync(int id, CancellationToken cancellationToken = default);
}
```

```csharp
// App/Services/PersonServices.cs
using {ServiceName}.App.Services.Interfaces;

namespace {ServiceName}.App.Services;

public class GetPersonService(…) : IGetPersonService
{
    …
}
```

```csharp
// App/Services/Order/Interfaces/IProcessorService.cs — integration marker (optional)
namespace {ServiceName}.App.Services.Order.Interfaces;

public interface IProcessorService<TModel>
    where TModel : class
{
    Task ProcessAsync(TModel model, CancellationToken cancellationToken = default);
}

public interface IOrderCreatedProcessorService : IProcessorService<OrderCreatedWebhook>;
```

```csharp
// App/Services/Order/OrderCreatedProcessorService.cs
public class OrderCreatedProcessorService(…) : IOrderCreatedProcessorService
{
    public Task ProcessAsync(OrderCreatedWebhook message, CancellationToken cancellationToken) { … }
}
```

---

## Checklist

- [ ] New `I*` file is under an `Interfaces/` folder — not beside its implementation
- [ ] Namespace ends with `.Interfaces`
- [ ] Consumers `using` the `.Interfaces` namespace (or global usings)
- [ ] Mappers are `static class`es with no interface — add an `I*` contract only when a mapper needs injected dependencies
