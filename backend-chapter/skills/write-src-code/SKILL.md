---
name: write-src-code
description: >-
  Guide for writing Backend Chapter source code: App services, Api boundary
  mappers, Infra clients, DI, and models. Use when adding or modifying
  production code in Api, App, App.Models, Infra, or Api.Models layers.
---

For testing guidance see **write-tests**. For chapter standards see **dotnet-best-practices**.

# Write Src Code

Hub skill for Backend Chapter production code — Api, App, App.Models, Infra, and Api.Models.

**Guidelines** apply to all backends. **Integration examples** (receivers, processors, enrichment) are optional — see [3_integration-service-patterns.md](reference/3_integration-service-patterns.md).

## Skill map

| Task | Where |
|------|-------|
| Layer boundaries, folder layout, clients | **dotnet-best-practices** |
| Configuration validation, Infra clients, mappers | This skill |
| Integration patterns (optional) | [3_integration-service-patterns.md](reference/3_integration-service-patterns.md) |
| Tests | **write-tests** |

## References

| # | Document | Topic |
|---|----------|-------|
| 1 | [1_configuration-validation.md](reference/1_configuration-validation.md) | FluentValidation + `ValidateOnStart()` |
| 2 | [2_infra-clients.md](reference/2_infra-clients.md) | New downstream client checklist |
| 3 | [3_integration-service-patterns.md](reference/3_integration-service-patterns.md) | Optional integration patterns |
| 4 | [4_azure-functions.md](reference/4_azure-functions.md) | Optional Functions receivers/processors |
| 5 | [5_enrichment-and-mappers.md](reference/5_enrichment-and-mappers.md) | Optional enrichment; Infra outbound mapping |

Cross-skill references: [2_layer-boundaries.md](../dotnet-best-practices/reference/2_layer-boundaries.md), [4_downstream-clients.md](../dotnet-best-practices/reference/4_downstream-clients.md), [1_src-folder-structure.md](../dotnet-best-practices/reference/1_src-folder-structure.md), [11_principles-and-patterns.md](../dotnet-best-practices/reference/11_principles-and-patterns.md).

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_receiver-function.cs](examples/1_receiver-function.cs) | HTTP receiver Function |
| 2 | [2_processor-function.cs](examples/2_processor-function.cs) | Service Bus processor Function |
| 3 | [3_receiver-service.cs](examples/3_receiver-service.cs) | Receiver App service |
| 4 | [4_processor-service.cs](examples/4_processor-service.cs) | Processor App service |
| 5 | [5_enrichment-pipeline.cs](examples/5_enrichment-pipeline.cs) | Enrichment pipeline |
| 6 | [6_webhook-model.cs](examples/6_webhook-model.cs) | Webhook domain model |
| 7 | [7_infra-client.cs](examples/7_infra-client.cs) | Infra HTTP client |

---

## Architecture overview

Five projects with a strict dependency direction:

```
{ServiceName}.Api
  └── {ServiceName}.Infra
        └── {ServiceName}.App
              └── {ServiceName}.App.Models
                    └── (optional external model NuGet packages)
{ServiceName}.Api.Models   ← public HTTP contracts (standalone)
```

Client **interfaces** live in `App/Clients/Interfaces/` — **one per downstream component**; **implementations** live in `Infra/Clients/{Name}/`. **App works with domain models only** — Api and Infra convert wire DTOs at their boundaries. See [../dotnet-best-practices/reference/2_layer-boundaries.md](../dotnet-best-practices/reference/2_layer-boundaries.md).

| Project | Role |
|---|---|
| **Api** | Host (`Program.cs`, Functions); **boundary mappers** (`Api/Mappers/`) — HTTP DTO ↔ domain |
| **App** | Use cases — services, enrichment, client interfaces; **domain only** |
| **App.Models** | **Domain models** — entities, value objects, feature types |
| **Infra** | Client implementations — wire DTOs in `Infra/.../Models/`, **map to domain** before return |
| **Api.Models** | Published NuGet HTTP contracts — **not** referenced from App |

New code must respect project boundaries and DTO↔domain conversion at Api and Infra edges.

Folder layout per project: [../dotnet-best-practices/reference/1_src-folder-structure.md](../dotnet-best-practices/reference/1_src-folder-structure.md).

See **write-tests** and **dotnet-best-practices** skills.

## Configuration validation (fail early)

**Every** settings type bound from `IConfiguration` must fail at startup when invalid — not on first use.

