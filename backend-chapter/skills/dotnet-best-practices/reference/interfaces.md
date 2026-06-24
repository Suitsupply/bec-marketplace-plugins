# Interfaces — always in `Interfaces/` folders

Every interface (`I*`) lives in an **`Interfaces/`** subfolder next to its implementations. **Never** place interface and implementation in the same folder.

Namespace mirrors the folder: `{ServiceName}.<Layer>.<Feature>.Interfaces`.

---

## Rule

| Do | Don't |
|----|-------|
| `Services/Receivers/Interfaces/IReceiverService.cs` | `Services/Receivers/IReceiverService.cs` next to `FooCreatedReceiverService.cs` |
| `Mappers/Mao/Interfaces/IMaoOrderCreatedMapper.cs` | `IOutboundFooMapper.cs` beside `OutboundFooMapper.cs` |
| `Clients/Interfaces/IFooClient.cs` | `App/Clients/IFooClient.cs` without `Interfaces/` |

**Implementations** sit in the **parent** folder (sibling of `Interfaces/`):

```
Services/Receivers/
├── Interfaces/
│   ├── IReceiverService.cs          # base + marker interfaces
│   └── IMaoEventsBackupService.cs
├── ReceiverServiceBase.cs
└── OrderCreatedReceiverService.cs
```

---

## Locations by layer

| Contract | Path | Namespace example |
|----------|------|-----------------|
| External client | `App/Clients/Interfaces/` | `{ServiceName}.App.Clients.Interfaces` |
| Shared App service | `App/Services/Interfaces/` | `{ServiceName}.App.Services.Interfaces` |
| Receiver service | `App/Services/Receivers/Interfaces/` | `{ServiceName}.App.Services.Receivers.Interfaces` |
| Processor service | `App/Services/Processors/Interfaces/` | `{ServiceName}.App.Services.Processors.Interfaces` |
| Processor validator | `App/Services/Processors/Validators/Interfaces/` | `…Processors.Validators.Interfaces` |
| Flow handler | `…/TransactionCreatedFlows/Interfaces/` | `…TransactionCreatedFlows.Interfaces` |
| Flow handler factory | `…/Factory/Interfaces/` | `…Factory.Interfaces` |
| Query service | `App/Services/Queries/Interfaces/` | `{ServiceName}.App.Services.Queries.Interfaces` |
| Outbound mapper | `App/Mappers/Mao/Interfaces/` | `{ServiceName}.App.Mappers.Mao.Interfaces` |
| Api boundary mapper | `Api/Mappers/Interfaces/` | `{ServiceName}.Api.Mappers.Interfaces` |
| Api messaging | `Api/Messaging/Interfaces/` | `{ServiceName}.Api.Messaging.Interfaces` |

**Infra** does not define App-facing interfaces — it **implements** interfaces from `App/Clients/Interfaces/`.

---

## File conventions

| Pattern | Detail |
|---------|--------|
| **One interface per file** | Default — `IFooClient.cs`, `IGetOrderService.cs` |
| **Marker interfaces** | Add to the base file — e.g. `IReceiverService.cs` contains `IReceiverService` plus `IFooCreatedReceiverService : IReceiverService` |
| **Shared enums** | Co-locate in `Clients/Interfaces/` when tied to client contracts — e.g. `EventType.cs` |
| **Registration** | DI binds `I*` from `Interfaces/` to implementation in parent folder |

```csharp
// App/Services/Receivers/Interfaces/IReceiverService.cs
namespace {ServiceName}.App.Services.Receivers.Interfaces;

public interface IReceiverService
{
    Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default);
}

public interface IFooCreatedReceiverService : IReceiverService;
```

```csharp
// App/Services/Receivers/FooCreatedReceiverService.cs
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.App.Services.Receivers;

public class FooCreatedReceiverService(…) : ReceiverServiceBase<FooCreatedWebhookRequest>(…), IFooCreatedReceiverService
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
- [ ] Api messaging: `Api/Messaging/Interfaces/IServiceBusRetryScheduler.cs` + `Api/Messaging/ServiceBusRetryScheduler.cs`
