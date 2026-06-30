---
name: write-integration-tests
description: >-
  Generate Reqnroll integration tests against live deployed hosts. Use when
  writing smoke or integration tests, runsettings, or blob-polling assertions.
  Enforces @smoke/@integration tags and secrets via env vars.
---

# Write Integration Tests

Integration tests execute against a **live, deployed** host — no mocking. See **write-tests** for the testing pyramid and CI stages.

> **Scope note — examples are demonstration-only.** The webhook / blob-backup-polling / `BlobBackupPoller` / file-driven outbound-event shapes below come from one specific event-driven integration service (`shopifyintegration`) and are **illustrative**. They are **not** required for every service. Implement **only what your service exposes** — e.g. a query/CRUD host may have just a `@smoke` connectivity-and-auth feature plus one `@integration` GET feature (no blob polling, no outbound fixtures). The settings/runsettings/tagging/hooks conventions apply regardless of which subset you use.

**Coverage by environment:** **TST** — `@smoke` plus at least one `@integration` feature per functional flow/feature; **PRD** — `@smoke` only (never `@integration`).

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_smoke-feature.feature](examples/1_smoke-feature.feature) | `@smoke` feature template |
| 2 | [2_runsettings-snippet.xml](examples/2_runsettings-snippet.xml) | Per-environment runsettings |
| 3 | [3_blob-poller-usage.cs](examples/3_blob-poller-usage.cs) | Blob backup polling |

---

## Purpose

Integration tests verify end-to-end behaviour against a live deployed Azure Function App — from webhook receipt through blob storage side effects. They run in CI after deployment and can be run locally with an `integrationtests.local.json` secrets file.

## Project location

All integration tests live in `test/{ServiceName}.IntegrationTests/`.

## Framework

- **Test runner**: NUnit 4 (latest stable) via `Reqnroll.NUnit` 3.3.4
- **HTTP**: plain `HttpClient` with `BaseAddress` set to the deployed function host
- **Blob polling**: `BlobBackupPoller` (tag-based Azure Blob query)
- **Global usings** already active: `NUnit.Framework` (no `using` needed)
- **No Moq** — all dependencies are real; nothing is mocked

---

## Project structure

**Not a mirror of `src/`** — same Reqnroll feature/flow layout as component tests, plus per-environment runsettings. See **write-tests** hub for layout comparison across tiers.

```
test/{ServiceName}.IntegrationTests/
├── Features/
│   ├── EndpointAuthentication/
│   │   └── EndpointAuthentication.feature     # @smoke - auth checks, no function key
│   ├── GetOrders/
│   │   ├── GetOrderFlow.feature               # @integration
│   │   └── GetOrderByInternalOrderIdFlow.feature
│   └── Webhooks/Processors/
│       ├── OrderCreated/
│       │   └── OrderCreated-Files.feature     # @integration - backup blob assertion
│       ├── OrderUpdated/
│       │   └── OrderUpdated-Files.feature
│       └── OrderTransactionCreated/
│           ├── OrderTransactionCreated-AuthoriseAndCapture-Files.feature
│           └── OrderTransactionCreated-Refund-Files.feature
├── StepDefinitions/
│   ├── EndpointAuthenticationStepDefinitions.cs
│   ├── GetOrderFlowStepDefinitions.cs
│   ├── GetOrderByInternalOrderIdFlowStepDefinitions.cs
│   └── OutboundBackupFilesStepDefinitions.cs
├── Support/
│   ├── BlobBackupPoller.cs                # Polls blob container by tag query with timeout
│   ├── Hooks.cs                           # Lifecycle: load settings, create HttpClient
│   ├── IntegrationTestSettings.cs         # Settings: env vars → local file → committed defaults
│   └── JsonFixtureComparer.cs             # Deep JSON equality, same as component tests
├── Scenarios/                             # JSON fixture files
│   ├── OrderCreated/
│   │   └── <ScenarioName>/
│   │       ├── WebhookPayload.json        # Minimal { "id": ..., "name": "..." }
│   │       └── ExpectedOutboundCreatedEvent.json
│   ├── OrderUpdated/
│   │   └── <ScenarioName>/
│   │       ├── WebhookPayload.json
│   │       └── ExpectedOutboundUpdatedEvent.json
│   └── OrderTransactionCreated/
│       └── <ScenarioName>/
│           ├── WebhookPayload.json        # Minimal { "id": ..., "order_id": ... }
│           └── ExpectedOutboundPaymentEvent.json
├── integrationtests.json                  # Committed localhost defaults — used by default in Visual Studio
├── integrationtests.tst.runsettings       # TST: @smoke + @integration
└── integrationtests.prd.runsettings       # PRD: @smoke only
```

