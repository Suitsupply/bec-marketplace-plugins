# `src/` folder structure

Typical layout for a chapter backend service, generalized from **ShopifyIntegration**. Namespace mirrors folder path: `{ServiceName}.<Layer>.<Feature>.<Sub>`.

Reference repo: `shopifyintegration/src/`.

---

## Solution overview

```
src/
├── {ServiceName}.Api/              # Azure Functions (or Web App) host
├── {ServiceName}.Api.Models/       # Published HTTP contracts (NuGet)
├── {ServiceName}.App/              # Business logic
├── {ServiceName}.App.Models/       # Domain models
└── {ServiceName}.Infra/            # Infrastructure implementations
```

---

## `{ServiceName}.Api`

Azure Functions host — triggers, host wiring, Api-only concerns.

```
{ServiceName}.Api/
├── Program.cs                      # DI bootstrap, AddInfrastructure, App service registration
├── host.json                       # Functions runtime (e.g. ServiceBus autoCompleteMessages: false)
├── local.settings.json             # Local config (gitignored)
├── {ServiceName}.Api.csproj
├── Functions/
│   ├── Receivers/                  # HTTP webhooks → App receiver services
│   │   ├── OrderCreatedReceiver.cs
│   │   ├── OrderUpdatedReceiver.cs
│   │   └── …
│   ├── Processors/                 # Service Bus queue listeners → App processor services
│   │   ├── OrderCreatedProcessor.cs
│   │   ├── OrderUpdatedProcessor.cs
│   │   └── …
│   └── Queries/                    # HTTP GET query endpoints
│       └── GetOrderFunction.cs
├── Mappers/                        # Boundary: domain ↔ Api.Models DTO
│   ├── GetOrderMapper.cs
│   └── Interfaces/
│       └── IGetOrderMapper.cs
└── Messaging/                      # Host-only messaging infrastructure
    ├── Interfaces/
    │   └── IServiceBusRetryScheduler.cs
    ├── ServiceBusRetryScheduler.cs
    ├── RetryOutcome.cs
    ├── Settings/
    │   └── MessageRetryOptions.cs
    └── Validators/
        └── MessageRetryOptionsValidator.cs
```

| Folder / file | Purpose |
|---------------|---------|
| `Functions/Receivers/` | HTTP `POST` webhooks; delegate to `I*ReceiverService` |
| `Functions/Processors/` | `ServiceBusTrigger` queue listeners; `IServiceBusRetryScheduler` on failure |
| `Functions/Queries/` | Read-only HTTP APIs; App query service + `Api/Mappers/` |
| `Mappers/` | Map `App.Models` domain ↔ `Api.Models` request/response DTOs |
| `Messaging/` | Retry scheduler (`Interfaces/` + implementation), Api-layer options — not business logic |

---

## `{ServiceName}.Api.Models`

Published NuGet — **public HTTP contracts only**. No references to App or Infra.

```
{ServiceName}.Api.Models/
├── {ServiceName}.Api.Models.csproj # PackageId: Suitsupply.{ServiceName}.Api.Models
└── Order/                          # Feature area (mirror public API surface)
    ├── Models/                     # Shared nested DTOs (used by requests and responses)
    │   ├── MoneyAmount.cs
    │   ├── OrderLineItem.cs
    │   └── …
    ├── Requests/                   # Inbound HTTP contracts
    │   └── GetOrderByInternalOrderIdRequest.cs
    └── Responses/                  # Outbound HTTP contracts
        └── GetOrderResponse.cs
```

Organize by **API feature** (`Order/`, `Product/`, …) with `Models/` + `Requests/` + `Responses/`. Put feature-specific request/response records in `Requests/` and `Responses/`; shared nested types in `Models/`.

---

## `{ServiceName}.App`

Business logic — **no** Infra types, **no** `Api.Models`. Depends on `App.Models` only (+ optional external domain NuGets).

