# `src/` and `test/` folder structure

> Reference **1** — Per-project folder trees (`src/` and `test/`), horizontal vs vertical layout, and dependency direction.

Namespace mirrors folder path: `{ServiceName}.<Layer>.<Feature>.<Sub>`.

---

## Five projects (all services)

```
src/
├── {ServiceName}.Api/              # Host (Azure Functions or ASP.NET Web App)
├── {ServiceName}.Api.Models/       # Published HTTP contracts (NuGet when applicable)
├── {ServiceName}.App/              # Business logic
├── {ServiceName}.App.Models/       # Domain models
└── {ServiceName}.Infra/            # Infrastructure implementations
```

Dependency direction: `Api` → `Infra` → `App` → `App.Models`. `Api.Models` stands alone.

---

## Folder layout — horizontal vs vertical

Choose by **how unrelated the features are**, not by host type.

| Layout | When | App organization |
|--------|------|------------------|
| **Horizontal** (default) | Single-purpose **microservice** — one bounded context, one main flow | Layer folders: `Services/`, `Clients/Interfaces/`, `Extensions/` (+ `Enrichment/` when needed) |
| **Vertical** | **Multiple unrelated features** in one deployable (internal tools, portals) | Feature folders repeated per layer: `VersionOverview/`, `BulkReplay/`, … |

Both layouts must still follow chapter rules: **`Interfaces/`** subfolders for contracts, layer boundaries, one client per downstream (horizontal: `App/Clients/Interfaces/` + `Infra/Clients/{Name}/`; vertical: feature-scoped `Clients/Interfaces/` or `Repository/Interfaces/` with matching Infra folder).

**Add folders when the service needs them** — not every backend uses every folder below. Match structure to what you are building:

| You need… | Typical folders |
|-----------|-----------------|
| HTTP webhook or event ingest | `Api/Functions/Receivers/`, `App/Services/Receivers/` |
| Async queue processing | `Api/Functions/Processors/`, `App/Services/Processors/`, optional `Api/Messaging/` |
| Fetch related data before publish | `App/Enrichment/` |
| HTTP read/query API | `Api/Functions/Queries/` or `Controllers/` — no receivers/processors |

Example of the full webhook → queue → enrich → publish flow: [14_integration-service-patterns.md](14_integration-service-patterns.md). A query API or Web App CRUD service uses only the folders that apply to its flow.

---

## Horizontal layout (typical microservice)

### `{ServiceName}.Api`

```
{ServiceName}.Api/
├── Program.cs
├── host.json                       # Functions only
├── local.settings.json             # Functions only (gitignored)
├── {ServiceName}.Api.csproj
├── Functions/                      # Azure Functions only — omit for Web App
│   ├── Receivers/                  # HTTP triggers (when applicable)
│   ├── Processors/                 # Service Bus triggers (when applicable)
│   └── Queries/                    # HTTP read APIs (when applicable)
├── Controllers/                    # Web App only — or feature subfolders
├── Mappers/
│   └── v1/                         # API version in folder when needed — not in class names
│       ├── FooWebhookMapper.cs
│       └── Interfaces/
│           └── IFooWebhookMapper.cs
└── Messaging/                      # Host-only infra (e.g. retry scheduler) — when applicable
    ├── Interfaces/
    ├── Settings/
    └── Validators/
```

| Folder | Purpose |
|--------|---------|
| `Functions/*` or `Controllers/` | HTTP / messaging entry points — delegate to App services |
| `Mappers/` | Domain ↔ `Api.Models` at HTTP boundary |
| `Messaging/` | Api-layer messaging infrastructure — not business logic |

### `{ServiceName}.Api.Models`

```
{ServiceName}.Api.Models/
├── {ServiceName}.Api.Models.csproj
└── {Feature}/
    └── Transport/
        ├── Models/                   # shared wire types referenced by requests/responses
        ├── Requests/
        └── Responses/
```

API versioning (`v1`, `v2`, …) belongs in the **Api** project folder structure (`Controllers/v1/`, `Mappers/v1/`, `Validators/v1/Transport/`) — **not** in `Api.Models` paths and **not** in type names (no `FooMapperV1`, `GetOrderRequestV2`).

### `{ServiceName}.App` (horizontal)

```
{ServiceName}.App/
├── Clients/
│   └── Interfaces/                 # I{Name}Client, I{Name}Publisher — one per downstream
├── Extensions/                     # {Type}Extensions — NOT *Helper classes
└── Services/
    ├── Interfaces/
    ├── {Feature}Service.cs
    └── …                           # optional: Receivers/, Processors/, Queries/ subfolders
```