---

## Step 1: Tags — control which tests run per environment

Every feature must be tagged with exactly one of:

| Tag | Meaning | Environments |
|---|---|---|
| `@smoke` | No function key, no blob polling. Verifies basic connectivity and auth enforcement. | TST, PRD |
| `@integration` | Requires whatever secrets/settings your scenarios need (e.g. function key, storage connection string). Posts real requests and asserts live side effects. | TST only — **never PRD** |

Place the tag on the `Feature:` line:

```gherkin
@smoke
Feature: Endpoint Authentication
  ...

@integration
Feature: Order Created Processor (File-Driven)
  ...
```

`TestCaseFilter` in the runsettings maps `@smoke` → `Category=smoke` and `@integration` → `Category=integration` (Reqnroll maps feature tags to NUnit categories automatically).

---

## Step 2: Runsettings — one file per environment

Three runsettings files control which tests run and which host URL is used:

| File | Environments tested | Secrets needed |
|---|---|---|
| `integrationtests.tst.runsettings` | TST | Whatever your `@integration` features require — via CI env vars (see below) |
| `integrationtests.prd.runsettings` | PRD | None (`@smoke` only) |

The **host URL** (e.g. `FUNCTIONS_HOST_URL`) can be hardcoded in runsettings — it is not a secret. **Secrets** are never hardcoded in runsettings; the pipeline injects them as environment variables (variable groups).

**Example** (shopifyintegration): `FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING`. Your service defines its own keys in `IntegrationTestSettings` to match what `@integration` scenarios actually use.

Adding a new environment:
1. Create `integrationtests.<env>.runsettings` following the existing template
2. Set `TestCaseFilter` to `Category=smoke|Category=integration` (or `Category=smoke` for production-like envs)
3. Hardcode the host URL env var for that environment (e.g. `FUNCTIONS_HOST_URL`)

---

## Step 3: Settings resolution

`IntegrationTestSettings.Load()` resolves required values in priority order (first wins):

1. **Environment variable** — set by the selected `.runsettings` file or the CI pipeline variable group
2. **`integrationtests.local.json`** — gitignored file next to the `.csproj` for personal overrides/secrets
3. **`integrationtests.json`** — committed, non-secret defaults (e.g. `"FUNCTIONS_HOST_URL": "http://localhost:7071/"`) next to the `.csproj`, copied to the output directory. Used **by default when running in Visual Studio** with no env vars or local file, so a plain run targets a locally running `func start`. Never put secrets here.

Define one property per secret or config value your `@integration` features need. **Names are service-specific** — the example below is from shopifyintegration, not a chapter-wide standard:

```json
{
  "FUNCTIONS_HOST_URL": "https://{service-slug}-tst-af.azurewebsites.net",
  "FUNCTIONS_CODE": "<function-key>",
  "BLOB_CONNECTION_STRING": "<storage-connection-string>"
}
```

Another service might use `API_KEY`, `STORAGE_CONNECTION_STRING`, or additional keys — add them to the record and to the pipeline variable group.

**Never commit** `integrationtests.local.json` — it is gitignored.

---

## Step 4: Feature file structure

### @smoke features — no function key, no blob

