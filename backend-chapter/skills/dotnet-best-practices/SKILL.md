---
name: dotnet-best-practices
description: >-
  Suitsupply Backend Chapter .NET and C# standards: project structure, naming,
  async, error handling, nullability, dependency injection, code review,
  performance, EditorConfig, and production-code templates. Hub skill — links to
  write-tests sub-skills. Use when writing, reviewing, or refactoring C#,
  creating backend services, or asking about coding standards.
---

# .NET Best Practices

Hub skill for Backend Chapter C# development. Works on Cursor and Claude Code.

**Guidelines** apply to all backends (microservices, Web Apps, tools). **Integration examples** (webhooks, queues, enrichment) are optional — see [14_integration-service-patterns.md](reference/14_integration-service-patterns.md).

**Single skill** for chapter standards and production templates. For tests apply **write-tests**.

## Skill map

| # | Task | Skill / document |
|---|------|------------------|
| — | General C# / chapter standards (hub) | `dotnet-best-practices` (this skill) |
| 1 | `src/` and `test/` folder layout | [1_src-folder-structure.md](reference/1_src-folder-structure.md) |
| 2 | Layer boundaries (DTO vs domain) | [2_layer-boundaries.md](reference/2_layer-boundaries.md) |
| 3 | Interface placement | [3_interfaces.md](reference/3_interfaces.md) |
| 4 | Downstream clients | [4_downstream-clients.md](reference/4_downstream-clients.md) |
| 5 | `.csproj` PropertyGroups | [5_csproj.md](reference/5_csproj.md) |
| 6 | EditorConfig / formatting | [6_editorconfig.md](reference/6_editorconfig.md) |
| 7 | Exception handling | [7_exception-handling.md](reference/7_exception-handling.md) |
| 8 | Logging / observability | [8_observability-logging.md](reference/8_observability-logging.md) |
| 9 | Extensions vs Helpers | [9_extensions-vs-helpers.md](reference/9_extensions-vs-helpers.md) |
| 10 | Named private methods | [10_named-private-methods.md](reference/10_named-private-methods.md) |
| 11 | SRP, DRY, SOLID, patterns | [11_principles-and-patterns.md](reference/11_principles-and-patterns.md) |
| 12 | Configuration validation template | [12_configuration-validation.md](reference/12_configuration-validation.md) |
| 13 | New Infra client template | [13_infra-clients.md](reference/13_infra-clients.md) |
| 14 | Integration patterns (optional) | [14_integration-service-patterns.md](reference/14_integration-service-patterns.md) |
| 15 | Azure Functions (optional) | [15_azure-functions.md](reference/15_azure-functions.md) |
| 16 | Enrichment / outbound mapping (optional) | [16_enrichment-and-mappers.md](reference/16_enrichment-and-mappers.md) |
| 17 | Models and Api mappers template | [17_models-and-mappers.md](reference/17_models-and-mappers.md) |
| 18 | `Program.cs` / host bootstrap | [18_program-registration-and-host.md](reference/18_program-registration-and-host.md) |
| — | ServiceInfo / shared Api packages | [Chapter common packages](#chapter-common-packages) |
| — | Production `.cs` examples | [examples/production/](examples/production/) |
| — | Write tests (unspecified tier) | `write-tests` |
| — | Unit tests | `write-unit-tests` |
| — | Component / Reqnroll tests | `write-component-tests` |
| — | Integration / smoke tests | `write-integration-tests` |
| — | Test suite health / planning | `analyze-test-suite` |

Repo-specific `.cursor/skills/` in a project extend these chapter skills. On conflict, repo skills win.

## References

| # | Document | Topic |
|---|----------|-------|
| 1 | [1_src-folder-structure.md](reference/1_src-folder-structure.md) | `src/` and `test/` layout — horizontal vs vertical |
| 2 | [2_layer-boundaries.md](reference/2_layer-boundaries.md) | DTO vs domain at Api and Infra edges |
| 3 | [3_interfaces.md](reference/3_interfaces.md) | `Interfaces/` folder placement |
| 4 | [4_downstream-clients.md](reference/4_downstream-clients.md) | One client per downstream component |
| 5 | [5_csproj.md](reference/5_csproj.md) | `.csproj` PropertyGroup templates |
| 6 | [6_editorconfig.md](reference/6_editorconfig.md) | EditorConfig rules and formatting |
| 7 | [7_exception-handling.md](reference/7_exception-handling.md) | Bubble up; log at Api |
| 8 | [8_observability-logging.md](reference/8_observability-logging.md) | Structured logging |
| 9 | [9_extensions-vs-helpers.md](reference/9_extensions-vs-helpers.md) | Extensions over `*Helper` classes |
| 10 | [10_named-private-methods.md](reference/10_named-private-methods.md) | Named private extraction |
| 11 | [11_principles-and-patterns.md](reference/11_principles-and-patterns.md) | SOLID, DRY, patterns index |
| 12 | [12_configuration-validation.md](reference/12_configuration-validation.md) | FluentValidation + `ValidateOnStart()` |
| 13 | [13_infra-clients.md](reference/13_infra-clients.md) | New downstream client checklist |
| 14 | [14_integration-service-patterns.md](reference/14_integration-service-patterns.md) | Optional integration patterns |
| 15 | [15_azure-functions.md](reference/15_azure-functions.md) | Optional Functions receivers/processors |
| 16 | [16_enrichment-and-mappers.md](reference/16_enrichment-and-mappers.md) | Optional enrichment; Infra outbound mapping |
| 17 | [17_models-and-mappers.md](reference/17_models-and-mappers.md) | Model records; Api mapper template |
| 18 | [18_program-registration-and-host.md](reference/18_program-registration-and-host.md) | `Program.cs` bootstrap, DI lifetimes, Web App host |

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_good-vs-bad.md](examples/1_good-vs-bad.md) | Good vs bad patterns — index |
| 2 | [2_async.md](examples/2_async.md) | Async/await and parallel tasks |
| 3 | [3_nullability.md](examples/3_nullability.md) | Null guards and nullable reference types |
| 4 | [4_di-config-and-coverage.md](examples/4_di-config-and-coverage.md) | DI, config validation, coverage exclusions |
| 5 | [5_logging-and-exceptions.md](examples/5_logging-and-exceptions.md) | Structured logging and bubble-up exceptions |
| 6 | [6_code-structure.md](examples/6_code-structure.md) | Named methods, blank lines, comments, SRP |
| 7 | [7_architecture-patterns.md](examples/7_architecture-patterns.md) | Clients, interfaces, layers, mappers, DRY |
| 8 | [8_style-and-performance.md](examples/8_style-and-performance.md) | Immutability, LINQ, naming, performance |