| Folder | Purpose |
|--------|---------|
| `Clients/Interfaces/` | Downstream contracts — **domain types only** |
| `Services/` | Use cases and orchestration |
| `Extensions/` | Pure logic on domain types |

Add `Enrichment/` when the service enriches data before publishing. Add `Services/Receivers/` or `Services/Processors/` when the App layer has that role — see the table above. Query APIs and simple Web Apps often need neither. **No `App/Mappers/`** — shape translation lives in `Api/Mappers/` or `Infra/Clients/.../Mappers/`.

### `{ServiceName}.App.Models`

```
{ServiceName}.App.Models/
├── {Feature}/
│   └── Models/                     # Domain types grouped by bounded context
└── …
```

Domain models for the App layer. Wire DTOs from external APIs belong in **Infra** `Clients/.../Models/`, not here.

### `{ServiceName}.Infra`

```
{ServiceName}.Infra/
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # AddInfrastructure(config)
├── Validators/
│   └── FluentValidateOptions.cs
└── Clients/
    └── {ClientName}/
        ├── {ClientName}.cs              # internal sealed
        ├── Mappers/                     # optional — domain → wire DTO
        ├── Settings/
        ├── Validators/
        └── Models/                      # Wire DTOs — Infra only
```

One folder per downstream component. See [4_downstream-clients.md](4_downstream-clients.md).

---

## Vertical layout (multi-feature services)

Same five projects; repeat the **feature name** as the top-level folder in each layer (`BulkReplay/`, `VersionOverview/`, …). Use when features share a host but have little shared domain logic. Prefer horizontal layout when the service has one primary flow.

Repo example: `fulfillmenttools` (`ItFfTools.*`).

### Feature slice across layers (example: `BulkReplay`)

```
{ServiceName}.Api/
└── BulkReplay/
    ├── Controllers/v1/
    ├── Mappers/v1/
    ├── Validators/v1/Transport/
    └── SwaggerExamples/              # optional

{ServiceName}.Api.Models/
└── BulkReplay/
    └── Transport/
        ├── Requests/
        └── Responses/

{ServiceName}.App/
└── BulkReplay/
    ├── Services/                     # IBulkReplayService + BulkReplayService
    ├── Settings/
    ├── Validators/Settings/
    └── Clients/
        └── Interfaces/               # IReplayClient

{ServiceName}.App.Models/
└── BulkReplay/
    └── Models/
        └── ReplayExecutionResult.cs

{ServiceName}.Infra/
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Validators/
│   └── FluentValidateOptions.cs
└── BulkReplay/
    └── HttpClients/
        └── ReplayClient.cs
```

Other features (`VersionOverview/`, `RetrieveAllBlobs/`, …) follow the same pattern at the same depth in each project.

---

## Namespace ↔ folder rule

```
src/Foo.App/Services/FooService.cs → namespace Foo.App.Services;
```

File-scoped namespaces; folder path must match exactly (IDE0130).

---

## What goes where (quick reference)

| Concern | Project / path |
|---------|----------------|
| HTTP entry (Functions) | `Api/Functions/` |
| HTTP entry (Web App) | `Api/Controllers/` or `Api/{Feature}/Controllers/` |
| HTTP read/query (when applicable) | `Api/Functions/Queries/` or `Api/Controllers/` |
| Webhook ingest (when applicable) | `Api/Functions/Receivers/`, `App/Services/Receivers/` |
| Queue processor (when applicable) | `Api/Functions/Processors/`, `App/Services/Processors/` |
| Pre-publish enrichment (when applicable) | `App/Enrichment/` |
| Service Bus retry scheduler (when applicable) | `Api/Messaging/` |
| Public API request DTO | `Api.Models/{Feature}/Transport/Requests/` |
| Public API response DTO | `Api.Models/{Feature}/Transport/Responses/` |
| Domain model | `App.Models/{Feature}/Models/` |
| Business orchestration | `App/Services/` or `App/{Feature}/Services/` |
| Client interface | `App/Clients/Interfaces/` (horizontal) or `App/{Feature}/Clients/Interfaces/` (vertical) |
| Client implementation | `Infra/Clients/{Name}/` (horizontal) or `Infra/{Feature}/HttpClients/` (vertical) |
| Api boundary mapper | `Api/Mappers/v1/Interfaces/` (or `Api/Mappers/Interfaces/` when unversioned) |
| Infra boundary mapper | `Infra/Clients/{Name}/Mappers/` (or inline in client) |
| Settings + FluentValidation | `*/Settings/` + `*/Validators/` |
| DI registration (infra) | `Infra/Extensions/ServiceCollectionExtensions.cs` |
| DI registration (host) | `Api/Program.cs` |