```gherkin
@smoke
Feature: Endpoint Authentication
  In order to protect the sample integration webhooks
  As the Azure Functions host
  I want to ensure that receiver endpoints require a valid API key

  Scenario Outline: Endpoints enforce the correct access level
    When I send a <method> request to "<route>"
    Then the response status code should be <code>

    Examples:
      | method | route                             | code |
      | GET    | /api/home                         | 200  |
      | POST   | /api/orders/created               | 401  |
```

- No function key appended — smoke tests deliberately omit auth to verify 401 (or equivalent) responses
- Uses `HttpClient` without auth headers; base URL comes from the host URL setting (e.g. `FUNCTIONS_HOST_URL`) only

### @integration features - file-driven, blob assertion

Each processor has its own feature file under `Features/Webhooks/Processors/{Name}/`. The **file name** and **`Feature:` title** must match (e.g. `OrderCreated-Files.feature` → `Feature: Order Created Processor (File-Driven)`).

| Feature file | `Feature:` title | Webhook route |
|---|---|---|
| `OrderCreated-Files.feature` | `Order Created Processor (File-Driven)` | `/api/orders/created` |
| `OrderUpdated-Files.feature` | `Order Updated Processor (File-Driven)` | `/api/orders/updated` |
| `OrderTransactionCreated-AuthoriseAndCapture-Files.feature` | `Order Transaction Created Processor Authorise And Capture (File-Driven)` | `/api/orders/transactions/created` |
| `OrderTransactionCreated-Refund-Files.feature` | `Order Transaction Created Processor Refund (File-Driven)` | `/api/orders/transactions/created` |

Use a regular hyphen `-` in scenario outline titles and fixture descriptions, not an en-dash.

```gherkin
@integration
Feature: Order Created Processor (File-Driven)
  End-to-end integration tests that hit the deployed Function App, wait for the outbound backup
  blob to land in the storage container, then verify its content against a fixture file.
  Each sub-folder under Scenarios/OrderCreated is a separate test case containing:
    - WebhookPayload.json           - the minimal sample webhook body { id, name }
    - ExpectedOutboundCreatedEvent.json  - the exact outbound order the backup blob must contain

  Scenario Outline: <scenarioFolder> - Order created backup blob matches fixture
    Given the order created scenario is loaded from folder "<scenarioFolder>"
    When the order created webhook is sent to the deployed function
    Then the outbound order backup blob matches the expected fixture

    Examples:
      | scenarioFolder              |
      | ShipToStore-ReadyToWearItem |
```

Key points:
- `WebhookPayload.json` is minimal — the deployed app fetches full details from external APIs where applicable
- The `When` step appends auth to the route as your service requires (e.g. `?code={Settings.FunctionsCode}` when using Azure Functions keys)
- The `Then` step polls for side effects (e.g. blob backup) using connection settings from `IntegrationTestSettings`

### @integration query endpoints

GET endpoints are tested under `Features/GetOrders/`:

| Feature file | Tag | What it verifies |
|---|---|---|
| `GetOrderFlow.feature` | `@integration` | Known sample order returns 200 with valid name |
| `GetOrderByInternalOrderIdFlow.feature` | `@integration` | Internal order ID resolution + 400 on missing param |

These use hardcoded order IDs from the test environment (not file-driven fixtures). Step definitions: `GetOrderFlowStepDefinitions.cs`, `GetOrderByInternalOrderIdFlowStepDefinitions.cs`.

---

## Step 5: Scenario fixture files

### Folder layout

```
Scenarios/
├── OrderCreated/
│   └── MyScenarioName/
│       ├── WebhookPayload.json
│       └── ExpectedOutboundCreatedEvent.json
├── OrderUpdated/
│   └── MyScenarioName/
│       ├── WebhookPayload.json
│       └── ExpectedOutboundUpdatedEvent.json
└── OrderTransactionCreated/
    └── MyScenarioName/
        ├── WebhookPayload.json
        └── ExpectedOutboundPaymentEvent.json
```

