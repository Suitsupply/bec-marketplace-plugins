# Integration service patterns (optional — examples only)

> Reference **3** — Optional receiver/processor/enrichment patterns for event-driven integration microservices.

**Not chapter guidelines.** These patterns apply to **event-driven integration microservices** (webhook ingest → queue → process → publish). Many backends do not use them — Web Apps, query APIs, and internal tools follow **write-src-code** and **dotnet-best-practices** without this file.

**Reference implementation:** `shopifyintegration` (receiver → blob backup → Service Bus → enrich → map → publish).

---

## When this applies

| Signal | Pattern |
|--------|---------|
| HTTP webhook ingest | Azure Functions `Functions/Receivers/` |
| Async queue processing | `Functions/Processors/` + `IServiceBusRetryScheduler` |
| Fetch related data before publish | `App/Enrichment/` pipelines + steps |
| Outbound event shape translation | `Infra/Clients/{Publisher}/Mappers/` after enrichment |
| Inbound event models | `App.Models/{Source}/Models/Webhooks/` |

If the service has none of the above, skip this document.

---

## Receive flow (example)

`ReceiverServiceBase<T>` template method: deserialize → backup to blob → forward to Service Bus. Subclass supplies event type, message id, blob path, tags.

See `shopifyintegration`: `App/Services/Receivers/ReceiverServiceBase.cs`, `OrderCreatedReceiverService.cs`.

Marker interfaces: `IReceiverService`, `IFooReceiverService : IReceiverService`.

---

## Process flow (example)

`deserialize → guard → enrich → map → publish → backup`

- Processor service orchestrates; distinct actions as named private methods (`PublishOutboundEventAsync`, `PublishBackupAsync`).
- Business rules in enrichment steps — not in mappers.
- Optional flow-handler factory when one event type branches on subtype.

See `shopifyintegration`: `OrderUpdatedProcessorService.cs`, `TransactionCreatedFlows/`.

---

## Api messaging (example)

`Api/Messaging/Interfaces/IServiceBusRetryScheduler.cs` — retry/dead-letter on processor failure.

`host.json`: `autoCompleteMessages: false` when Functions complete messages manually.

---

## Enrichment and outbound publish

Full detail: [5_enrichment-and-mappers.md](5_enrichment-and-mappers.md) (also optional / integration-only). App enriches; Infra maps domain → wire before publish.

---

## Azure Functions templates

[4_azure-functions.md](4_azure-functions.md), [../examples/](../examples/) (`1_receiver-function.cs`, `2_processor-function.cs`, `4_processor-service.cs`, `5_enrichment-pipeline.cs`, `6_webhook-model.cs`).