```
{ServiceName}.App/
├── {ServiceName}.App.csproj
├── Clients/
│   └── Interfaces/                 # One I* per downstream — IShopifyGraphQLClient, IServiceBusClient, IMaoPublisher, …
├── Enrichment/
│   ├── OrderCreatedEnrichmentPipeline.cs
│   ├── OrderUpdatedEnrichmentPipeline.cs
│   └── Steps/
│       ├── FetchOrderStep.cs
│       ├── FetchStoreLocationStep.cs
│       └── …
├── Extensions/                       # Extension methods on domain models — NOT *Helper classes
│   ├── ShopifyOrderExtensions.cs     # ResolveAlternateOrderId, IsShipToStore, …
│   ├── MoneyExtensions.cs
│   ├── StreamExtensions.cs           # ReadStreamAsString
│   └── Logging/                      # Optional — prefix helpers when many log call sites (ShopifyIntegration pattern)
│       ├── ProcessorLoggingExtensions.cs
│       ├── ReceiverLoggingExtensions.cs
│       └── …
├── Mappers/
│   └── Mao/                          # Outbound publish mapping (domain/envelope → MAO shape)
│       ├── MaoOrderCreatedMapper.cs
│       ├── Interfaces/
│       ├── Lines/                    # Line-level mappers
│       └── Payment/
├── Services/
│   ├── Interfaces/                   # IAlterationService, IOrderHistoryService, …
│   ├── Receivers/
│   │   ├── Interfaces/               # IReceiverService, IOrderCreatedReceiverService, …
│   │   ├── ReceiverServiceBase.cs
│   │   └── OrderCreatedReceiverService.cs
│   ├── Processors/
│   │   ├── Interfaces/               # IProcessorService, IOrderCreatedProcessorService, …
│   │   ├── OrderCreatedProcessorService.cs
│   │   ├── Validators/               # Post-enrichment validation
│   │   │   └── Interfaces/
│   │   └── TransactionCreatedFlows/  # Strategy handlers (example)
│   │       ├── Factory/
│   │       │   └── Interfaces/
│   │       ├── Interfaces/
│   │       ├── KlarnaAuthorizationFlowHandler.cs
│   │       └── …
│   └── Queries/
│       ├── Interfaces/
│       └── GetOrderService.cs
```

| Folder | Purpose |
|--------|---------|
| `Clients/Interfaces/` | One `I*` per downstream component — contracts implemented in Infra — **domain types only** |
| `Enrichment/` | Pipelines + `Steps/` — business rules, fetch related data |
| `Mappers/` | Outbound shape translation after enrichment (no business logic); `Mappers/…/Interfaces/` for `I*` contracts |
| `Services/Receivers/` | Webhook receive flow (backup + enqueue) via `ReceiverServiceBase<T>` |
| `Services/Processors/` | Deserialize → enrich → map → publish orchestration |
| `Services/Processors/…/FlowHandlers/` or `TransactionCreatedFlows/` | Strategy + factory per scenario |
| `Services/Queries/` | Read models for query endpoints |
| `Extensions/` | `{Type}Extensions` on models; optional `Logging/` prefix helpers. **No `*Helper` classes** |

---

## `{ServiceName}.App.Models`

Domain models — webhooks, GraphQL/domain entities, enrichment envelopes. No framework or Infra dependencies.

```
{ServiceName}.App.Models/
├── {ServiceName}.App.Models.csproj   # Often assembly-level [ExcludeFromCodeCoverage]
├── Shopify/                          # External system domain (rename per integration)
│   ├── OrderExtensions.cs            # Optional: co-locate when extending types in this folder
│   └── Webhooks/
│       ├── OrderCreatedWebhookRequest.cs
│       └── …
├── Enrichment/
│   ├── OrderCreatedEnrichmentEnvelope.cs
│   └── …
├── Alterations/                      # Feature-specific domain types
├── ShipmentMethod/
└── Mao/                              # Internal publish wrappers / constants
    └── Payment/
```

| Folder | Purpose |
|--------|---------|
| `{Source}/Webhooks/` | Inbound webhook domain models (after Api boundary mapping) |
| `{Source}/{Type}Extensions.cs` | Optional co-located extensions when the extended type lives in the same folder |
| `Enrichment/` | Feature-specific envelopes (`*EnrichmentEnvelope`) |
| Feature folders | Domain types grouped by bounded context (`Alterations/`, `ShipmentMethod/`, …) |

Wire DTOs from external HTTP APIs belong in **Infra** `Clients/.../Models/`, not here. Published third-party shapes (e.g. MAO NestedModels NuGet) are referenced directly where appropriate.

---

