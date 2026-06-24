# Interfaces — always in `Interfaces/` folders

> Reference **3** — Every `I*` contract lives in an `Interfaces/` subfolder next to its implementation.

Every interface (`I*`) lives in an **`Interfaces/`** subfolder next to its implementations. **Never** place interface and implementation in the same folder.

Namespace mirrors the folder: `{ServiceName}.<Layer>.<Feature>.Interfaces`.

---

## Rule

| Do | Don't |
|----|-------|
| `Services/Receivers/Interfaces/IReceiverService.cs` | `Services/Receivers/IReceiverService.cs` next to `FooReceiverService.cs` |
| `Mappers/Interfaces/IFooMapper.cs` (Api or Infra) | `IOutboundFooMapper.cs` beside `OutboundFooMapper.cs` |
| `Clients/Interfaces/IFooClient.cs` | `App/Clients/IFooClient.cs` without `Interfaces/` |

**Implementations** sit in the **parent** folder (sibling of `Interfaces/`):

```
Services/Receivers/
├── Interfaces/
│   ├── IReceiverService.cs              # integration example
│   └── IFooReceiverService.cs
├── ReceiverServiceBase.cs               # integration example only
└── FooReceiverService.cs
```

---

## Locations by layer

| Contract | Path | Namespace example |
|----------|------|-----------------|
| External client | `App/Clients/Interfaces/` | `{ServiceName}.App.Clients.Interfaces` |
| App Services | `App/Services/Interfaces/` | `{ServiceName}.App.Services.Interfaces` |
| Api boundary mapper | `Api/Mappers/Interfaces/` | `{ServiceName}.Api.Mappers.Interfaces` |
| Infra boundary mapper | `Infra/Clients/{Name}/Mappers/Interfaces/` | `{ServiceName}.Infra.Clients.{Name}.Mappers.Interfaces` |

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
// App/Services/Receivers/Interfaces/IReceiverService.cs
namespace {ServiceName}.App.Services.Receivers.Interfaces;

public interface IReceiverService
{
    Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default);
}

public interface IFooReceiverService : IReceiverService;
```

```csharp
// App/Services/Receivers/FooReceiverService.cs
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.App.Services.Receivers;

public class FooReceiverService(…) : ReceiverServiceBase<FooCreatedWebhookRequest>(…), IFooReceiverService  // integration example
{
    …
}
```

---

## Checklist

- [ ] New `I*` file is under an `Interfaces/` folder — not beside its implementation
- [ ] Namespace ends with `.Interfaces`
- [ ] Consumers `using` the `.Interfaces` namespace (or global usings)
- [ ] Api mappers: `Api/Mappers/Interfaces/IGetOrderMapper.cs` + `Api/Mappers/GetOrderMapper.cs`
