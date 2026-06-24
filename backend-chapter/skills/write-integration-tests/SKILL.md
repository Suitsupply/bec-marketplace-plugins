---
name: write-integration-tests
description: >-
  Generate Reqnroll integration tests against live deployed hosts. Use when
  writing smoke or integration tests, runsettings, or blob-polling assertions.
  Enforces @smoke/@integration tags and secrets via env vars.
---

# Write Integration Tests

Integration tests execute against a **live, deployed** host — no mocking. See **write-tests** for the testing pyramid and CI stages.

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

- **Test runner**: NUnit 3 via `Reqnroll.NUnit` 3.3.4
- **HTTP**: plain `HttpClient` with `BaseAddress` set to the deployed function host
- **Blob polling**: `BlobBackupPoller` (tag-based Azure Blob query)
- **Global usings** already active: `NUnit.Framework` (no `using` needed)
- **No Moq** — all dependencies are real; nothing is mocked

---

## Project structure

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
│   ├── IntegrationTestSettings.cs         # Settings: env vars → local file fallback
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
├── integrationtests.tst.runsettings       # TST: @smoke + @integration
├── integrationtests.acc.runsettings       # ACC: @smoke + @integration
└── integrationtests.prd.runsettings       # PRD: @smoke only
```

---

## Step 1: Tags — control which tests run per environment

Every feature must be tagged with exactly one of:

| Tag | Meaning | Environments |
|---|---|---|
| `@smoke` | No function key, no blob polling. Verifies basic connectivity and auth enforcement. | TST, ACC, PRD |
| `@integration` | Requires `FUNCTIONS_CODE` and `BLOB_CONNECTION_STRING`. Posts a real webhook and asserts blob output. | TST, ACC only — **never PRD** |

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
| `integrationtests.tst.runsettings` | TST | `FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING` (from CI) |
| `integrationtests.acc.runsettings` | ACC | `FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING` (from CI) |
| `integrationtests.prd.runsettings` | PRD | None (`@smoke` only) |

`FUNCTIONS_HOST_URL` is hardcoded in the runsettings (non-secret). All secrets come **exclusively** from the CI pipeline `env:` block (variable groups). Never hardcode secrets in runsettings.

Adding a new environment:
1. Create `integrationtests.<env>.runsettings` following the existing template
2. Set `TestCaseFilter` to `Category=smoke|Category=integration` (or `Category=smoke` for production-like envs)
3. Hardcode `FUNCTIONS_HOST_URL` for that environment

---

## Step 3: Settings resolution

`IntegrationTestSettings.Load()` resolves three required values in priority order:

1. **Environment variable** — set by CI pipeline via variable group
2. **`integrationtests.local.json`** — gitignored file next to the `.csproj` for local development

```json
{
  "FUNCTIONS_HOST_URL": "https://{service-slug}-tst-af.azurewebsites.net",
  "FUNCTIONS_CODE":     "<your-function-key>",
  "BLOB_CONNECTION_STRING": "<your-storage-connection-string>"
}
```

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

- No `FUNCTIONS_CODE` appended — smoke tests deliberately omit the key to verify 401 responses
- Uses `HttpClient` without auth headers; base URL comes from `FUNCTIONS_HOST_URL` only

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
- `WebhookPayload.json` is minimal - the deployed app fetches full order/transaction details from the external API
- The step appends `?code=<FUNCTIONS_CODE>` to the route automatically
- The `Then` step polls blob storage for up to **120 seconds**, finding the blob by tag query (`orderId` + `maoEventType`)
- `maoEventType` tag values: `"Create"` (OrderCreated), `"UpdateOrderNote"` (OrderUpdated), `"SavePaymentHeader"` / `"UpdatePaymentTransaction"` (OrderTransactionCreated)

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
- Access settings via `featureContext.Get<IntegrationTestSettings>(Hooks.SettingsKey)` (only available in `@integration` features)
- Access `HttpClient` via `scenarioContext.Get<HttpClient>(Hooks.HttpClientKey)`
- Use `Hooks.SettingsKey`, `Hooks.HttpClientKey`, `Hooks.ResponseKey` for context keys (not a separate `ScenarioContextKeys` class)
- For inline scenario context, use string literals as keys (e.g. `"WebhookPayload"`, `"ExpectedOutboundFixture"`, `"TestStartedAt"`)

---

## Step 7: Hooks and lifecycle

| Hook | Tag filter | Scope | What it does |
|---|---|---|---|
| `BeforeFeature("integration")` | `@integration` only | Feature | Calls `IntegrationTestSettings.Load()`, stores in `FeatureContext` |
| `BeforeScenario` | All | Scenario | Resolves base URL; creates `HttpClient { BaseAddress }` |
| `AfterScenario` | All | Scenario | Disposes `HttpClient` |

`@smoke` scenarios do not trigger `BeforeFeature("integration")` — they use `FUNCTIONS_HOST_URL` directly from the environment variable.

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

Integration tests run automatically after TST deployment via `devops/azurepipelines/azure-pipeline.yaml`:

- **TST deploy stage** - runs `@smoke` + `@integration` with secrets from variable group `write-tests-tst`
- **PRD deploy stage** - runs `@smoke` only (`integrationtests.prd.runsettings`)

Secrets (`FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING`) are injected as pipeline environment variables, never committed.

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
    var url = $"/api/refunds/created?code={Settings.FunctionsCode}";
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
- [ ] `?code={Settings.FunctionsCode}` appended to all webhook POST routes
- [ ] `testStartedAt` captured immediately before the webhook call (not before scenario setup)
- [ ] Blob polling uses the correct `maoEventType` for the processor under test
- [ ] Scenario outline titles and descriptions use `-`, not en-dashes
- [ ] Secrets (`FUNCTIONS_CODE`, `BLOB_CONNECTION_STRING`) never hardcoded - env vars or `integrationtests.local.json` only
- [ ] Step definitions class is `public sealed` with `[Binding]` and primary constructor
