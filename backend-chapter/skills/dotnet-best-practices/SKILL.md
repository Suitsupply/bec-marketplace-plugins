---
name: dotnet-best-practices
description: >-
  Suitsupply Backend Chapter .NET and C# standards: project structure, naming,
  async, error handling, nullability, dependency injection, code review,
  performance, and EditorConfig rules. Hub skill — links to write-src-code and
  write-tests sub-skills. Use when writing, reviewing, or refactoring C#,
  creating backend services, or asking about coding standards.
---

# .NET Best Practices

Hub skill for Backend Chapter C# development. Works on Cursor and Claude Code.

For production code patterns apply **write-src-code**. For tests apply **write-tests** (routes to unit/component/integration sub-skills).

## Skill map

| Task | Skill |
|------|-------|
| General C# / standards / EditorConfig | `dotnet-best-practices` (this skill) |
| SRP, DRY, SOLID, patterns | `dotnet-best-practices` — [reference/principles-and-patterns.md](reference/principles-and-patterns.md) |
| Add function, service, client, mapper | `write-src-code` |
| Layer boundaries (DTO vs domain) | `dotnet-best-practices` — [reference/layer-boundaries.md](reference/layer-boundaries.md) |
| `src/` folder layout per project | `dotnet-best-practices` — [reference/src-folder-structure.md](reference/src-folder-structure.md) |
| Logging / observability | `dotnet-best-practices` — [reference/observability-logging.md](reference/observability-logging.md) |
| Exception handling | `dotnet-best-practices` — [reference/exception-handling.md](reference/exception-handling.md) |
| Extensions vs Helpers | `dotnet-best-practices` — [reference/extensions-vs-helpers.md](reference/extensions-vs-helpers.md) |
| Interface placement | `dotnet-best-practices` — [reference/interfaces.md](reference/interfaces.md) |
| Downstream clients | `dotnet-best-practices` — [reference/downstream-clients.md](reference/downstream-clients.md) |
| Named private methods | `dotnet-best-practices` — [reference/named-private-methods.md](reference/named-private-methods.md) |
| "Write tests" (unspecified) | `write-tests` |
| Unit tests | `write-unit-tests` |
| Component / Reqnroll tests | `write-component-tests` |
| Integration / smoke tests | `write-integration-tests` |
| Test suite health / planning | `analyze-test-suite` |

Repo-specific `.cursor/skills/` in a project extend these chapter skills. On conflict, repo skills win.

---

## 1. Project structure

Standard backend service layout:

```
{ServiceName}/
├── src/
│   ├── {ServiceName}.Api/           # Host (Azure Functions or ASP.NET Web App)
│   ├── {ServiceName}.Api.Models/    # Published NuGet API contracts
│   ├── {ServiceName}.App/           # Use cases, services, mappers, client interfaces
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

Per-project folder trees: [reference/src-folder-structure.md](reference/src-folder-structure.md)

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
| **App** | Business logic, enrichment, outbound mappers, client **interfaces** — **domain models only** |
| **App.Models** | **Domain models** — webhooks, envelopes, entities; App's language |
| **Infra** | Client implementations; **maps wire DTOs → domain** before returning to App |
| **Api.Models** | Public HTTP wire contracts (NuGet) — **not** used inside App (Only in Api) |

### Layer boundaries — DTO vs domain

**Separate dependencies at the edges.** The **App layer never works with wire DTOs** from HTTP or external APIs.

| Boundary | Receives | Converts | Passes on |
|----------|----------|----------|-----------|
| **Api** | `Api.Models` / HTTP request DTOs | `Api/Mappers/` → domain | `App.Models` to App services |
| **Infra** | External API wire DTOs (`Infra/.../Models/`) | Client mapping → domain | `App.Models` via `IClient` interfaces |
| **App** | Domain models only | Business logic, enrichment | Domain / envelope to outbound mappers |

```
HTTP DTO → [Api mapper] → domain → App → IClient → Infra → wire DTO → [Infra map] → domain → App
App domain → [Api mapper] → Api.Models response DTO → HTTP
```

`App` must **not** reference `Api`, `Api.Models`, or Infra wire DTOs. Client interfaces in `App/Clients/` use **domain types only**.

Full reference: [reference/layer-boundaries.md](reference/layer-boundaries.md)

Every project `.csproj` uses the chapter `PropertyGroup` template. See [reference/csproj.md](reference/csproj.md).

Client interfaces live in `App/Clients/Interfaces/`; implementations in `Infra/Clients/{Name}/`. **One client per downstream component** — never merge unrelated external systems into a single `I*` client. See [reference/downstream-clients.md](reference/downstream-clients.md).

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

**Required configuration** — part of the chapter **fail early** rule: every settings section uses FluentValidation + `ValidateOnStart()`. Missing or invalid values prevent the app from starting.

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

Details: **write-src-code** §4.

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

## 2. Naming conventions

| Element | Convention | Example |
|---------|------------|---------|
| Projects | `{ServiceName}.{Layer}` PascalCase | `OrderSync.App` |
| Namespaces | Mirror folder path | `OrderSync.App.Services.Processors` |
| Interfaces | `I` prefix; file in **`Interfaces/`** subfolder | `App/Clients/Interfaces/IShopifyGraphQLClient.cs` |
| Downstream clients | `I{Name}Client` or `I{Name}Publisher` — **never** `*ApiClient` | `IOrderHistoryClient`, `IMaoPublisher` |
| Services | `{Feature}{Role}Service` | `FooCreatedReceiverService` |
| Azure Functions | `{Feature}{Role}` | `FooCreatedReceiver` |
| Async methods | `Async` suffix | `ProcessAsync` |
| Methods | Name describes behaviour — reader should not need to open the body | `PublishOutboundEventAsync`, not `Handle` |
| Constants | PascalCase static class | `ServiceBusConstants` |
| Extension classes | `{Type}Extensions` — **not** `*Helper` | `ShopifyOrderExtensions`, `MoneyExtensions` |
| Test outer class | `{ClassUnderTest}Tests` static | `FooReceiverTests` |
| Test method | `Should{Outcome}_When{Condition}` | `ShouldReturnAccepted_WhenPayloadValid` |
| Positional records | One parameter per line | See write-src-code §5 |

---

## 3. Async patterns

- Public I/O-bound methods return `Task`/`Task<T>` with `Async` suffix.
- **Never** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on tasks.
- Accept `CancellationToken` as the last parameter; pass through to all downstream async calls.
- **ConfigureAwait**: not required in ASP.NET Core / Azure Functions host code. Use `ConfigureAwait(false)` in library code only when you have measured need.
- Avoid `async void` except event handlers.
- Do not wrap sync code in `Task.Run` unless deliberately offloading CPU work.

See [examples/good-vs-bad.md](examples/good-vs-bad.md).

---

## 4. Error handling and logging

| Rule | Detail |
|------|--------|
| **Bubble up** | **App and Infra do not catch unexpected exceptions** — let them propagate to Api `Functions/` |
| **Api boundary** | Catch in Function entry only; `LogError(ex, …)` then **rethrow** (`throw;`) or map to HTTP `500` / `IServiceBusRetryScheduler` |
| Exceptions | Unrecoverable failures, HTTP pipeline errors, unexpected I/O failures |
| Result types | Expected business-rule failures the caller must handle (`Result<T>`, etc.) — prefer in App layer |
| Domain exceptions | App-layer types (e.g. `AppValidationException`); map to HTTP at Api boundary |
| Web apps | `IExceptionHandler` + `ProblemDetails` (.NET 8+). Never leak stack traces in production |
| Azure Functions HTTP | Catch → `LogError` → `ObjectResult` 500 |
| Azure Functions Service Bus | Catch → `LogError`/`LogWarning` → retry scheduler (no rethrow) or `throw` when no scheduler |
| **Process entry** | **First log** after guards with ids **available at that layer** — `{Function}` / `{MessageId}` at Api; business ids (e.g. `{OrderId}`) in App after deserialize |
| **Errors** | Logged at **Api layer** with `LogError(ex, …)` — App does not catch-and-log unexpected failures |
| **Info logs** | Beyond process entry: only when they help **troubleshooting** or **dashboards** |
| Log format | Single-line structured templates; `{NamedProperties}`; no multi-line messages |
| Guard clauses | `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` at public entry points |
| Broad catch | **Api layer only** — CA1031 justified at Function boundaries |

Full guides: [reference/exception-handling.md](reference/exception-handling.md), [reference/observability-logging.md](reference/observability-logging.md). Examples: [examples/good-vs-bad.md](examples/good-vs-bad.md).

---

## 5. Nullability

- `<Nullable>enable</Nullable>` on all projects.
- `?` on optional reference types; `required` + `init` on non-nullable DTO properties.
- `ThrowIfNull` / `ThrowIfNullOrWhiteSpace` at boundaries — no manual `if (x == null) throw`.
- Avoid `!` unless proven safe after a guard.
- Prefer empty collections over null returns for collections.

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
- **Configuration (fail early):** every settings class bound from `IConfiguration` must use `AddOptions<T>().Bind(...).ValidateOnStart()` with a FluentValidation `AbstractValidator<T>` via `FluentValidateOptions<T>`. The host must not start with missing or invalid config. See **Configuration validation** below and **write-src-code** [reference/configuration-validation.md](../write-src-code/reference/configuration-validation.md).

Details and registration checklist: **write-src-code** §3–4.

---

## Configuration validation (fail early)

**Every setting is validated on startup.** No exceptions for “simple” or “optional-looking” sections — if code reads `IOptions<T>`, `T` must have a validator and `ValidateOnStart()`.

| Requirement | Detail |
|-------------|--------|
| Binding | `AddOptions<TSettings>().Bind(config.GetSection(nameof(TSettings))).ValidateOnStart()` |
| Validation | `FluentValidateOptions<TSettings>` + `AbstractValidator<TSettings>` |
| Error messages | `"<Section>:<Property> is required in configuration"` (or constraint-specific text) |
| Settings type | `[ExcludeFromCodeCoverage]` `record` with `init` properties |
| Tests | Unit-test each validator in `{ServiceName}.UnitTests` |

**Applies to:** `ServiceSettings`, every Infra client settings record, Api-layer options (retry, messaging, etc.), and any future `IOptions<T>`.

**Blocking in code review:** new or changed settings without startup validation.

Full pattern: **write-src-code** [reference/configuration-validation.md](../write-src-code/reference/configuration-validation.md)

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

**Reviewer checklist:**
- [ ] `dotnet format` run — no unformatted diffs; VS **Code Cleanup** applied when developed in Visual Studio
- [ ] Tests prove observable behaviour, not internals
- [ ] DI lifetimes correct
- [ ] Every new or changed settings class has FluentValidation + `ValidateOnStart()` (fail early)
- [ ] Entry logs use ids available at that layer (`{Function}` / `{MessageId}` at Api; business ids in App after deserialize)
- [ ] Unexpected exceptions bubble from App/Infra — logged only at Api `Functions/` (rethrow, HTTP 500, or retry scheduler)
- [ ] Informational logs beyond entry are justified (troubleshooting / dashboards only)
- [ ] No secrets or env-specific values committed
- [ ] Methods and classes have **single responsibility** — one reason to change, one task per method
- [ ] Orchestration methods use **named private methods** for distinct actions — `ProcessAsync` is readable at a glance
- [ ] Api converts inbound DTOs → domain before calling App; outbound domain → `Api.Models` DTO
- [ ] Infra converts wire DTOs → domain before returning from `IClient`; App never sees Infra `Models/`
- [ ] Each downstream component has its own client (`App/Clients/Interfaces/` + `Infra/Clients/{Name}/`) — no god-clients
- [ ] New interfaces live in **`Interfaces/`** folders — not beside implementations (see [interfaces.md](reference/interfaces.md))
- [ ] Mapper contains no business logic — decisions and enrichment done before `Map()`
- [ ] Repeated logic extracted (base class, factory, pipeline, or **extension methods**) — no copy-paste of flows; no new `*Helper` classes
- [ ] No unnecessary comments or XML doc noise
- [ ] Parameters not mutated; records used for immutable data
- [ ] Logic-free files (`Settings`, DI wiring, Infra wire DTOs) and **Infra client implementations** have `[ExcludeFromCodeCoverage]`; App services, validators, and mappers do not

---

## Design principles — SRP, DRY, and patterns

**Single Responsibility is non-negotiable.** Every class and every method should do **one thing** and do it well. This is the default lens for all production code — not an optional refactor note.

**Reference files** (illustrative `.cs` with chapter examples): [reference/principles-and-patterns.md](reference/principles-and-patterns.md)

| Principles | Patterns |
|------------|----------|
| [1_SOLID.cs](reference/principles/1_SOLID.cs) | [factory-pattern.cs](reference/patterns/factory-pattern.cs) |
| [2_DRY.cs](reference/principles/2_DRY.cs) | [strategy-pattern.cs](reference/patterns/strategy-pattern.cs) |
| [3_KISS.cs](reference/principles/3_KISS.cs) | [template-method-pattern.cs](reference/patterns/template-method-pattern.cs) |
| [4_YAGNI.cs](reference/principles/4_YAGNI.cs) | [decorator-pattern.cs](reference/patterns/decorator-pattern.cs) |
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
| **Separation of concerns** | Api / App / Infra; **DTO↔domain at Api and Infra edges** | [5_SeparationOfConcerns.cs](reference/principles/5_SeparationOfConcerns.cs), [layer-boundaries.md](reference/layer-boundaries.md) |
| **Encapsulation** | `internal` Infra, `IOptions<T>`, immutable DTOs | [6_Encapsulation.cs](reference/principles/6_Encapsulation.cs) |
| **Composition over inheritance** | DI, shallow bases, decorators | [7_CompositionOverInheritance.cs](reference/principles/7_CompositionOverInheritance.cs) |
| **Template Method** | `ReceiverServiceBase<T>` — shared receive flow | [template-method-pattern.cs](reference/patterns/template-method-pattern.cs) |
| **Strategy / Handler** | `FlowHandlers/` — one handler per scenario | [strategy-pattern.cs](reference/patterns/strategy-pattern.cs) |
| **Factory** | `ITransactionFlowHandlerFactory`, `IStarWarsClientFactory` | [factory-pattern.cs](reference/patterns/factory-pattern.cs) |
| **Decorator** | `StarWarsServiceLoggingDecorator`, Scrutor `Decorate<>` | [decorator-pattern.cs](reference/patterns/decorator-pattern.cs) |
| **Pipeline** | `EnrichmentPipeline` + discrete steps | [enrichment-and-mappers.md](../write-src-code/reference/enrichment-and-mappers.md) |
| **Mapper** | Shape translation only — **no business logic**; enrich first via envelope/domain model | **write-src-code** §6, [enrichment-and-mappers.md](../write-src-code/reference/enrichment-and-mappers.md) |
| **Options + validation** | `IOptions<T>` + FluentValidation fail-early | [configuration-validation.md](../write-src-code/reference/configuration-validation.md) |

When adding behaviour, **extend the existing pattern** in the repo before introducing a parallel approach.

### DRY — eliminate copy-paste with the right abstraction

Repeated code is a design smell. When the same sequence appears **twice**, note it; when it appears **three or more times** (or twice with clear variation points), **refactor** — do not paste again.

| Duplication type | Prefer |
|------------------|--------|
| Same algorithm, different hooks (blob path, event type, queue name) | **`abstract` base class** — e.g. `ReceiverServiceBase<T>`, `OutboundLineProductMapperBase` |
| Same caller, behaviour varies by type/key | **Factory** + strategy/handler per variant — e.g. `IFlowHandlerFactory` |
| Same multi-step flow, steps reusable | **Pipeline** + one class per step |
| Same pure calculation on a model, no instance state | **Extension methods** on the type — `{Type}Extensions` in `App/Extensions/` (see [extensions-vs-helpers.md](reference/extensions-vs-helpers.md)) |
| Same test arrangement | **Abstract test base** + `FixtureFactory` helpers |

```csharp
// ✗ wrong — third receiver copy-pasting deserialize → backup → queue
public sealed class BarReceiver { /* 40 lines identical to FooReceiver */ }

