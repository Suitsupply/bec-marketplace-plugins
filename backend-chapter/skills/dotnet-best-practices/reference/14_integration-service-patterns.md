# Integration service patterns (optional — examples only)

> Reference **14** — Optional receiver/processor/enrichment patterns for event-driven integration microservices.

**Not chapter guidelines.** These patterns apply to **event-driven integration microservices** (webhook ingest → queue → process → publish). Many backends do not use them — Web Apps, query APIs, and internal tools follow **dotnet-best-practices** without this file.

**Reference implementation:** `shopifyintegration` (receiver → blob backup → Service Bus → enrich → publish → Infra maps on outbound).

---

## When this applies

| Signal | Pattern |
|--------|---------|
| HTTP webhook ingest | Azure Functions `Functions/{Resource}/` (e.g. `Order/OrderCreatedReceiver.cs`) |
| Async queue processing | `Functions/{Resource}/` + `IServiceBusRetryScheduler` |
| Fetch related data before publish | `App/Enrichment/` pipelines + steps |
| Outbound event shape translation | `Infra/Clients/{Publisher}/Mappers/` after enrichment |
| Inbound event models | `App.Models/{Source}/Models/Webhooks/` |

If the service has none of the above, skip this document.

---

## Receive flow (example)

`ReceiverServiceBase<T>` template method: backup to blob → forward to Service Bus. Api deserializes HTTP JSON → maps to domain before calling App. Subclass supplies event type, message id, blob path, tags.

See `shopifyintegration`: `App/Services/Order/ReceiverServiceBase.cs`, `OrderCreatedReceiverService.cs` (legacy services may still use `Receivers/` until migrated).

Marker interfaces: `IReceiverService`, `IFooReceiverService : IReceiverService`.

---

## Process flow (example)

`deserialize → guard → enrich → publish (domain) → backup` — inbound **deserialize/map** at the processor Function; outbound **map** runs inside the Infra publish client, not in App.

- Processor service orchestrates; distinct actions as named private methods (`PublishOutboundEventAsync`, `PublishBackupAsync`).
- Business rules in enrichment steps — not in mappers.
- Optional flow-handler factory when one event type branches on subtype.

See `shopifyintegration`: `OrderUpdatedProcessorService.cs`, `TransactionCreatedFlows/`.

---

## Api messaging (example)

`IServiceBusRetryScheduler` from **`Suitsupply.Common.ServiceBusRetryScheduler`** — retry/dead-letter on processor failure. Register via `AddServiceBusRetryScheduler` in `Program.cs`.

`host.json`: `autoCompleteMessages: false` when Functions complete messages manually.

---

## Enrichment and outbound publish

Full detail: [16_enrichment-and-mappers.md](16_enrichment-and-mappers.md) (also optional / integration-only). App enriches; Infra maps domain → wire before publish.

---

## Azure Functions templates

[15_azure-functions.md](15_azure-functions.md), [production examples](../examples/production/) (`1_receiver-function.cs`, `2_processor-function.cs`, `4_processor-service.cs`, `5_enrichment-pipeline.cs`, `6_webhook-model.cs`).