1. **Settings record** — `.../Settings/{Name}.cs`, `[ExcludeFromCodeCoverage]` (no logic — property bag only), `init` properties
2. **Validator** — `.../Validators/{Name}Validator.cs`, `AbstractValidator<T>`, messages `"<Section>:<Property> …"`
3. **Registration** — `AddOptions<T>().Bind(config.GetSection(nameof(T))).ValidateOnStart()` plus `FluentValidateOptions<T>`

Register Infra settings in `AddInfrastructure`; Api-only settings in `Program.cs`. Reuse one `FluentValidateOptions<T>` adapter per solution (`Infra/Validators/`).

Full reference: [reference/1_configuration-validation.md](reference/1_configuration-validation.md)

---

## Web App host

ASP.NET Web App services use Controllers or minimal APIs in `{ServiceName}.Api` but share the same App, Infra, and App.Models layers. Register `Suitsupply.Common.ServiceInfo.AspNet` and map controllers; boundary mappers live in `Api/Mappers/`.

**Vertical feature slices** suit services with multiple unrelated tools (e.g. `Api/{Feature}/Controllers/` mirrored in App and Infra). See [1_src-folder-structure.md](../dotnet-best-practices/reference/1_src-folder-structure.md).

Azure Functions patterns for receivers and processors apply **when your service uses that flow** — see [3_integration-service-patterns.md](reference/3_integration-service-patterns.md).

---

## General coding conventions