Production `.cs` templates: [examples/production/](examples/production/). Test templates: **write-tests** sub-skills.

---

## 1. Project structure

Standard backend service layout:

```
{ServiceName}/
├── src/
│   ├── {ServiceName}.Api/           # Host (Azure Functions or ASP.NET Web App)
│   ├── {ServiceName}.Api.Models/    # Published NuGet API contracts
│   ├── {ServiceName}.App/           # Use cases, services, enrichment, client interfaces
│   ├── {ServiceName}.App.Models/    # Internal DTOs and domain models
│   └── {ServiceName}.Infra/         # Client implementations, DI wiring
├── test/
│   ├── {ServiceName}.UnitTests/
│   ├── {ServiceName}.ComponentTests/
│   └── {ServiceName}.IntegrationTests/
├── devops/
│   ├── azurepipelines/              # CI/CD entry YAML
│   │   └── azure-pipeline.yaml      # Build, test, deploy, optional Confluence publish
│   └── bicep/
│       ├── azuredeploy.bicep        # Top-level orchestration (modules under resources/)
│       ├── azuredeploy.parameters.tst.json
│       ├── azuredeploy.parameters.prd.json
│       └── resources/               # hostingplan, functionapp|appservice, appinsights, storage, …
├── docs/                            # Markdown; may publish to Confluence on main
├── .editorconfig
└── {ServiceName}.slnx
```

Per-project folder trees and layout choice (horizontal vs vertical): [reference/1_src-folder-structure.md](reference/1_src-folder-structure.md)

**Dependency direction** (inward only):

```
{ServiceName}.Api
  └── {ServiceName}.Infra
        └── {ServiceName}.App
              └── {ServiceName}.App.Models
{ServiceName}.Api.Models   ← standalone published contracts
```

| Project | Role |
|---------|------|
| **Api** | HTTP entry point, `Program.cs`, DI bootstrap; **maps DTOs ↔ domain at boundary** |
| **App** | Business logic, services, enrichment, client **interfaces** — **domain models only** |
| **App.Models** | **Domain models** — entities, value objects, feature types; App's language |
| **Infra** | Client implementations; **maps domain ↔ wire DTOs** at the external API boundary |
| **Api.Models** | Public HTTP wire contracts (NuGet) — **not** used inside App (Only in Api) |

### Layer boundaries — DTO vs domain

**Separate dependencies at the edges.** The **App layer never works with wire DTOs** from HTTP or external APIs.

| Boundary | Receives | Converts | Passes on |
|----------|----------|----------|-----------|
| **Api** | `Api.Models` / HTTP request DTOs | `Api/Mappers/` — request/response DTO ↔ domain | `App.Models` to App services |
| **Infra** | Domain from App; wire DTOs from external APIs | Client mapping — domain → wire request; wire response → domain | `App.Models` via `IClient` interfaces |
| **App** | Domain models only | Business logic, enrichment | Domain models to Infra clients |

### Inbound HTTP (Api → App)

```
Inbound: HTTP → Api (map: request DTO → domain) → App
```

### Outbound HTTP (App → Api)

```
Response: App → Api (map: domain → response DTO) → HTTP
```