## `{ServiceName}.Infra`

Infrastructure implementations — **one client folder per downstream component** (HTTP API, queue, blob, pub/sub).

```
{ServiceName}.Infra/
├── {ServiceName}.Infra.csproj
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # AddInfrastructure(config)
├── Validators/
│   └── FluentValidateOptions.cs         # Shared IValidateOptions adapter
└── Clients/
    ├── ShopifyGraphQLClient/
    │   ├── ShopifyGraphQLClient.cs
    │   ├── Authentication/              # Token provider, delegating handler
    │   ├── Models/                      # Wire DTOs — internal to Infra
    │   ├── Settings/
    │   └── Validators/
    ├── ServiceBusClient/
    │   ├── Settings/                    # ServiceBusOptions, nested queue names
    │   └── Validators/
    ├── BlobStorageClient/
    │   ├── Settings/
    │   └── Validators/
    ├── MaoPublisherClient/
    │   ├── Settings/
    │   └── Validators/
    ├── AlterationsClient/
    ├── OrderHistoryClient/
    ├── SaleOrderClient/
    └── ShipmentMethodClient/
```

Each folder is a **separate downstream** — do not merge unrelated systems into one client. See [downstream-clients.md](downstream-clients.md).

**Per-client folder pattern** (repeat for **each** downstream component):

```
Clients/{ClientName}/
├── {ClientName}.cs                   # [ExcludeFromCodeCoverage] internal sealed — implements App/Clients/Interfaces/I*
├── Settings/{Name}Settings.cs
├── Validators/{Name}SettingsValidator.cs
└── Models/                           # Optional — external API JSON shapes
```

| Folder | Purpose |
|--------|---------|
| `Extensions/` | `AddInfrastructure` — register all clients, options, validators |
| `Validators/` | Shared `FluentValidateOptions<T>` |
| `Clients/*/` | **One folder per downstream component**; `[ExcludeFromCodeCoverage]` `internal sealed` implementation; own Settings + Validators |

---

## Namespace ↔ folder rule

```
src/ShopifyIntegration.App/Services/Processors/OrderCreatedProcessorService.cs
→ namespace ShopifyIntegration.App.Services.Processors;
```

File-scoped namespaces; folder path must match exactly (IDE0130).

---

## What goes where (quick reference)

| Concern | Project / path |
|---------|----------------|
| HTTP trigger class | `Api/Functions/` |
| Service Bus listener | `Api/Functions/Processors/` |
| Retry / dead-letter | `Api/Messaging/` (`Interfaces/IServiceBusRetryScheduler.cs` + implementation) |
| Public API request DTO | `Api.Models/{Feature}/Requests/` |
| Public API response DTO | `Api.Models/{Feature}/Responses/` |
| Shared API nested DTO | `Api.Models/{Feature}/Models/` |
| Webhook / domain model | `App.Models/` |
| Business orchestration | `App/Services/` |
| Enrichment rules | `App/Enrichment/Steps/` |
| Outbound MAO/API mapping | `App/Mappers/` |
| Client interface (per downstream) | `App/Clients/Interfaces/I{Name}.cs` |
| Client implementation (per downstream) | `Infra/Clients/{Name}/` |
| Service interface | `App/Services/{Feature}/Interfaces/` or `App/Services/Interfaces/` |
| Mapper interface | `App/Mappers/{Area}/Interfaces/` or `Api/Mappers/Interfaces/` |
| Api messaging | `Api/Messaging/Interfaces/` | `{ServiceName}.Api.Messaging.Interfaces` |
| External API wire DTO | `Infra/Clients/{Name}/Models/` |
| Settings + FluentValidation | `*/Settings/` + `*/Validators/` |
| DI registration (infra) | `Infra/Extensions/ServiceCollectionExtensions.cs` |
| DI registration (App services) | `Api/Program.cs` |

---

## Related references

- Layer boundaries (DTO vs domain): [layer-boundaries.md](layer-boundaries.md)
- Interface placement: [interfaces.md](interfaces.md)
- Downstream clients: [downstream-clients.md](downstream-clients.md)
- `.csproj` PropertyGroups: [csproj.md](csproj.md)
- Azure Functions patterns: [../../write-src-code/reference/azure-functions.md](../../write-src-code/reference/azure-functions.md)