- **Single responsibility (SRP)** — **every class one job, every method one task.** If a name needs "and", or a method mixes validation + I/O + mapping, split it. See **dotnet-best-practices** Design principles.
- **Format before commit** — always run `dotnet format` from the solution root; in Visual Studio also run **Code Cleanup** (**Ctrl+K**, **Ctrl+E**). See [6_editorconfig.md](../dotnet-best-practices/reference/6_editorconfig.md#apply-before-commit).
- **Extensions over Helpers** — small pure logic on a model (validation, sums, field extraction) → `{Type}Extensions` in `App/Extensions/` — **never** `*Helper` classes in `src/`. See [9_extensions-vs-helpers.md](../dotnet-best-practices/reference/9_extensions-vs-helpers.md).
- **Interfaces in `Interfaces/` folders** — every `I*` contract lives in an `Interfaces/` subfolder; implementations sit in the parent folder. See [3_interfaces.md](../dotnet-best-practices/reference/3_interfaces.md).
- **Patterns over copy-paste** — when the same flow appears repeatedly, use chapter patterns: `abstract` base classes, factories, pipelines + steps, decorators (`Decorate<>`). Reference: [11_principles-and-patterns.md](../dotnet-best-practices/reference/11_principles-and-patterns.md). Integration-specific examples: [3_integration-service-patterns.md](reference/3_integration-service-patterns.md).
- **Less is more** — keep code and comments as short as possible.
- **Method names** explain what the code does; if the name needs a comment, rename the method.
- **Extract named private methods** — when a block performs one identifiable action (publish, backup, fetch), move it to a `private` method with a descriptive name; keep action-specific logs inside that method. `ProcessAsync` should read as orchestration. See [10_named-private-methods.md](../dotnet-best-practices/reference/10_named-private-methods.md).
- **Short methods and classes** — a long method or class is a signal that responsibilities are mixed; extract focused types or private methods.
- **Comments** — only when code cannot speak for itself: complex logic, business rules, or assumptions. No comment restating what the code obviously does. Same rule for **`///` XML doc comments** — skip boilerplate on self-explanatory members.
- **Logging** — single-line structured `Log*`; **entry log with ids available at that layer** (`{Function}` / `{MessageId}` at Api; business ids in App); **log all errors** with `LogError(ex,…)` — see [8_observability-logging.md](../dotnet-best-practices/reference/8_observability-logging.md)
- **Records for immutability** — prefer positional `record` for DTOs and value objects; `readonly record struct` for small pure values.
- **C# 12**: use primary constructors everywhere; no field-assigning constructor bodies unless unavoidable. **Exception**: classes extending framework types (e.g. `DelegatingHandler`) may use a classic constructor when `ArgumentNullException.ThrowIfNull` guards are needed on injected parameters — primary constructors would force the weaker `?? throw` pattern for field initializers.
- **`<Nullable>enable`**: all projects; `required` + `init` for non-nullable value objects; `?` on optional fields
- **Guard clauses at public entry points**: `ArgumentNullException.ThrowIfNull(x)` and `ArgumentException.ThrowIfNullOrWhiteSpace(x)` — no manual `if (x == null) throw`
- **Namespaces** mirror the folder path exactly: `{ServiceName}.<Layer>[.<Feature>[.<Sub>]]`
- **`sealed`** on any class not designed for inheritance; `abstract` only when a base class exists and extensions are expected
- **No `using` for globally available namespaces** (check `<ImplicitUsings>` and `<Using>` in each `.csproj`)
- **`[ExcludeFromCodeCoverage]`** on files **without unit-testable logic in isolation** — settings records, DI wiring (`Program`, `ServiceCollectionExtensions`), Infra **client implementations** (`internal sealed` classes in `Infra/Clients/`), Infra wire DTOs, thin logging wrappers. **Not** on App services, validators, or Infra mappers (validators and mappers are unit-tested).
- **Parameter line wrapping**: keep class definitions, primary constructors, and method signatures on a **single line** unless doing so would exceed **160 characters**. Only split across multiple lines when the single-line form is longer than 160 chars. 
**Exception: positional `record` types** — always place each parameter on its own line, regardless of total length (see §5).

```csharp
  // ✓ fits on one line — keep it there
  public sealed class FooService(ILogger<FooService> logger, IOrderClient orderClient, IEventBlobStorageClient eventBlobStorageClient)

  // ✓ genuinely long — split is justified
  public sealed class BarProcessorService(ILogger<BarProcessorService> logger, BarWorkflowPipeline pipeline,
      IOutboundEventPublisher publisher, IStoreServiceBusClient storeServiceBusClient,
      IRelatedDataService relatedDataService, IOrderHistoryClient orderHistoryClient) : IBarProcessorService
```

- **Readability over optimisation** — write clear code first; avoid clever micro-optimisations unless profiling proves they matter.
- **Configuration (fail early)** — every settings class bound from config has a FluentValidation validator and `ValidateOnStart()`; the host must not start with invalid configuration. See [reference/1_configuration-validation.md](reference/1_configuration-validation.md).
- **Blank line before final `return`** — one empty line before the method's last `return` statement. **Exempt** only between `Log*` and `return` (log may sit directly above `return`; blank line still required before the log).
- **LINQ formatting** — multi-line chains: `var name =` on the first line; source object on the next line; each operator on its own line, indented one level deeper than the source:

```csharp
  var active =
      items?
          .Where(i => i.IsActive)
          .OrderBy(i => i.Priority)
          .FirstOrDefault(i => i.Code == targetCode)
          ?.Value;
```

  Short chains that fit **within 160 characters** may remain on one line.

```csharp
  // ✗ wrong — cramming a long chain on one line
  var active = items?.Where(i => i.IsActive).OrderBy(i => i.Priority).FirstOrDefault(i => i.Code == code)?.Value;

  // ✗ wrong — multiple operators on one line
  var active = items?.Where(i => i.IsActive).OrderBy(i => i.Priority)
      .FirstOrDefault(i => i.Code == code)?.Value;
```

- **Parameter immutability**: never mutate an object that was passed in as a method parameter. If a modified version is needed, construct and return a new object (copy/`with` expression) instead. Objects created within the same method scope may be built up before being returned.


```csharp
  // ✗ wrong — mutates the parameter
  void Enrich(MyModel model) { model.Field = "value"; }

  // ✓ correct — returns a new object; caller owns the result
  MyModel Enrich(MyModel model) => model with { Field = "value" };

  // ✓ also correct — object is created and owned within this scope
  MyModel Build() { var m = new MyModel(); m.Field = "value"; return m; }
```

  **Framework exemptions** — the following patterns are unavoidably mutation-based by the .NET API design and are acceptable:
  - `IMemoryCache.GetOrCreateAsync(key, entry => { entry.AbsoluteExpirationRelativeToNow = ...; })` — `ICacheEntry` must be mutated to configure expiry.
  - `DelegatingHandler` pipelines mutating `HttpRequestMessage.Headers` to inject auth tokens.
  - `.ConfigureHttpClient((sp, client) => { client.BaseAddress = ...; })` — `IHttpClientFactory` passes an `HttpClient` specifically for configuration; no immutable alternative exists.

---

## 1. Azure Functions — HTTP triggers (integration services)

**Optional.** Only for event-driven integration services with HTTP ingest. See [3_integration-service-patterns.md](reference/3_integration-service-patterns.md).

**Location:** `src/{ServiceName}.Api/Functions/Receivers/` (or `Functions/` for other HTTP triggers)

**Pattern:**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using {ServiceName}.App.Extensions;
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.Api.Functions.Receivers;

public class FooReceiver(ILogger<FooReceiver> logger, IFooReceiverService fooReceiverService)
{
    [Function(nameof(FooReceiver))]
    [OpenApiOperation(nameof(FooReceiver), "Foo Receivers")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(string), Required = true)]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.Accepted)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "foo/created")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked.", nameof(FooReceiver));

        try
        {
            var rawJson = await request.Body.ReadStreamAsString();
            await fooReceiverService.ProcessAsync(rawJson, cancellationToken);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed.", nameof(FooReceiver));
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
```

Rules:
- Class is **not** `sealed` (Azure Functions host needs to subclass in some scenarios)
- **Primary constructor** — inject `ILogger<T>` + the specific `I*ReceiverService`
- `[Function(nameof(...))]` + all four OpenAPI attributes
- `AuthorizationLevel.Function` on secured HTTP routes
- **Entry log:** `LogInformation("{Function} invoked.", …)` **before** body read; business ids logged in App after deserialize
- **Errors:** `LogError(ex, "{Function} failed.", …)` → HTTP 500
- **Body:** `ReadStreamAsString()` from `{ServiceName}.App.Extensions`
- `try/catch` at **Api** `Functions/` for unrecoverable failures — `LogError(ex, …)` then return `500` or delegate to retry scheduler. App/Infra let unrecoverable exceptions bubble; catch **specific** exceptions only when a recovery path exists (fallback value, optional step). See [7_exception-handling.md](../dotnet-best-practices/reference/7_exception-handling.md).

Processors, receiver/processor App services, enrichment, and webhook models are **integration examples** — [3_integration-service-patterns.md](reference/3_integration-service-patterns.md), [4_azure-functions.md](reference/4_azure-functions.md), [examples/](examples/).

---

## 2. New external client

**One client per downstream component** — if the dependency is a new external system (API, queue, blob, publisher), add a **new** client; do not extend an unrelated existing client. See [4_downstream-clients.md](../dotnet-best-practices/reference/4_downstream-clients.md).

Adding a new external HTTP dependency requires changes in three places.

### 2a. Interface — `App/Clients/Interfaces/`

```csharp
namespace {ServiceName}.App.Clients.Interfaces;

public interface IFooClient
{
    Task<FooResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
```

### 2b. Implementation — `Infra/Clients/FooClient/`

```csharp
// Infra/Clients/FooClient/FooClient.cs
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Models.Foo;

namespace {ServiceName}.Infra.Clients.FooClient;

[ExcludeFromCodeCoverage]
internal sealed class FooClient(HttpClient httpClient) : IFooClient
{
    public async Task<FooResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await httpClient.GetAsync($"api/v1/foo/{Uri.EscapeDataString(id)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FooResult>(cancellationToken: cancellationToken);
    }
}
```

### 2c. Settings record — `Infra/Clients/FooClient/Settings/`

```csharp
// Settings/FooSettings.cs
using System.Diagnostics.CodeAnalysis;

namespace {ServiceName}.Infra.Clients.FooClient.Settings;

[ExcludeFromCodeCoverage]
public record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
    public string ClientId { get; init; } = default!;
}
```

### 2d. Validator — `Infra/Clients/FooClient/Validators/`

```csharp
// Validators/FooSettingsValidator.cs
using FluentValidation;
using {ServiceName}.Infra.Clients.FooClient.Settings;

namespace {ServiceName}.Infra.Clients.FooClient.Validators;

internal sealed class FooSettingsValidator : AbstractValidator<FooSettings>
{
    public FooSettingsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotNull()
            .WithMessage("FooSettings:BaseUrl is required in configuration");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("FooSettings:ClientId is required in configuration");
    }
}
```

### 2e. Registration — `Infra/Extensions/ServiceCollectionExtensions.cs`

Add a private extension method and call it from `AddInfrastructure`:

```csharp
private static IServiceCollection AddFooClient(this IServiceCollection services, IConfiguration config)
{
    services.AddOptions<FooSettings>()
        .Bind(config.GetSection(nameof(FooSettings)))
        .ValidateOnStart();
    services.AddSingleton<IValidateOptions<FooSettings>>(
        _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));

    services.AddHttpClient<IFooClient, FooClient>()
        .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() // for internal SSO-protected APIs
        .ConfigureHttpClient((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<FooSettings>>().Value;
            client.BaseAddress = opts.BaseUrl;
            client.DefaultRequestHeaders.Add("Scope", $"{opts.ClientId}/.default");
        });

    return services;
}
```

Then in `AddInfrastructure`: `services.AddFooClient(config);`

---

## 3. Dependency injection lifetimes

| Lifetime | What goes here |
|---|---|
| `AddSingleton` | Azure SDK clients (`BlobServiceClient`, `ServiceBusClient`), infra typed wrappers (`EventBlobStorageClient`, `StoreServiceBusClient`), outbound publishers |
| `AddScoped` | App domain services (typical default for request-scoped work) |
| `AddTransient` | Stateless helpers, mappers, pipeline steps (when used) |

**Register in `Program.cs`** (not inside `AddInfrastructure`):
- Scoped/Transient App-layer services belong in `Program.ConfigureServices`
- Infrastructure wiring belongs in `ServiceCollectionExtensions.AddInfrastructure`

---

## 4. DI registration checklist — `Program.cs`

Register in this order when bootstrapping or extending the Api host:

```csharp
// 0. Service info (required on every Api host)
services.AddServiceInfo(config.GetSection(nameof(ServiceSettings)));
```

Configure `ServiceSettings:ServiceName` in `local.settings.json` (Functions: `ServiceSettings__ServiceName`) or `appsettings.json` before running locally. See **dotnet-best-practices** [Chapter common packages](#chapter-common-packages).

**Azure Functions** — add package `Suitsupply.Common.ServiceInfo.Functions` to `{ServiceName}.Api.csproj`.

**Web App** — add package `Suitsupply.Common.ServiceInfo.AspNet` to `{ServiceName}.Api.csproj`.

Register App-layer services in `Program.ConfigureServices` with the lifetime from §3. Integration registration examples: [3_integration-service-patterns.md](reference/3_integration-service-patterns.md).

---

## 5. Model conventions

### API models (`App.Models/`)

Use **positional records** with `[property: JsonPropertyName]`. Always place each parameter on its own line — even for short records with two or three properties:

```csharp
public record FooNode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("someField")] string? SomeField);
```

This one-parameter-per-line rule applies to **all** positional records in the codebase (including `Api.Models` contracts that have no `JsonPropertyName`):

```csharp
// ✓ correct — each parameter on its own line
public record MoneyAmount(
    string? Amount,
    string? CurrencyCode);