### `WebhookPayload.json`

Minimal sample webhook body. The deployed app fetches the full order details from the real sample GraphQL API:

```json
{
    "id": 6833372823808,
    "name": "SHA1758"
}
```

### `ExpectedOutboundCreatedEvent.json` / `ExpectedOutboundUpdatedEvent.json`

The exact JSON the outbound backup blob must contain. Use `null` for fields that are non-deterministic (or add them to `IgnoredJsonProperties` in the step definitions class):

- `TaxDetailId` - `Guid.NewGuid()` at runtime, always ignored for OrderCreated
- `CreatedTimestamp`, `UpdatedTimestamp` - `DateTime.UtcNow` at runtime, always ignored for OrderUpdated
- `AlterationId`, billing address fields - ignored where live API data is non-deterministic (see `OutboundBackupFilesStepDefinitions.cs`)

Adding a new file-driven scenario:
1. Create fixture files under `Scenarios/{Domain}/<ScenarioName>/`
2. Add the scenario name to the `Examples` table in the matching processor feature file (e.g. `OrderCreated-Files.feature`)
3. If new non-deterministic fields appear, add them to the appropriate `IgnoredJsonProperties` set in `OutboundBackupFilesStepDefinitions.cs`

---

## Step 6: Step definitions class structure

```csharp
using Reqnroll;
using {ServiceName}.IntegrationTests.Support;

namespace {ServiceName}.IntegrationTests.StepDefinitions;

[Binding]
public sealed class MyFlowStepDefinitions(FeatureContext featureContext, ScenarioContext scenarioContext)
{
    private IntegrationTestSettings Settings => featureContext.Get<IntegrationTestSettings>(Hooks.SettingsKey);
    private HttpClient HttpClient => scenarioContext.Get<HttpClient>(Hooks.HttpClientKey);

    [Given(@"...")]
    public void GivenSomething() { ... }

    [When(@"...")]
    public async Task WhenSomething() { ... }

    [Then(@"...")]
    public async Task ThenSomething() { ... }
}
```

Conventions:
- Primary constructor with `FeatureContext featureContext, ScenarioContext scenarioContext`
- `public sealed` with `[Binding]`
- Access settings via `featureContext.Get<IntegrationTestSettings>(Hooks.SettingsKey)` (available in all features — `@smoke` and `@integration` alike)
- Access `HttpClient` via `scenarioContext.Get<HttpClient>(Hooks.HttpClientKey)`
- Use `Hooks.SettingsKey`, `Hooks.HttpClientKey`, `Hooks.ResponseKey` for context keys (not a separate `ScenarioContextKeys` class)
- For inline scenario context, use string literals as keys (e.g. `"WebhookPayload"`, `"ExpectedOutboundFixture"`, `"TestStartedAt"`)

---

## Step 7: Hooks and lifecycle

| Hook | Tag filter | Scope | What it does |
|---|---|---|---|
| `BeforeFeature` | All (no tag) | Feature | Calls `IntegrationTestSettings.Load()`, stores in `FeatureContext` |
| `BeforeScenario` | All (no tag) | Scenario | Reads loaded settings; creates `HttpClient { BaseAddress }` |
| `AfterScenario` | All | Scenario | Disposes `HttpClient` |

**One path for all features.** `@smoke` and `@integration` are handled identically — both load settings in `BeforeFeature` and build the `HttpClient` in `BeforeScenario`. Do not branch the hooks per tag; the tags only drive which features run per environment (via the runsettings `TestCaseFilter`). The host URL always resolves the same way (env var → local file → committed default).

---

## Step 8: BlobBackupPoller

`BlobBackupPoller` finds blobs using Azure Blob tag-based queries and a polling loop:

```csharp
var poller = new BlobBackupPoller(Settings.BlobConnectionString, IntegrationTestSettings.OutboundEventsBackupContainerName);
var blobContent = await poller.PollForContentAsync(orderId, maoEventType, testStartedAt, timeout);
```

