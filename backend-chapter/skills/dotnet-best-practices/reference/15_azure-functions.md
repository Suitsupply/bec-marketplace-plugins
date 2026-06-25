# Azure Functions patterns (integration services — optional)

> Reference **15** — HTTP receivers and Service Bus processors for integration microservices.

**Not chapter guidelines.** HTTP receivers, Service Bus processors, and related App services for **event-driven integration microservices**.

**Logging / observability:** [8_observability-logging.md](8_observability-logging.md).

Many chapter backends use **ASP.NET Web Apps** instead — see [18_program-registration-and-host.md](18_program-registration-and-host.md). Do not add Functions folders unless the service actually uses Azure Functions.

Reference implementation: `shopifyintegration`. Overview: [14_integration-service-patterns.md](14_integration-service-patterns.md).

## Receivers (HTTP)

- Location: `src/{ServiceName}.Api/Functions/Receivers/`
- **Api maps inbound JSON** — deserialize to `Api.Models` request DTO → `Api/Mappers/` → domain → `IReceiverService.ProcessAsync(domain)`
- **Api layer catches unrecoverable failures** — `LogError(ex, …)` → `500`; App/Infra bubble those. Recoverable failures (fallback) may be caught in App/Infra with a specific exception type
- **Entry log:** `LogInformation("{Function} invoked.", …)` before body read
- Happy path: `AcceptedResult`
- Example: [1_receiver-function.cs](../examples/production/1_receiver-function.cs)

## Processors (Service Bus queue listeners)

- Location: `src/{ServiceName}.Api/Functions/Processors/`
- Inject `IServiceBusRetryScheduler` + queue options — `RescheduleOrDeadLetterAsync` on failure
- Success: `CompleteMessageAsync`; failure: `LogError` if dead-lettered, `LogWarning` if rescheduled
- Function entry log: `{MessageId}`; App processor entry log: primary business correlation id
- Optional `_Debug` HTTP twin with `LogWarning`
- Example: [2_processor-function.cs](../examples/production/2_processor-function.cs)

## Query endpoints (HTTP GET)

- Location: `src/{ServiceName}.Api/Functions/Queries/`
- App query service returns domain; `Api/Mappers/` → `Api.Models` response DTO

## Receiver / processor App services (examples)

- Marker interfaces inheriting `IReceiverService` / `IProcessorService`
- `ReceiverServiceBase<TModel>` for shared webhook ingest — override hooks only
- Processor: map at Function → `ProcessAsync(domain)` — Infra maps inside the outbound client; see [16_enrichment-and-mappers.md](16_enrichment-and-mappers.md)

Examples: [3_receiver-service.cs](../examples/production/3_receiver-service.cs), [4_processor-service.cs](../examples/production/4_processor-service.cs).