// ✓ correct — shared flow in base; subclass owns only what differs
public sealed class BarCreatedReceiverService(…) : ReceiverServiceBase<BarWebhookRequest>(…)
{
    protected override EventType EventType => EventType.BarCreated;
    protected override string BuildBlobPath(BarWebhookRequest model) => …;
}
```

**Do not over-abstract:** one-off duplication of a few lines does not need a framework — see [4_YAGNI.cs](reference/principles/4_YAGNI.cs). Extract when repetition is real and the variation point is clear.

Details and templates: **write-src-code** §1, §6, [azure-functions.md](../write-src-code/reference/azure-functions.md), [enrichment-and-mappers.md](../write-src-code/reference/enrichment-and-mappers.md), [examples/good-vs-bad.md](examples/good-vs-bad.md), [reference/principles-and-patterns.md](reference/principles-and-patterns.md).

---

## 9. Code clarity and style

**Less is more.** Keep code and comments as short as possible.

| Rule | Detail |
|------|--------|
| **Single responsibility** | **One job per class, one task per method** — see **Design principles** above; split when names need "and" or methods grow past one level of abstraction |
| **Extensions over Helpers** | Small pure logic on a model → `{Type}Extensions` with `this` receiver — **never** `*Helper` classes in `src/` |
| **Interfaces in `Interfaces/`** | Every `I*` contract in an `Interfaces/` subfolder — implementations in parent folder |
| Method names | Must explain what the method does — the name is the primary documentation |
| **Extract named steps** | Group each distinct action into a **private method** with a descriptive name (`PublishEventToMaoAsync`) — orchestration methods read as a short sequence of steps. See [named-private-methods.md](reference/named-private-methods.md) |
| Method length | Short methods; if it does more than one thing, extract private methods or new types |
| Class length | One responsibility per class; prefer several focused types over one orchestrator |
| Comments | **No comments** unless the code cannot speak for itself: complex algorithms, non-obvious business rules, or documented assumptions. Applies to `//`, `/* */`, and **`///` XML doc comments** |
| XML docs | Do not add `///` summary noise on self-explanatory members. Reserve for published public API surfaces where consumers need contract docs |
| Records | Prefer `record` / positional records for immutable DTOs and value objects |
| Parameter mutation | Never modify an object passed as a parameter — construct a copy and return it (see [examples/good-vs-bad.md](examples/good-vs-bad.md)) |
| Logging | Always **single-line** structured templates; entry log with correlation ids; log all errors; sparse info — see [observability-logging.md](reference/observability-logging.md) |
| **Format before commit** | Run **`dotnet format`** and VS **Code Cleanup** — see [editorconfig.md](reference/editorconfig.md#apply-before-commit) |
| Readability | **Readability over optimisation** — clear code first; optimise only with evidence (profiler, benchmark, production metrics) |
| LINQ chains | Multi-line: `=` on first line, **source object on the next line**, then **one operator per line** (see below) |
| Method signatures | **Single line** — classes, constructors, and methods on one line unless longer than **160 characters** (positional `record` parameters excepted) |
| Blank line before final `return` | Insert one empty line before the method's **last** `return` — **exempt** only between `Log*` and `return` (log may sit directly above `return`; blank line still required before the log) |

**LINQ formatting** — when a chain does not fit on one line:

```csharp
var raw =
    metafields?.Edges?
        .Select(e => e.Node)
        .OfType<Metafield>()
        .FirstOrDefault(n => n.Namespace == AdyenNamespace && n.Key == TransactionDetailsKey)
        ?.Value;
```

- Assignment (`var name =`) ends the first line.
- Source expression (`metafields?.Edges?`) starts on the next line, indented once.
- Each subsequent operator (`.Select`, `.Where`, `.FirstOrDefault`, …) on its own line, indented one level deeper than the source.
- Nullable continuation (`?.Value`) stays on the same line as the last operator when it belongs to that call.
- Short chains that fit within **160 characters** may stay on one line.

Details and templates: **write-src-code** general conventions.

---

## 8. Performance

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

**Before every commit:** run **`dotnet format`** and, in Visual Studio, **Code Cleanup** (Analyze → Code Cleanup → Run Code Cleanup, or **Ctrl+K**, **Ctrl+E**). See [reference/editorconfig.md](reference/editorconfig.md#apply-before-commit).

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

Full annotated reference: [reference/editorconfig.md](reference/editorconfig.md)

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

Full templates and per-project descriptions: [reference/csproj.md](reference/csproj.md)

Pair with `.editorconfig` (see [reference/editorconfig.md](reference/editorconfig.md)).

### Code coverage exclusions

Mark files **without testable logic** with `[ExcludeFromCodeCoverage]` so coverage metrics reflect behaviour, not boilerplate. Apply at **class/record** level (or assembly level for entire model-only projects).

| Apply to | Examples |
|----------|----------|
| Settings records | `FooSettings`, `ServiceBusOptions`, `MessageRetryOptions` |
| DI wiring | `Program`, `ServiceCollectionExtensions` |
| **Infra client implementations** | `FooClient`, `MaoPubSubPublisher`, `StoreServiceBusClient` — covered by component/integration tests, not unit-tested in isolation |
| Pure DTOs / deserialization models | Infra `Clients/.../Models/` wire shapes with no behaviour |
| Thin logging wrappers (optional) | `ReceiverLoggingExtensions`, `ProcessorLoggingExtensions` — when many call sites; not required |

**Do not** apply to **App** behavioural code — services, validators, mappers, enrichment steps. App validators are unit-tested; settings records are excluded because they are property bags only.

`{ServiceName}.App.Models` may use assembly-level exclusion in `.csproj` when the project is DTOs only:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" />
</ItemGroup>
```

Details: **write-src-code** general conventions.

---

## Examples

Cross-cutting good/bad patterns: [examples/good-vs-bad.md](examples/good-vs-bad.md)

Production templates: **write-src-code** `examples/`

Test templates: **write-unit-tests**, **write-component-tests**, **write-integration-tests** `examples/`
