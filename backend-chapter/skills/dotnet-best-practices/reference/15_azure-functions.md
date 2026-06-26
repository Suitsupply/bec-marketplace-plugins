# Azure Functions patterns (integration services — optional)

> Reference **15** — HTTP receivers, Service Bus processors, and query functions for integration microservices.

**Not chapter guidelines.** HTTP receivers, Service Bus processors, and related App services for **event-driven integration microservices**.

**Logging / observability:** [8_observability-logging.md](8_observability-logging.md).

Many chapter backends use **ASP.NET Web Apps** instead — see [18_program-registration-and-host.md](18_program-registration-and-host.md). Do not add Functions folders unless the service actually uses Azure Functions.

Reference implementation: `shopifyintegration`. Overview: [14_integration-service-patterns.md](14_integration-service-patterns.md).

## Folder layout — by resource

Group Azure Functions **by resource** (`PersonFunctions`, `VehicleFunctions`), not by trigger type. Put every trigger for one resource in a single class under `Functions/`. Method names carry the role:

| Role | Typical method | Trigger |
|------|----------------|---------|
| Query | `GetAsync` | HTTP GET |
| Processor | `Run` | Service Bus (+ optional `_Debug` HTTP) |
| Receiver | `PostAsync` | HTTP POST webhook |

```
Api/Functions/
├── PersonFunctions.cs           # GetAsync, Run, ProcessDebugAsync
└── VehicleFunctions.cs

App/Services/
├── Interfaces/
│   ├── IGetPersonService.cs
│   └── IPersonRequestedProcessorService.cs
└── PersonServices.cs
```

Vertical slice (`Example/Functions/PersonFunctions.cs`, `Example/Services/PersonService.cs`) or horizontal at Api/App root — same rule: **one class per resource** for functions; shared interfaces in `Services/Interfaces/`.

### Acceptable alternative — one class per trigger/role

Splitting a resource's triggers into separate classes is fine when the triggers are unrelated or the per-resource class is getting large. Keep them under the resource folder/namespace and let the **class name** carry the role:

```
Api/Functions/Persons/
├── GetPersonFunction.cs          # HTTP GET query
└── UpdatePersonFunction.cs       # Service Bus Run + ProcessDebugAsync HTTP twin
```

Either style is accepted — choose one per service and apply it consistently. The role-per-method convention above still applies to whichever methods live in each class.

## Receivers (HTTP)

- Location: `src/{ServiceName}.Api/Functions/{Resource}Functions.cs` (e.g. `Order/OrderCreatedReceiver` class in `OrderFunctions.cs`)
- **Api maps inbound JSON** — deserialize to `Api.Models` request DTO → `Api/Mappers/` → domain → `IReceiverService.ProcessAsync(domain)`
- **Api layer catches unrecoverable failures** — `LogError(ex, …)` → `500`; App/Infra bubble those. Recoverable failures (fallback) may be caught in App/Infra with a specific exception type
- **Entry log:** `LogInformation("{Function} invoked.", …)` before body read
- Happy path: `AcceptedResult`
- Example: [1_receiver-function.cs](../examples/production/1_receiver-function.cs)

## Processors (Service Bus queue listeners)

- Location: `src/{ServiceName}.Api/Functions/{Resource}/`
- Inject `IServiceBusRetryScheduler` + queue options — `RescheduleOrDeadLetterAsync` on failure. Full pattern (delayed retry, delivery-count carry-forward, `MessageRetryOptions`, `RetryOutcome`): [19_servicebus-retry-scheduler.md](19_servicebus-retry-scheduler.md)
- Success: `CompleteMessageAsync`; failure: `LogError` if dead-lettered, `LogWarning` if rescheduled
- Function entry log: `{MessageId}`; App processor entry log: primary business correlation id
- **`_Debug` HTTP twin**: pair the Service Bus trigger with a sibling HTTP `[Function("…Debug")]` method (e.g. route `…/process/debug`) so component tests can drive the processor in-process. Log at `LogWarning` since it is a non-production entry point. Map it in `ApplicationFactory.ConfigureWebHost`.
- Example: [2_processor-function.cs](../examples/production/2_processor-function.cs)

## Query endpoints (HTTP GET)

- Location: `src/{ServiceName}.Api/Functions/{Resource}/`
- App query service returns domain; `Api/Mappers/` → `Api.Models` response DTO

## OpenAPI annotations (HTTP-triggered functions)

Annotate every **HTTP-triggered** function (receivers, query endpoints, `_Debug` twins) with OpenAPI attributes so the generated Swagger/OpenAPI document stays accurate. Package: **`Microsoft.Azure.Functions.Worker.Extensions.OpenApi`** on the Api project.

| Attribute | Use |
|-----------|-----|
| `[OpenApiOperation("{operationId}", "{tag}")]` | Operation id + grouping tag (e.g. `"GetPerson", "Person"`) |
| `[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]` | Function-key auth on `AuthorizationLevel.Function` endpoints |
| `[OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]` | Route/query parameters |
| `[OpenApiRequestBody("application/json", typeof({Request}), Required = true)]` | POST/PUT request body shape (`Api.Models` DTO) |
| `[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof({Response}))]` | Success response with body (`Api.Models` DTO) |
| `[OpenApiResponseWithoutBody(HttpStatusCode.NotFound)]` | Status-only responses (404, 202, …) |

```csharp
[Function("GetPerson")]
[OpenApiOperation("GetPerson", "Person")]
[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
[OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(GetPersonResponse))]
[OpenApiResponseWithoutBody(HttpStatusCode.NotFound)]
public async Task<IActionResult> GetPersonAsync(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/{id:int}")] HttpRequest request,
    int id,
    CancellationToken cancellationToken = default) { … }
```

- Reference response/request types from **`Api.Models`** only — never domain or Infra wire DTOs.
- Document **every** status code the function can return (success + 404 / 202 / 500 as applicable).
- Service Bus–triggered functions need no OpenAPI attributes; their HTTP `_Debug` twin does.

## Receiver / processor App services (examples)

- Marker interfaces inheriting `IReceiverService` / `IProcessorService`
- `ReceiverServiceBase<TModel>` for shared webhook ingest — override hooks only
- Processor: map at Function → `ProcessAsync(domain)` — Infra maps inside the outbound client; see [16_enrichment-and-mappers.md](16_enrichment-and-mappers.md)

Examples: [3_receiver-service.cs](../examples/production/3_receiver-service.cs), [4_processor-service.cs](../examples/production/4_processor-service.cs).
