# Downstream clients — one per component

> Reference **4** — One client interface and Infra folder per downstream system.

Every **downstream component** the service integrates with gets its **own client** — one `I*` interface in App and one implementation folder in Infra. Do **not** combine multiple downstream systems into a single client class.

---

## Rule

**One downstream = one client contract + one Infra folder + one settings record + one validator + one `Add*Client` registration.**

App services compose multiple clients — that is expected. The anti-pattern is a god-client that hides several downstreams behind one interface.

### Examples (illustrative — not required names)

| Downstream component | Client |
|---------------------|--------|
| Partner GraphQL API | `IPartnerGraphQLClient` → `Infra/Clients/PartnerGraphQLClient/` |
| Event publisher (pub/sub) | `IOutboundEventPublisher` → `Infra/Clients/OutboundEventPublisherClient/` |
| Internal REST API | `IOrderHistoryClient` → `Infra/Clients/OrderHistoryClient/` |
| Event blob backup | `IEventBlobStorageClient` → `Infra/Clients/EventBlobStorageClient/` |
| Store / processor queue (Service Bus) | `IStoreServiceBusClient` → `Infra/Clients/StoreServiceBusClient/` |

---

## What counts as a downstream component

| Type | Examples | Client |
|------|----------|--------|
| External HTTP/REST API | Partner API, internal microservice | `I{Name}Client` + typed `HttpClient` |
| External GraphQL API | Admin / partner GraphQL | `I{Name}Client` |
| Message broker / queue | Azure Service Bus | `I{Purpose}ServiceBusClient` — e.g. `IStoreServiceBusClient` |
| Object storage | Azure Blob | `I{Purpose}BlobStorageClient` — e.g. `IEventBlobStorageClient` |
| Pub/Sub / event publisher | Kafka topic, cloud pub/sub | `I{Name}Publisher` |

Different **base URLs**, **auth schemes**, **SDKs**, or **operational ownership** → separate client, even when the systems belong to the same product family.

Vertical-layout services may place interfaces under `App/{Feature}/Clients/Interfaces/` instead of central `App/Clients/Interfaces/` — still **one contract per downstream**.

---

## Structure per client (horizontal layout)

```
App/Clients/Interfaces/IOrderHistoryClient.cs

Infra/Clients/OrderHistoryClient/
├── OrderHistoryClient.cs              # internal sealed — implements IOrderHistoryClient
├── Settings/OrderHistorySettings.cs
├── Validators/OrderHistorySettingsValidator.cs
└── Models/                            # Wire DTOs — Infra only
```

Register in `ServiceCollectionExtensions` with a dedicated private method:

```csharp
services.AddOrderHistoryClient(config);   // one Add* per downstream
```

---

## Do / don't

```csharp
// ✗ wrong — one client for unrelated downstreams
public interface IIntegrationClient
{
    Task<Order?> GetOrderAsync(string id, CancellationToken ct);
    Task PublishEventAsync(OutboundEvent msg, CancellationToken ct);
    Task<History?> GetHistoryAsync(string id, CancellationToken ct);
}

// ✓ correct — one interface per downstream; service composes
public class FetchRelatedDataStep(IOrderClient orderClient, IOrderHistoryClient historyClient)
{
    // uses each client for its own system
}
```

```csharp
// ✗ wrong — stuffing a second downstream into an existing client "because it's also HTTP"
// Adding publish methods to IOrderClient

// ✓ correct — new downstream → new client folder + interface + registration
```

---

## Naming

Never use `Api` in client names — it is redundant. Use **`Client`** or **`Publisher`** only. Do not use `*ApiClient` — drop the `Api` segment (`ISaleOrderClient`, not `ISaleOrderApiClient`).

**Name by purpose, not technology.** Avoid generic SDK names (`IServiceBusClient`, `IBlobStorageClient`) — they hide what the client does and encourage god-clients. Prefer `IStoreServiceBusClient`, `IEventBlobStorageClient`, `IOrderHistoryClient`, etc.

| Pattern | When |
|---------|------|
| `I{Name}Client` | HTTP/GraphQL/REST API, infrastructure SDK wrapper (Service Bus, Blob), any downstream integration |
| `I{Name}Publisher` | Outbound fire-and-forget publish (pub/sub, topic) |

Interface file: `App/Clients/Interfaces/I{Name}Client.cs` or `I{Name}Publisher.cs` — see [3_interfaces.md](3_interfaces.md).

---

## Checklist

- [ ] Each downstream component has its own `I*` contract
- [ ] Each has a matching Infra implementation folder — no shared mega-client
- [ ] Own `Settings/` + `Validators/` + `ValidateOnStart()` per client
- [ ] Own `Add{Name}Client` (or `Add{Name}`) registration in `AddInfrastructure` (or `Program.cs` for compact Web Apps)
- [ ] App services inject only the clients they need
- [ ] Infra client implementation classes have `[ExcludeFromCodeCoverage]`