Webhook → queue → process → publish example: [14_integration-service-patterns.md](14_integration-service-patterns.md).

---

## `test/` folder structure

### Three test projects (all services)

```
test/
├── {ServiceName}.UnitTests/        # Isolated class tests — mirror src/ layout
├── {ServiceName}.ComponentTests/   # In-process end-to-end — Reqnroll, mocked Infra
└── {ServiceName}.IntegrationTests/ # Live deployed host — Reqnroll, no mocks
```

| Project | Layout rule | Details |
|---------|-------------|---------|
| **UnitTests** | **Mirror `src/`** — same folder path and namespace under `test/{ServiceName}.UnitTests/` | `src/.../Api/Functions/Receivers/FooReceiver.cs` → `test/.../UnitTests/Api/Functions/Receivers/FooReceiverTests.cs` |
| **ComponentTests** | **Feature-oriented** — `Features/`, `StepDefinitions/`, `Support/`, `Scenarios/` | `WebApplicationFactory<Program>`; Infra fully mocked |
| **IntegrationTests** | Same as component — **fewer scenarios**, live host | `@smoke` / `@integration` tags; `*.runsettings` per environment |

All test projects use the chapter `.csproj` templates (`IsPackable` false; coverlet on unit/component). See [5_csproj.md](5_csproj.md).

### `{ServiceName}.UnitTests`

```
test/{ServiceName}.UnitTests/
├── AssemblyInfo.cs                   # InstancePerTestCase
├── Helpers/
│   ├── FixtureFactory.cs             # registers ICustomization / specimen builders
│   ├── Customizations/               # e.g. PresentmentMoneyCustomization.cs
│   └── ArgumentsNullChecker.cs
├── Api/                              # mirrors src/{ServiceName}.Api/
│   ├── Functions/
│   └── Mappers/
├── App/                              # mirrors src/{ServiceName}.App/
│   ├── Services/
│   └── Enrichment/
└── Infra/                            # mirrors src/{ServiceName}.Infra/
    └── Clients/
        └── Mappers/
```

Namespace: `{ServiceName}.UnitTests.<mirrored-path>` (e.g. `{ServiceName}.UnitTests.Api.Functions.Receivers`).

Details: **write-unit-tests**.

### `{ServiceName}.ComponentTests`

```
test/{ServiceName}.ComponentTests/
├── Features/                         # Gherkin — grouped by concern / function
│   ├── GetOrders/
│   └── Webhooks/
│       ├── Receivers/
│       └── Processors/{Name}/
├── StepDefinitions/                  # Shared [Binding] classes
├── Support/
│   ├── ApplicationFactory.cs
│   ├── Hooks.cs
│   └── JsonFixtureComparer.cs
└── Scenarios/                        # JSON fixtures for file-driven tests
    └── {Domain}/{ScenarioName}/
```

Details: **write-component-tests**.

### `{ServiceName}.IntegrationTests`

```
test/{ServiceName}.IntegrationTests/
├── Features/                         # @smoke and @integration tags
├── StepDefinitions/
├── Support/
│   ├── BlobBackupPoller.cs
│   ├── Hooks.cs
│   └── IntegrationTestSettings.cs
├── Scenarios/
├── integrationtests.tst.runsettings
├── integrationtests.acc.runsettings
└── integrationtests.prd.runsettings  # @smoke only
```

Details: **write-integration-tests**. Pyramid and when to use each tier: **write-tests**.

### What goes where (tests)

| Concern | Project / path |
|---------|----------------|
| Unit test class | `test/{ServiceName}.UnitTests/` — path mirrors `src/` |
| Component / integration feature | `test/.../Features/{concern}/` |
| File-driven test fixtures | `test/.../ComponentTests/Scenarios/` or `IntegrationTests/Scenarios/` |

---

## Related references

- Layer boundaries: [2_layer-boundaries.md](2_layer-boundaries.md)
- Interface placement: [3_interfaces.md](3_interfaces.md)
- Downstream clients: [4_downstream-clients.md](4_downstream-clients.md)
- `.csproj` PropertyGroups: [5_csproj.md](5_csproj.md)
- Integration examples: [14_integration-service-patterns.md](14_integration-service-patterns.md)
