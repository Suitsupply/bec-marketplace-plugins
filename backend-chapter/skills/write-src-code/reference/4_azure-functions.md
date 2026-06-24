# Azure Functions patterns (integration services — optional)

> Reference **4** — HTTP receivers and Service Bus processors for integration microservices.

**Not chapter guidelines.** HTTP receivers, Service Bus processors, and related App services for **event-driven integration microservices**.

Many chapter backends use **ASP.NET Web Apps** instead — see **write-src-code** § Web App host. Do not add Functions folders unless the service actually uses Azure Functions.

Reference implementation: `shopifyintegration`. Overview: [3_integration-service-patterns.md](3_integration-service-patterns.md).

## Receivers (HTTP)

- Location: `src/{ServiceName}.Api/Functions/Receivers/`
- **Api layer catches unrecoverable failures** — `LogError(ex, …)` → `500`; App/Infra bubble those. Recoverable failures (fallback) may be caught in App/Infra with a specific exception type
- **Entry log:** `LogInformation("{Function} invoked.", …)` before body read
- Happy path: `AcceptedResult`
- Example: [../examples/1_receiver-function.cs](../examples/1_receiver-function.cs)

## Processors (Service Bus queue listeners)

- Location: `src/{ServiceName}.Api/Functions/Processors/`
- Inject `IServiceBusRetryScheduler` + queue options — `RescheduleOrDeadLetterAsync` on failure
- Success: `CompleteMessageAsync`; failure: `LogError` if dead-lettered, `LogWarning` if rescheduled
- Function entry log: `{MessageId}`; App processor entry log: primary business correlation id
- Optional `_Debug` HTTP twin with `LogWarning`
- Example: [../examples/2_processor-function.cs](../examples/2_processor-function.cs)

## Query endpoints (HTTP GET)

- Location: `src/{ServiceName}.Api/Functions/Queries/`
- App query service returns domain; `Api/Mappers/` → `Api.Models` response DTO

## Receiver / processor App services (examples)

- Marker interfaces inheriting `IReceiverService` / `IProcessorService`
- `ReceiverServiceBase<TModel>` for shared webhook ingest — override hooks only
- Processor: deserialize → enrich → map → publish — see [5_enrichment-and-mappers.md](5_enrichment-and-mappers.md)

Examples: [../examples/3_receiver-service.cs](../examples/3_receiver-service.cs), [../examples/4_processor-service.cs](../examples/4_processor-service.cs).
