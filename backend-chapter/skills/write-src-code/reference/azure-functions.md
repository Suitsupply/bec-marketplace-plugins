# Azure Functions Patterns

Service-specific patterns (processors, receiver/processor App services, queries). Not every backend service uses these — apply when the repo has webhook/queue flows like ShopifyIntegration.

Full templates in [../examples/](../examples/).

## Receivers (HTTP)

- Location: `src/{ServiceName}.Api/Functions/Receivers/`
- **Only Api layer catches** — `LogError(ex, …)` → `500`; App receiver services let exceptions bubble
- **Entry log:** `LogInformation("{Function} invoked.", …)` before body read — see [observability-logging.md](../../dotnet-best-practices/reference/observability-logging.md)
- Happy path: `AcceptedResult`

## Processors (Service Bus queue listeners)

- Location: `src/{ServiceName}.Api/Functions/Processors/`
- Inject `IServiceBusRetryScheduler` + `IOptions<ServiceBusOptions>` — `RescheduleOrDeadLetterAsync` on failure (not manual delivery-count catches)
- Success: `CompleteMessageAsync`; failure: `LogError` if dead-lettered, `LogWarning` if rescheduled
- Function entry log: `{MessageId}`; App processor entry log: `{OrderId}` — see [observability-logging.md](../../dotnet-best-practices/reference/observability-logging.md)
- Always include `_Debug` HTTP twin with `LogWarning`
- Full example: [../examples/processor-function.cs](../examples/processor-function.cs)

## Query endpoints (HTTP GET)

- Location: `src/{ServiceName}.Api/Functions/Queries/`
- Service in `App/Services/Queries/`, mapper in `Api/Mappers/`, DTO in `Api.Models`

## Receiver services

- Marker interface: `IFooCreatedReceiverService : IReceiverService`
- Extend `ReceiverServiceBase<TModel>` — override EventType, GetMessageId, GetPath, GetTags
- Add `EventType` enum value for new webhook types

## Processor services

- Deserialize → guard → enrich → map → publish → backup
- Return early when mapper returns null