### Outbound call (App → Infra → external API)

```
Outgoing: App → Infra (map: domain → wire request DTO) → external API
          → Infra (map: wire response DTO → domain) → App
```

`App` must **not** reference `Api`, `Api.Models`, or Infra wire DTOs. Client interfaces in `App/Clients/` use **domain types only**.

Full reference: [reference/2_layer-boundaries.md](reference/2_layer-boundaries.md)

Every project `.csproj` uses the chapter `PropertyGroup` template. See [reference/5_csproj.md](reference/5_csproj.md).

### `devops/` layout

Infrastructure and delivery live under `devops/`, separate from application code.

| Path | Purpose |
|------|---------|
| `devops/azurepipelines/` | Azure DevOps pipeline entry point (`azure-pipeline.yaml` / `.yml`). |
| `devops/bicep/azuredeploy.bicep` | Resource-group-scoped orchestration; composes modules from `resources/` |
| `devops/bicep/azuredeploy.parameters.{env}.json` | Per-environment parameters (`tst`, `prd`;) |
| `devops/bicep/resources/` | One module per concern — e.g. `hostingplan`, `functionapp` + `functionappsettings` (Functions) or `appservice` + `appservicesettings` (Web App), `appinsights`, `storageaccount` |

**Pipeline conventions:**

- Reference shared templates from `Stores/cicd-templates` (`dotnet-build-stage-template.yml`) and `Shared/PipelineTemplates` (e.g. Confluence publish).
- Typical stages: **Build** (restore, compile, unit + component tests, Sonar) → optional **PublishDocumentation** → **Deploy TST** → **Deploy PRD**.
- Parameterize: solution path (`.slnx`), test project paths, `sonarProjectKey`, resource group / app names per environment, integration test runsettings + variable groups.
- Triggers: `main`, `release/*`, `hotfix/*`; PR validation on `main`.

**Bicep conventions:**

- `targetScope = 'resourceGroup'` on the entry template.
- `env` parameter restricted to allowed values (`tst`, `prd`).
- App settings reference team Key Vault (`teamKeyVault` param) — secrets are not in Bicep.
- Function App hosts: storage account, user-assigned identity, Service Bus role assignments as needed. Web App hosts: simpler plan + web app modules.
- Resource names use `{service-slug}-{env}-…` pattern (e.g. `ordersync-tst-af`).

---

## Chapter common packages

Shared **Suitsupply** NuGet packages registered on every Api host. Bootstrap order and host variants: [18_program-registration-and-host.md](reference/18_program-registration-and-host.md).

### ServiceInfo (`Suitsupply.Common.ServiceInfo`)

Every Api host registers service metadata via **`AddServiceInfo`** — exposes version/environment info for dashboards, smoke tests, and `/api/home` (Functions) or `/` (Web App).

**NuGet package (Api project):**

| Host | Package |
|------|---------|
| Azure Functions | `Suitsupply.Common.ServiceInfo.Functions` |
| ASP.NET Web App | `Suitsupply.Common.ServiceInfo.AspNet` |

**Registration in `Program.cs`** — call early in `ConfigureServices`, before app services:

```csharp
using Common.ServiceInfo.Extensions;

services.AddServiceInfo(config.GetSection(nameof(ServiceSettings)));
```

**ServiceSettings** — configure before local runs or deployment:

| Key | Required | Rules |
|-----|----------|-------|
| `ServiceSettings:ServiceName` | Yes | Human-readable service name, minimum 3 characters |

**Azure Functions** — `local.settings.json` (gitignored) and Azure app settings:

```json
{
  "Values": {
    "ServiceSettings__ServiceName": "{ServiceName}.Api"
  }
}
```

**ASP.NET Web App** — `appsettings.json` (and environment-specific overrides):

```json
{
  "ServiceSettings": {
    "ServiceName": "{ServiceName}.Api"
  }
}
```

**Response shape** (`GET /api/home` or `GET /`): `serviceName`, `assemblyVersion`, `environment`, `machineName`, `osDescription`. Used by `@smoke` integration tests for connectivity checks.