- `orderId` - the outbound `OrderId` field from the expected fixture (e.g. `"SHA1758"`)
- `maoEventType` - `"Create"`, `"UpdateOrderNote"`, `"SavePaymentHeader"`, or `"UpdatePaymentTransaction"`
- `testStartedAt` - captured as `DateTimeOffset.UtcNow` just before the webhook is sent
- `timeout` — defaults to **120 seconds** in the step definitions
- Polls every **4 seconds**; throws `TimeoutException` if no matching blob appears

---

## Step 9: JsonFixtureComparer

Identical behaviour to the component test comparer (same implementation, different namespace). Deep JSON equality from the perspective of the expected fixture: iterates expected keys only, allows extra keys on actual, compares numbers by decimal value.

---

## Step 10: CI integration

Integration tests run automatically after deployment (e.g. `devops/azurepipelines/azure-pipeline.yaml`):

- **TST deploy stage** — runs `@smoke` + `@integration`; pipeline injects secrets as env vars (variable group per environment)
- **PRD deploy stage** — runs `@smoke` only (`integrationtests.prd.runsettings`)

Map each `IntegrationTestSettings` property to a pipeline variable — never commit secret values. Example variable names from shopifyintegration: `FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING`.

---

## Complete example: adding a new @integration flow

### 1. Add scenario files

```
Scenarios/OrderCreated/CustomMade-NL/
├── WebhookPayload.json          → {"id": 7001234567890, "name": "SHA2100"}
└── ExpectedOutboundCreatedEvent.json → full expected outbound JSON
```

### 2. Add the row to the matching processor feature file

```gherkin
# Features/Webhooks/Processors/OrderCreated/OrderCreated-Files.feature
    Examples:
      | scenarioFolder              |
      | ShipToStore-ReadyToWearItem |
      | CustomMade-NL               |
```

No step changes required — the file-driven steps are generic.

### 3. If adding a wholly new flow (new webhook type)

Add new `Given`/`When`/`Then` steps in a new `{FlowName}StepDefinitions.cs`:

```csharp
[Given(@"the refund scenario is loaded from folder ""(.*)""")]
public void GivenRefundScenarioIsLoaded(string folderName) { ... }

[When(@"the refund created webhook is sent to the deployed function")]
public async Task WhenRefundWebhookIsSent()
{
    var payload = scenarioContext.Get<JsonNode>("WebhookPayload");
    var url = $"/api/refunds/created?code={Settings.FunctionsCode}"; // adapt auth query param to your host
    var response = await HttpClient.PostAsync(url, new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
    Assert.That((int)response.StatusCode, Is.EqualTo(202));
}

[Then(@"the refund backup blob matches the expected fixture")]
public async Task ThenRefundBlobMatchesFixture() { ... }
```

---

## Checklist before writing integration tests

- [ ] Feature tagged `@smoke` or `@integration` (not both)
- [ ] Feature file is under the correct folder and its `Feature:` title matches the file name
- [ ] `@integration` features are never added to `integrationtests.prd.runsettings`
- [ ] `WebhookPayload.json` is minimal (no full order/transaction data)
- [ ] Expected fixture verified against a real test run
- [ ] Non-deterministic fields (generated GUIDs, timestamps) added to `IgnoredJsonProperties`
- [ ] Auth on webhook/query routes uses settings from `IntegrationTestSettings` (not hardcoded)
- [ ] `testStartedAt` captured immediately before the triggering HTTP call (not before scenario setup)
- [ ] Side-effect polling (blob, etc.) uses the correct tags/keys for the flow under test
- [ ] Scenario outline titles and descriptions use `-`, not en-dashes
- [ ] Secrets never hardcoded — env vars (CI) or `integrationtests.local.json` (local) only; names are service-specific (e.g. shopifyintegration uses `FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING`)
- [ ] Step definitions class is `public sealed` with `[Binding]` and primary constructor