// ✗ wrong — all on one line
public record MoneyAmount(string? Amount, string? CurrencyCode);
```

### Settings records (`Infra/.../Settings/`, `Api/.../Settings/`)

Use **non-positional record** with `init` properties. **Every** settings record requires a matching `AbstractValidator<T>` registered with `ValidateOnStart()` — see [reference/1_configuration-validation.md](reference/1_configuration-validation.md).

```csharp
[ExcludeFromCodeCoverage]
public record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
    public string ClientId { get; init; } = default!;
}
```

### Workflow context records (integration services only)

When an integration service enriches data before outbound mapping, use a feature-specific context record populated by a pipeline. See [3_integration-service-patterns.md](reference/3_integration-service-patterns.md) and [5_enrichment-and-mappers.md](reference/5_enrichment-and-mappers.md).

### Value objects / domain results

Prefer `readonly record struct` for small, pure value results:

```csharp
public readonly record struct ShippingChargeResult(decimal Amount, string TaxCode);
```

---

## 6. Mappers

Mappers translate shape at **layer boundaries only** — **no business logic**, no client calls, no classification rules. **Api** and **Infra** map; **App does not**.

| Location | Maps |
|----------|------|
| `Api/Mappers/` | Domain ↔ `Api.Models` HTTP contracts (inbound request / outbound response) |
| `Infra/Clients/{Name}/Mappers/` | Domain ↔ wire request/response/publish DTO (or inline in client) |

```csharp
// Api/Mappers/Interfaces/IGetFooMapper.cs
namespace {ServiceName}.Api.Mappers.Interfaces;