`ServiceSettings` validator and fail-early pattern: [Configuration validation (fail early)](#configuration-validation-fail-early), [12_configuration-validation.md](reference/12_configuration-validation.md).

---

## 2. Naming conventions

| Element | Convention | Example |
|---------|------------|---------|
| Projects | `{ServiceName}.{Layer}` PascalCase | `OrderSync.App` |
| Namespaces | Mirror folder path | `OrderSync.App.Services.Processors` |
| Interfaces | `I` prefix; file in **`Interfaces/`** subfolder | `App/Clients/Interfaces/IOrderHistoryClient.cs` |
| Downstream clients | `I{Name}Client` or `I{Name}Publisher` | `IOrderHistoryClient`, `IOutboundEventPublisher` |
| Services | `{Feature}{Role}Service` | `FooReceiverService`, `FooProcessorService` |
| Azure Functions | `{Feature}{Role}` | `FooReceiver`, `FooProcessor` |
| Async methods | `Async` suffix | `ProcessAsync` |
| Methods | Name describes behaviour — reader should not need to open the body | `PublishOutboundEventAsync`, not `Handle` |
| Constants | PascalCase static class | `ServiceBusConstants` |
| Extension classes | `{Type}Extensions`; avoid helper | `OrderExtensions`, `MoneyExtensions` |
| Test outer class | `{ClassUnderTest}Tests` static | `FooReceiverTests` |
| Test method | `Should{Outcome}_When{Condition}` | `ShouldReturnAccepted_WhenPayloadValid` |
| Positional records | One parameter per line | [17_models-and-mappers.md](reference/17_models-and-mappers.md) |
| API versioning | In **folders/namespaces** only — never in type names | `Api/Mappers/v1/FooWebhookMapper.cs`, not `FooWebhookMapperV1` |

**Downstream client layout:** interfaces in `App/Clients/Interfaces/`; implementations in `Infra/Clients/{Name}/`. **One client per downstream component** — never merge unrelated external systems into a single `I*` client. See [4_downstream-clients.md](reference/4_downstream-clients.md).

---

## 3. Async patterns

- Public I/O-bound methods return `Task`/`Task<T>` with `Async` suffix.
- **Never** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on tasks.
- Accept `CancellationToken` as the last parameter; pass through to all downstream async calls.
- **ConfigureAwait**: not required in ASP.NET Core / Azure Functions host code. Use `ConfigureAwait(false)` in library code only when you have measured need.
- Avoid `async void` except event handlers.
- Do not wrap sync code in `Task.Run` unless deliberately offloading CPU work.
- **Independent I/O in parallel** — start async calls without `await`. For `Task<T>`, await each task for its result. For `Task` (no return), use `Task.WhenAll` when you only need to wait for all to finish. Do not parallelize when the second call needs the first result.

```csharp
// ✗ wrong — sequential when calls are independent
await orderClient.GetByIdAsync(orderId, cancellationToken);
await historyClient.GetByOrderIdAsync(orderId, cancellationToken);

// ✓ correct — start both, then await each (no Task.WhenAll needed)
var orderTask = orderClient.GetByIdAsync(orderId, cancellationToken);
var historyTask = historyClient.GetByOrderIdAsync(orderId, cancellationToken);
var order = await orderTask;
var history = await historyTask;
```

Use **`Task.WhenAll`** when tasks return `Task` (no result) and you only need to wait for all to finish — there is nothing to await individually:

```csharp
// ✗ wrong — sequential when both are independent side effects
await blobClient.UploadBackupAsync(backup, cancellationToken);
await publisher.PublishOutboundAsync(outbound, cancellationToken);

// ✓ correct — start both, then WhenAll (no return values to read)
var backupTask = blobClient.UploadBackupAsync(backup, cancellationToken);
var publishTask = publisher.PublishOutboundAsync(outbound, cancellationToken);
await Task.WhenAll(backupTask, publishTask);
```

See [2_async.md](examples/2_async.md).

---

## 4. Error handling and logging

| Rule | Detail |
|------|--------|
| **Bubble up** | **Unrecoverable** exceptions propagate from App/Infra to Api `Functions/` — do not catch-and-log just to rethrow |
| **Recover in App/Infra** | When a defined fallback exists (default value, skip optional step, return `null`) — catch the **specific** exception, `LogWarning` if useful, continue |
| **Api boundary** | Catch in Function entry only; `LogError(ex, …)` then **rethrow** (`throw;`) or map to HTTP `500` / `IServiceBusRetryScheduler` |
| Exceptions | Unrecoverable failures, HTTP pipeline errors, unexpected I/O failures |
| Result types | Expected business-rule failures the caller must handle (`Result<T>`, etc.) — prefer in App layer |
| Domain exceptions | App-layer types (e.g. `AppValidationException`); map to HTTP at Api boundary |
| Web apps | `IExceptionHandler` + `ProblemDetails` (.NET 8+). Never leak stack traces in production |
| Azure Functions HTTP | Catch → `LogError` → `ObjectResult` 500 |
| Azure Functions Service Bus | Catch → `LogError`/`LogWarning` → retry scheduler (no rethrow) or `throw` when no scheduler |
| **Process entry** | **First log** after guards with ids **available at that layer** — `{Function}` / `{MessageId}` at Api; business ids (e.g. `{OrderId}`) in App after deserialize |
| **Errors** | Unrecoverable failures logged at **Api** with `LogError(ex, …)` — recoverable failures handled in App/Infra with fallback; no catch-and-rethrow |
| **Info logs** | Beyond process entry: only when they help **troubleshooting** or **dashboards** |
| Log format | Single-line structured templates; `{NamedProperties}`; no multi-line messages |
| Guard clauses | `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` at public entry points |
| Broad catch | **`catch (Exception)` at Api only** — in App/Infra, catch **specific** exceptions only when a recovery path exists |

Full guides: [reference/7_exception-handling.md](reference/7_exception-handling.md), [reference/8_observability-logging.md](reference/8_observability-logging.md). Examples: [examples/1_good-vs-bad.md](examples/1_good-vs-bad.md).

---

## 5. Nullability

- `<Nullable>enable</Nullable>` on all projects.
- `?` on optional reference types; `required` + `init` on non-nullable DTO properties.
- `ThrowIfNull` / `ThrowIfNullOrWhiteSpace` at boundaries — no manual `if (x == null) throw`.
- Avoid `!` unless proven safe after a guard.
- Prefer empty collections over null returns for collections.

Examples: [3_nullability.md](examples/3_nullability.md).

---

## 6. Dependency injection

| Lifetime | Use for |
|----------|---------|
| **Singleton** | Thread-safe stateless services, Azure SDK clients, typed HTTP clients (one typed client per downstream) |
| **Scoped** | Per-request / per-function-invocation (receiver/processor services) |
| **Transient** | Mappers, enrichment steps, validators, lightweight stateless helpers |

- Constructor injection only — primary constructors in C# 12.
- **No service locator** — ban `IServiceProvider.GetService` in business code.
- Register infra in `Infra/Extensions/ServiceCollectionExtensions.AddInfrastructure()`.
- Register App services in host `Program.cs`.
- **Configuration (fail early):** every settings class bound from `IConfiguration` must use `ValidateOnStart()` with FluentValidation — see [Configuration validation](#configuration-validation-fail-early) and [12_configuration-validation.md](reference/12_configuration-validation.md).

Bootstrap checklist and lifetimes: [18_program-registration-and-host.md](reference/18_program-registration-and-host.md).

---

## Configuration validation (fail early)

**Every setting is validated on startup** — if code reads `IOptions<T>`, `T` must have a FluentValidation `AbstractValidator<T>`, `FluentValidateOptions<T>`, and `ValidateOnStart()`. Blocking in code review when missing.

**Applies to:** `ServiceSettings`, every Infra client settings record, Api-layer options, and any future `IOptions<T>`.

**Implementation template** (artifacts, `FluentValidateOptions`, registration, validator examples): [12_configuration-validation.md](reference/12_configuration-validation.md)

---

## 7. Code review expectations

**Blocking (must fix):**
- Correctness bugs, security issues, secrets in code
- Broken or missing tests for non-trivial behaviour
- Layer violations (Infra or `Api.Models` referenced from App; Infra wire DTOs in App interfaces)
- Missing null guards on new public methods
- `TreatWarningsAsErrors` violations

**Non-blocking (suggestion):**
- Naming, readability, optional refactors
- Performance micro-optimizations without evidence

**PR size:** prefer &lt;400 lines changed; one logical change per PR.

**Boy scout rule:** clean up what you touch (format, minor fixes) — but **separate PR**. Do not mix formatting or unrelated cleanup with your feature or bugfix change.

**Reviewer checklist:**
- [ ] `dotnet format` run — no unformatted diffs; VS **Code Cleanup** applied when developed in Visual Studio
- [ ] Tests prove observable behaviour, not internals
- [ ] DI lifetimes correct
- [ ] Every new or changed settings class has FluentValidation + `ValidateOnStart()` (fail early)
- [ ] Entry logs use ids available at that layer (`{Function}` / `{MessageId}` at Api; business ids in App after deserialize)
- [ ] Unrecoverable exceptions bubble from App/Infra — logged at Api `Functions/` (rethrow, HTTP 500, or retry scheduler)
- [ ] Recoverable failures in App/Infra use a defined fallback — specific `catch`, not catch-and-rethrow
- [ ] Informational logs beyond entry are justified (troubleshooting / dashboards only)
- [ ] No secrets or env-specific values committed
- [ ] Methods and classes have **single responsibility** — one reason to change, one task per method
- [ ] Orchestration methods use **named private methods** for distinct actions — `ProcessAsync` is readable at a glance
- [ ] Api converts inbound DTOs → domain before calling App; outbound domain → `Api.Models` DTO
- [ ] Infra converts wire DTOs → domain before returning from `IClient`; maps domain → wire DTO before send/publish; App never sees Infra `Models/`
- [ ] Each downstream component has its own client (`App/Clients/Interfaces/` + `Infra/Clients/{Name}/`) — no god-clients
- [ ] New interfaces live in **`Interfaces/`** folders — not beside implementations (see [3_interfaces.md](reference/3_interfaces.md))
- [ ] Api/Infra mapper contains no business logic — decisions and enrichment done before mapping
- [ ] Repeated logic extracted (base class, factory, pipeline, or **extension methods**) — no copy-paste of flows; no new `*Helper` classes
- [ ] No unnecessary comments or XML doc noise
- [ ] Parameters not mutated; records used for immutable data
- [ ] Logic-free files (`Settings`, DI wiring, Infra wire DTOs) and **Infra client implementations** have `[ExcludeFromCodeCoverage]`; App services, validators, and Infra mappers do not

---

## Design principles — SRP, DRY, and patterns

**Single Responsibility is non-negotiable.** Every class and every method should do **one thing** and do it well. This is the default lens for all production code — not an optional refactor note.

**Reference files** (illustrative `.cs` with chapter examples): [reference/11_principles-and-patterns.md](reference/11_principles-and-patterns.md)

| Principles | Patterns |
|------------|----------|
| [1_SOLID.cs](reference/principles/1_SOLID.cs) | [1_factory-pattern.cs](reference/patterns/1_factory-pattern.cs) |
| [2_DRY.cs](reference/principles/2_DRY.cs) | [2_strategy-pattern.cs](reference/patterns/2_strategy-pattern.cs) |
| [3_KISS.cs](reference/principles/3_KISS.cs) | [3_template-method-pattern.cs](reference/patterns/3_template-method-pattern.cs) |
| [4_YAGNI.cs](reference/principles/4_YAGNI.cs) | [4_decorator-pattern.cs](reference/patterns/4_decorator-pattern.cs) |
| [5_SeparationOfConcerns.cs](reference/principles/5_SeparationOfConcerns.cs) | |
| [6_Encapsulation.cs](reference/principles/6_Encapsulation.cs) | |
| [7_CompositionOverInheritance.cs](reference/principles/7_CompositionOverInheritance.cs) | |

### Single Responsibility Principle (SRP)

| Scope | Rule | Signal to split |
|-------|------|-----------------|
| **Class** | One reason to change — one role in the system | Hard to name without `And` / `Manager` / `Helper`; many unrelated dependencies |
| **Method** | One logical task, one level of abstraction | Method needs a comment to explain *what* it does; > ~15–20 lines of branching logic |
| **Layer** | Api = HTTP/wiring, App = use cases, Infra = external I/O | Business rules leaking into Functions or HTTP clients |

**Practical checks before merging:**
- Can you describe the class in one sentence without "and"?
- Does each public method map to a single verb (`Fetch`, `Map`, `Publish`)?
- Would a change to blob backup force you to edit the mapper? If yes, responsibilities are mixed.

Prefer **many small, focused types** over one large orchestrator. Extract private methods first; extract new classes when the logic is reused or testable on its own.

### Apply established principles and patterns

Use chapter conventions and well-known patterns **by default** — do not reinvent structure when a standard seam already exists:

| Principle / pattern | Where it shows up | Reference |
|---------------------|-------------------|-----------|
| **SOLID** | DIP, SRP, OCP across layers | [1_SOLID.cs](reference/principles/1_SOLID.cs) |
| **DRY** | Base classes, factories, shared mappers | [2_DRY.cs](reference/principles/2_DRY.cs) |
| **KISS / YAGNI** | Simple pipelines; extract on real pain | [3_KISS.cs](reference/principles/3_KISS.cs), [4_YAGNI.cs](reference/principles/4_YAGNI.cs) |
| **Separation of concerns** | Api / App / Infra; **DTO↔domain at Api and Infra edges** | [5_SeparationOfConcerns.cs](reference/principles/5_SeparationOfConcerns.cs), [2_layer-boundaries.md](reference/2_layer-boundaries.md) |
| **Encapsulation** | `internal` Infra, `IOptions<T>`, immutable DTOs | [6_Encapsulation.cs](reference/principles/6_Encapsulation.cs) |
| **Composition over inheritance** | DI, shallow bases, decorators | [7_CompositionOverInheritance.cs](reference/principles/7_CompositionOverInheritance.cs) |
| **Template Method** | Shared algorithm + hooks in `abstract` base | [3_template-method-pattern.cs](reference/patterns/3_template-method-pattern.cs) |
| **Strategy / Handler** | One handler per scenario + factory | [2_strategy-pattern.cs](reference/patterns/2_strategy-pattern.cs) |
| **Factory** | `I*Factory` resolves variant handlers/clients | [1_factory-pattern.cs](reference/patterns/1_factory-pattern.cs) |
| **Decorator** | Cross-cutting logging/metrics via `Decorate<>` | [4_decorator-pattern.cs](reference/patterns/4_decorator-pattern.cs) |
| **Pipeline** | Multi-step flow with discrete steps (integration services) | [14_integration-service-patterns.md](reference/14_integration-service-patterns.md) |
| **Mapper** | Api/Infra shape translation only — **no business logic**; **not** in App | [17_models-and-mappers.md](reference/17_models-and-mappers.md), [2_layer-boundaries.md](reference/2_layer-boundaries.md) |
| **Options + validation** | `IOptions<T>` + FluentValidation fail-early | [12_configuration-validation.md](reference/12_configuration-validation.md) |

When adding behaviour, **extend the existing pattern** in the repo before introducing a parallel approach.

### DRY — eliminate copy-paste with the right abstraction

Repeated code is a design smell. When the same sequence appears **twice**, note it; when it appears **three or more times** (or twice with clear variation points), **refactor** — do not paste again.

| Duplication type | Prefer |
|------------------|--------|
| Same algorithm, different hooks (path, id, tags) | **`abstract` base class** — e.g. shared ingest base, Infra `OutboundLineMapperBase` |
| Same caller, behaviour varies by type/key | **Factory** + strategy/handler per variant |
| Same multi-step flow, steps reusable | **Pipeline** + one class per step (integration services) |
| Same pure calculation on a model, no instance state | **Extension methods** on the type — `{Type}Extensions` in `App/Extensions/` (see [9_extensions-vs-helpers.md](reference/9_extensions-vs-helpers.md)) |
| Same test arrangement | **Abstract test base** + `FixtureFactory` helpers |

```csharp
// ✗ wrong — third copy of the same ingest flow
public sealed class BarIngestService { /* 40 lines identical to FooIngestService */ }

// ✓ correct — shared flow in abstract base; subclass owns only what differs
public sealed class BarIngestService(…) : IngestServiceBase<BarInboundEvent>(…)
{
    protected override string BuildStoragePath(BarInboundEvent model) => …;
}
```

Integration example: `ReceiverServiceBase<T>` in `shopifyintegration` — see [14_integration-service-patterns.md](reference/14_integration-service-patterns.md).

**Do not over-abstract:** one-off duplication of a few lines does not need a framework — see [4_YAGNI.cs](reference/principles/4_YAGNI.cs). Extract when repetition is real and the variation point is clear.

Details and templates: references [12](reference/12_configuration-validation.md)–[18](reference/18_program-registration-and-host.md), [examples/production/](examples/production/), [examples/1_good-vs-bad.md](examples/1_good-vs-bad.md), [reference/11_principles-and-patterns.md](reference/11_principles-and-patterns.md).

---

## 8. Code clarity and style

**Less is more.** Keep code and comments as short as possible.

| Rule | Detail |
|------|--------|
| **Single responsibility** | **One job per class, one task per method** — see **Design principles** above; split when names need "and" or methods grow past one level of abstraction |
| **Extensions over Helpers** | Small pure logic on a model → `{Type}Extensions` with `this` receiver — **never** `*Helper` classes in `src/` |
| **Interfaces in `Interfaces/`** | Every `I*` contract in an `Interfaces/` subfolder — implementations in parent folder |
| Method names | Must explain what the method does — the name is the primary documentation |
| **Extract named steps** | Group each distinct action into a **private method** with a descriptive name (`PublishOutboundEventAsync`) — orchestration methods read as a short sequence of steps. See [10_named-private-methods.md](reference/10_named-private-methods.md) |
| Method length | Short methods; if it does more than one thing, extract private methods or new types |
| Class length | One responsibility per class; prefer several focused types over one orchestrator |
| Comments | Only when code cannot speak for itself — business rules, external constraints, complex behaviour. See [6_code-structure.md](examples/6_code-structure.md#comments) |
| XML docs | No boilerplate on self-explanatory members. Use `///` when the type name is not enough — problem, resolution steps, link to `docs/` |
| Records | Prefer `record` / positional records for immutable DTOs and value objects |
| Parameter mutation | Never modify an object passed as a parameter — construct a copy and return it (see [8_style-and-performance.md](examples/8_style-and-performance.md#immutability)) |
| Logging | Always **single-line** structured templates; entry log with correlation ids; log all errors; sparse info — see [8_observability-logging.md](reference/8_observability-logging.md) |
| **Format before commit** | Run **`dotnet format`** and VS **Code Cleanup** — see [6_editorconfig.md](reference/6_editorconfig.md#apply-before-commit) |
| Readability | **Readability over optimisation** — clear code first; optimise only with evidence (profiler, benchmark, production metrics) |
| LINQ chains | Multi-line: `=` on first line, **source object on the next line**, then **one operator per line** (see below) |
| Method signatures | **Single line** — classes, constructors, and methods on one line unless longer than **160 characters** (positional `record` parameters excepted) |
| Blank line before final `return` | Insert one empty line before the method's **last** `return` — **exempt** only between `Log*` and `return` (log may sit directly above `return`; blank line still required before the log) |
| **Return directly** | Do not assign to a local only to `return` it on the next line — `return` the expression directly |

```csharp
// ✗ wrong — needless local used only for return
var names = items.Select(i => i.Name);
return names;

// ✓ correct
return items.Select(i => i.Name);
```

Use a local when it improves readability (multiple uses, debugging, or a long chain with a meaningful name).

**LINQ formatting** — when a chain does not fit on one line:

```csharp
var active =
    items?
        .Where(i => i.IsActive)
        .OrderBy(i => i.Priority)
        .FirstOrDefault(i => i.Code == targetCode)
        ?.Value;
```

- Assignment (`var name =`) ends the first line.
- Source expression (`items?`) starts on the next line, indented once.
- Each subsequent operator (`.Select`, `.Where`, `.FirstOrDefault`, …) on its own line, indented one level deeper than the source.
- Nullable continuation (`?.Value`) stays on the same line as the last operator when it belongs to that call.
- Short chains that fit within **160 characters** may stay on one line.

Examples: [8_style-and-performance.md](examples/8_style-and-performance.md#linq-formatting).

Production templates: references [12](reference/12_configuration-validation.md)–[18](reference/18_program-registration-and-host.md) and [examples/production/](examples/production/).

---

## 9. Performance

- **Readability over optimisation** — prefer clear code; do not micro-optimise without measured need.
- Default to clarity — optimize with evidence.
- `IEnumerable<T>` in APIs is fine; materialize once if iterating multiple times.
- `StringBuilder` for loops; interpolation for simple cases.
- `Span<T>`/`Memory<T>` for hot parsing/buffer paths only.
- Structured logging — avoid string interpolation when log level is disabled (CA1873).
- `IHttpClientFactory` — never `new HttpClient()` per request.

---

## EditorConfig

Chapter standard enforced at build time with `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`.

**Before every commit:** run **`dotnet format`** and, in Visual Studio, **Code Cleanup** (Analyze → Code Cleanup → Run Code Cleanup, or **Ctrl+K**, **Ctrl+E**). See [reference/6_editorconfig.md](reference/6_editorconfig.md#apply-before-commit).

**Error-level rules (must pass CI):**

| Rule | Meaning |
|------|---------|
| `csharp_style_namespace_declarations = file_scoped` | File-scoped namespaces only |
| `csharp_style_prefer_primary_constructors` | Primary constructors |
| `IDE0090` / target-typed `new` | `new(...)` when type apparent |
| `dotnet_style_namespace_match_folder` / IDE0130 | Namespace = folder path |
| `IDE0290` | Primary constructor diagnostic |
| `IDE0160` / `IDE0161` | File-scoped namespace diagnostics |

**Notable suppressed rules:**
- CA2007 (`ConfigureAwait`) — disabled; not required in host code
- CS8618 — disabled; use `required`/`init` instead

Full annotated reference: [reference/6_editorconfig.md](reference/6_editorconfig.md)

Copy to new repos: [reference/editorconfig](reference/editorconfig)

---

## Cross-cutting build settings

Every `src/` and `test/` `.csproj` includes three `PropertyGroup` blocks **before** any `ItemGroup`:

1. **Build settings** — `net10.0`, nullable, warnings as errors, code style in build, XML docs with `CS1591` suppressed
2. **Deterministic / CI** — `Deterministic` + `ContinuousIntegrationBuild` when `CI=true`
3. **Package metadata** — `Authors`, `Company`, `Product`, `Description`, `Copyright`, `PackageTags`, `RepositoryType`

**Azure Functions Api** adds to group 1: `AzureFunctionsVersion` v4, `OutputType` Exe.

**Test projects** add `IsPackable` false to group 1. Unit and component tests add a coverlet `PropertyGroup`.

**Api.Models** adds `PackageId` in the metadata group for NuGet publish.

Full templates and per-project descriptions: [reference/5_csproj.md](reference/5_csproj.md)

Pair with `.editorconfig` (see [reference/6_editorconfig.md](reference/6_editorconfig.md)).

### Code coverage exclusions

Mark files **without testable logic** with `[ExcludeFromCodeCoverage]` so coverage metrics reflect behaviour, not boilerplate. Apply at **class/record** level (or assembly level for entire model-only projects).

| Apply to | Examples |
|----------|----------|
| Settings records | `FooSettings`, `ServiceBusOptions`, `MessageRetryOptions` |
| DI wiring | `Program`, `ServiceCollectionExtensions` |
| **Infra client implementations** | `FooClient`, `OutboundEventPublisher` — covered by component/integration tests, not unit-tested in isolation |
| Pure DTOs / deserialization models | Infra `Clients/.../Models/` wire shapes with no behaviour |
| Thin logging wrappers (optional) | Prefix logging extensions when many call sites — not required |

**Do not** apply to **App** behavioural code — services, validators. **Do** unit-test Infra mappers. App validators are unit-tested; settings records are excluded because they are property bags only.

`{ServiceName}.App.Models` may use assembly-level exclusion in `.csproj` when the project is DTOs only:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" />
</ItemGroup>
```

Details: Cross-cutting build settings (above); registration templates: [18_program-registration-and-host.md](reference/18_program-registration-and-host.md).

---

## Examples

Cross-cutting good/bad patterns: [1_good-vs-bad.md](examples/1_good-vs-bad.md) (index)

Production templates: references [12](reference/12_configuration-validation.md)–[18](reference/18_program-registration-and-host.md) and [examples/production/](examples/production/)

Test templates: **write-unit-tests**, **write-component-tests**, **write-integration-tests** — each skill's `examples/` folder
