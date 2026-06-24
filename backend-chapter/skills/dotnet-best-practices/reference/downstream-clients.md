# Downstream clients — one per component

Every **downstream component** the service integrates with gets its **own client** — one `I*` interface in App and one implementation folder in Infra. Do **not** combine multiple downstream systems into a single client class.

---

## Rule

| Downstream component | Client |
|---------------------|--------|
| Shopify GraphQL API | `IShopifyGraphQLClient` → `Infra/Clients/ShopifyGraphQLClient/` |
| MAO Pub/Sub publisher | `IMaoPublisher` → `Infra/Clients/MaoPublisherClient/` |
| Order History API | `IOrderHistoryClient` → `Infra/Clients/OrderHistoryClient/` |
| Azure Service Bus | `IServiceBusClient` → `Infra/Clients/ServiceBusClient/` |
| Azure Blob Storage | `IBlobStorageClient` → `Infra/Clients/BlobStorageClient/` |
| Alterations API | `IAlterationsClient` → `Infra/Clients/AlterationsClient/` |

**One downstream = one client contract + one Infra folder + one settings record + one validator + one `Add*Client` registration.**

App services and enrichment steps **compose** multiple clients — that is expected. The anti-pattern is a god-client that hides several downstreams behind one interface.

---

## What counts as a downstream component

| Type | Examples | Client |
|------|----------|--------|
| External HTTP/REST API | Partner API, internal microservice | `I{Name}Client` + typed `HttpClient` |
| External GraphQL API | Shopify Admin API | `I{Name}Client` |
| Message broker / queue | Azure Service Bus | `IServiceBusClient` |
| Object storage | Azure Blob | `IBlobStorageClient` |
| Pub/Sub / event publisher | MAO, Kafka topic | `I{Name}Publisher` |

Different **base URLs**, **auth schemes**, **SDKs**, or **operational ownership** → separate client, even when the systems belong to the same product family.

---

## Structure per client

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
    Task<Order?> GetShopifyOrderAsync(string id, CancellationToken ct);
    Task PublishToMaoAsync(MaoEventMessage msg, CancellationToken ct);
    Task<OrderHistory?> GetHistoryAsync(string id, CancellationToken ct);
}

// ✓ correct — one interface per downstream; service composes
public class FetchOrderHistoryStep(
    IShopifyGraphQLClient shopifyClient,
    IOrderHistoryClient orderHistoryClient)
{
    // uses each client for its own system
}
```

```csharp
// ✗ wrong — stuffing a second downstream into an existing client "because it's also HTTP"
// Adding MAO publish methods to IShopifyGraphQLClient

// ✓ correct — new downstream → new client folder + interface + registration
// App/Clients/Interfaces/IMaoPublisher.cs
// Infra/Clients/MaoPublisherClient/MaoPubSubPublisher.cs
```

---

## Naming

Never use `Api` in client names — it is redundant. Use **`Client`** or **`Publisher`** only.

| Pattern | When |
|---------|------|
| `I{Name}Client` | HTTP/GraphQL/REST API, infrastructure SDK wrapper (Service Bus, Blob), any downstream integration |
| `I{Name}Publisher` | Outbound fire-and-forget publish (pub/sub, topic) |

Interface file: `App/Clients/Interfaces/I{Name}Client.cs` or `I{Name}Publisher.cs` — see [interfaces.md](interfaces.md).

---

## Checklist

- [ ] Each downstream component has its own `I*` in `App/Clients/Interfaces/`
- [ ] Each has a matching `Infra/Clients/{Name}/` folder — no shared mega-client
- [ ] Own `Settings/` + `Validators/` + `ValidateOnStart()` per client
- [ ] Own `Add{Name}Client` (or `Add{Name}`) registration method in `AddInfrastructure`
- [ ] App services inject only the clients they need — not a kitchen-sink integration client
- [ ] Infra client implementation classes have `[ExcludeFromCodeCoverage]`