public interface IGetFooMapper
{
    GetFooResponse Map(Foo domain);
}

// Api/Mappers/GetFooMapper.cs
namespace {ServiceName}.Api.Mappers;

public sealed class GetFooMapper : IGetFooMapper
{
    public GetFooResponse Map(Foo domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new GetFooResponse(
            Id: domain.Id,
            Name: domain.Name);
    }
}
```

Rules:
- Interface in `…/Interfaces/`; implementation in parent folder — see [3_interfaces.md](../dotnet-best-practices/reference/3_interfaces.md)
- **Api mappers:** App service returns domain; Api maps to `Api.Models` before HTTP response
- **Infra mappers:** App passes domain to `IClient`; Infra maps domain → wire DTO before send/publish
- Return `null` when required input is missing (caller decides early exit)
- **One mapper, one output shape** (SRP)
- Blank line before the method's final `return` (no blank line between log and return; blank line still before log)
- Pure **structural** shared mapping → `abstract` base — not for business rules
- Pure **model logic** → `{Type}Extensions` — not `*Helper` classes

Integration outbound publish (App enriches → Infra maps): [5_enrichment-and-mappers.md](reference/5_enrichment-and-mappers.md).

---

## 7. Null-guard quick reference

| Situation | Guard |
|---|---|
| Reference parameter must not be null | `ArgumentNullException.ThrowIfNull(x)` |
| String parameter must not be null or whitespace | `ArgumentException.ThrowIfNullOrWhiteSpace(x)` |
| Post-deserialization check | `ArgumentNullException.ThrowIfNull(model)` |
| Non-null assertion after prior check | Use `!` sparingly, only when type system cannot infer |
| Required property on record | `required` keyword + `init` accessor |

---

## Checklist before submitting new src code

- [ ] **`dotnet format`** run on the solution; **Code Cleanup** run in Visual Studio if applicable
- [ ] Api host calls `services.AddServiceInfo(config.GetSection(nameof(ServiceSettings)))` and `ServiceSettings:ServiceName` is configured
- [ ] Every `src/` `.csproj` has the three standard `PropertyGroup` blocks (build, deterministic/CI, metadata) — see **dotnet-best-practices** [reference/5_csproj.md](../dotnet-best-practices/reference/5_csproj.md)
- [ ] Every settings class (Infra, Api, `ServiceSettings`) has `AbstractValidator<T>` + `AddOptions` + `ValidateOnStart()` + `FluentValidateOptions<T>`
- [ ] Settings records, Infra wire DTOs, and **Infra client implementations** have `[ExcludeFromCodeCoverage]`; App validators and services do not
- [ ] New downstream: **new** client (`App/Clients/Interfaces/` + `Infra/Clients/{Name}/`) — not methods on an unrelated client
- [ ] New external client: interface in `App/Clients/Interfaces/`, implementation + Settings/ + Validators/ in `Infra/Clients/<Name>/`
- [ ] New `I*` contract is in an `Interfaces/` folder — not beside its implementation
- [ ] New settings validator uses `AbstractValidator<T>` with `"<Section>:<Property> is required in configuration"` error messages
- [ ] DI lifetimes: Singleton for infra clients, Scoped for App services, Transient for stateless mappers/helpers
- [ ] All public method entry points have `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`
- [ ] Primary constructors used; no field-assigning constructor bodies
- [ ] App layer uses **domain models only** — no `Api.Models` or Infra wire DTOs in App services/interfaces
- [ ] Infra maps wire DTO → domain before returning from `IClient`; maps domain → wire DTO before send/publish
- [ ] Api maps inbound HTTP DTO → domain before calling App; maps domain → `Api.Models` on responses
- [ ] Namespace matches folder path exactly
- [ ] `sealed` on classes not designed for inheritance; `abstract` base when shared algorithm + hooks
- [ ] Each class/method has single responsibility — no mixed validate+I/O+map in one method
- [ ] Distinct actions extracted to **named private methods** — orchestration reads as steps (see [10_named-private-methods.md](../dotnet-best-practices/reference/10_named-private-methods.md))
- [ ] Api/Infra mapper has **no business logic** — prepared domain/context in, shape translation out
- [ ] No duplicated flows — used base class, factory, pipeline, or extension methods where copy-paste would repeat
- [ ] No `*Helper` classes in `src/` — model logic lives in `{Type}Extensions`
- [ ] No method mutates an object received as a parameter — new objects are constructed and returned instead
- [ ] Log calls are single-line structured templates; entry logs use ids available at that layer
- [ ] No `catch (Exception)` + `LogError` + rethrow in App/Infra — unrecoverable failures bubble to Api; recoverable failures use specific catch + fallback
- [ ] No unnecessary comments or XML doc boilerplate
- [ ] Methods and classes are focused; names describe behaviour
- [ ] Multi-line LINQ: source on new line, one operator per line
- [ ] Blank line before method's final `return` (exempt only between `Log*` and `return`)
