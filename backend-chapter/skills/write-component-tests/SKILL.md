---
name: write-component-tests
description: >-
  Generate Reqnroll/Gherkin component tests following Backend Chapter conventions.
  Use when asked to write component tests, feature files, or step definitions.
  Enforces ApplicationFactory mocking, feature file naming, and Scenarios layout.
---

# Write Component Tests

Component tests verify an Azure Function end-to-end in-process, with all infrastructure dependencies replaced by `Mock<>` objects — including blob storage, Service Bus, **downstream HTTP clients** (`I*Client` from `App/Clients/Interfaces/`), and outbound publishers. **Use real Api/Infra mappers** (same as unit tests) — only external I/O is mocked. See **write-tests** for the testing pyramid.

> **Scope note — examples are demonstration-only.** The blob-storage / outbound-publisher / processor / file-driven-`Scenarios` / `ReceiverEndpoints` shapes below are drawn from one specific event-driven integration service (`shopifyintegration`) and are **illustrative**. They are **not** mandatory for every service. Mock and test **only the infrastructure your host actually has** — a simple query/CRUD service typically has just one or two `I*Client` mocks and a few GET/POST routes (no blob, no publisher, no file-driven fixtures). Apply the conventions (naming, `ApplicationFactory`, hooks, untagged features) to whatever subset you need.

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_feature-file.feature](examples/1_feature-file.feature) | Gherkin feature file template |
| 2 | [2_step-definitions.cs](examples/2_step-definitions.cs) | Step definitions binding |
| 3 | [3_application-factory-snippet.cs](examples/3_application-factory-snippet.cs) | `ApplicationFactory`, `Hooks`, `ScenarioContextKeys` |

---

## Purpose

Component tests run with `WebApplicationFactory<Program>` and use Reqnroll (Gherkin) as the test specification language. All infrastructure dependencies — blob storage, Service Bus, **downstream HTTP clients**, external APIs, outbound publishers, etc. — are replaced by `Mock<>` objects in `ApplicationFactory`.

## Project location

All component tests live in `test/{ServiceName}.ComponentTests/`.

## Framework

- **Test runner**: NUnit 4 (latest stable) via `Reqnroll.NUnit` 3.3.4
- **Mocking**: Moq 4
- **Host**: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`)
- **Global usings** already active: `Moq`, `NUnit.Framework` (no need to add `using` for these)
- **No tags**: component feature files are **never** tagged with `@smoke` / `@integration` — those tags are integration-tier only (they drive the `.runsettings` `TestCaseFilter`, which component tests do not use)

---

## Project structure

**Not a mirror of `src/`** — organize by **resource** (and optional vertical slice, e.g. `Example/Person/`). See **write-tests** hub for layout comparison across tiers.

```
test/{ServiceName}.ComponentTests/
├── Features/
│   ├── Example/                              # optional vertical slice name
│   │   └── Person/
│   │       ├── GetPersonFlow.feature
│   │       └── PersonRequested.feature
│   └── Order/                                # or at Features root when no slice
│       ├── OrderCreated.feature
│       ├── OrderCreated-Files.feature
│       ├── OrderCreated-Receiver.feature     # Service Bus backup receiver (if any)
│       └── ReceiverEndpoints.feature         # optional — grouped HTTP receivers for one resource
├── StepDefinitions/                   # shared [Binding] classes (not 1:1 with feature files)
│   ├── ReceiverEndpointsStepDefinitions.cs
│   ├── {FlowName}ProcessorFlowStepDefinitions.cs
│   ├── {FlowName}ProcessorFlowFilesStepDefinitions.cs
│   └── ...
├── Support/
│   ├── ApplicationFactory.cs          # WebApplicationFactory with all mocks
│   ├── Hooks.cs                       # BeforeFeature/BeforeScenario lifecycle
│   ├── ScenarioContextKeys.cs         # constant keys for ScenarioContext/FeatureContext
│   ├── TestDataBuilder.cs             # optional — in-memory domain model factories
│   └── JsonFixtureComparer.cs         # JSON fixture deep-equality comparer
└── Scenarios/                         # JSON fixtures for file-driven tests
    └── {FlowName}/
        └── <ScenarioName>/
            ├── SourceInput.json         # upstream / enrichment input shape
            └── ExpectedOutbound.json    # expected publish / wire DTO
```

Add folders only for flows that exist in the service — not every processor needs every suffix (`-Files`, `-ServiceBusFailures`, `-UnhappyFlow`).

---

## Step 1: Feature file naming convention

Feature files live under `Features/{Resource}/` (or `Features/{Slice}/{Resource}/`). The **file name** and **`Feature:` title** must describe the same Azure Function.

| Function type | Path | Feature file(s) | `Feature:` title |
|---|---|---|---|
| HTTP receivers (grouped) | `{Resource}/` | `ReceiverEndpoints.feature` | `Receiver Endpoints` |
| Service Bus receiver | `{Resource}/` | `{Name}-Receiver.feature` | `{Name} Receiver` |
| Processor (programmatic) | `{Resource}/` | `{FlowName}.feature` | `{FlowName} Processor` |
| Processor (file-driven) | `{Resource}/` | `{FlowName}-Files.feature` | `{FlowName} Processor (File-Driven)` |
| Processor (Service Bus failures) | `{Resource}/` | `{FlowName}-ServiceBusFailures.feature` | `{FlowName} Processor Service Bus Failures` |
| Processor (unhappy path) | `{Resource}/` | `{FlowName}-UnhappyFlow.feature` | `{FlowName} Processor Unhappy Flow` |
| Query endpoints | `{Resource}/` | `Get{Resource}Flow.feature` | `Get {Resource} Flow` |

**Examples:** `FooCreated.feature` → `Feature: Foo Created Processor`; `FooCreated-Files.feature` → `Feature: Foo Created Processor (File-Driven)`.

**Grouping rule**: HTTP receivers that share identical behaviour (accept POST → blob → Service Bus → 202) are covered together in `ReceiverEndpoints.feature` using a `Scenario Outline`.

**One-per-function rule**: processors and Service Bus receivers with distinct retry/dead-letter logic each get their own feature file(s) under the matching processor folder.

**Step definitions file rule**: step definition classes are shared across features. Create a new `{Flow}StepDefinitions.cs` only when new steps are needed — reuse existing `[Binding]` classes first.

**Gherkin punctuation**: use a regular hyphen `-` in scenario outline titles and fixture descriptions, not an en-dash.

### Processor `When` step wording

Use the full processor / flow name in steps so scenarios read unambiguously:

| Style | Example step |
|---|---|
| Programmatic | `the foo created processor receives resource {id}` |
| File-driven | `the foo created processor processes the file-driven scenario` |
| Service Bus failure | `the foo created processor receives resource {id} at Service Bus delivery count {n}` |

Match the domain vocabulary of the service (`order`, `shipment`, `transaction`, etc.) — keep wording consistent within a flow.

---

## Step 2: Feature file structure

### HTTP receiver features

HTTP receivers with shared behaviour are tested together in `Features/{Resource}/ReceiverEndpoints.feature` (not one feature file per receiver):

```gherkin
Feature: Receiver Endpoints
  ...

  Scenario Outline: A valid webhook payload is processed successfully
    Given the request body is '<payload>'
    When I send a POST request to "<route>"
    Then the response status code should be 202
    And the blob storage client should have received 1 upload for event '<eventType>' tagged with resourceId '<resourceId>'
    And the service bus client should have received 1 message for event '<eventType>' with the request body and message id '<messageId>'

    Examples:
      | route                 | payload                | eventType    | messageId | resourceId |
      | /api/foo/created      | {"id":1,"name":"1001"} | FooCreated   | 1         | 1          |
      | /api/bar/updated      | {"id":2,"name":"1002"} | BarUpdated   | 2         | 2          |
```

Add a row per receiver route. Document which JSON field supplies `resourceId` for each payload shape in a comment or table in the feature file.

All grouped receivers reuse steps from `ReceiverEndpointsStepDefinitions.cs` — no new step definition file is needed unless behaviour diverges.

### Processor features (programmatic style)

Programmatic scenarios build domain or upstream models in-memory (via `TestDataBuilder` or `Fixture`) and verify the published outbound payload field by field.

```gherkin
Feature: Foo Created Processor
  ...

  Background:
    Given the outbound publisher returns message id "pub-sub-test-id"

  Scenario: Standard resource is processed, published and backed up
    Given the sample resource 10001 is configured for the happy path
    And the downstream API returns the expected lookup for code "STANDARD"
    When the foo created processor receives resource 10001
    Then the outbound publisher is called once
    And the published outbound event has the expected field values
    And Service Bus receives a backup message with message id "pub-sub-test-id"
```

Key conventions:
- One `Background` step sets up the publisher mock and captures the published model in a `List<T>`
- `scenarioContext.Set(captured, ScenarioContextKeys.CapturedOutboundEvents)` in the Background step saves the captured list for `Then` assertions
- The `When` step posts to the processor **debug route** (e.g. `/api/foo/created/process/debug`) when the service exposes one for in-process testing
- `Then` steps assert Moq `Verify` counts and field values on the captured list

### Processor features (file-driven style)

File-driven tests compare the full outbound JSON output against a fixture in `Scenarios/{FlowName}/{ScenarioName}/`.

```gherkin
Feature: Foo Created Processor (File-Driven)
  ...

  Background:
    Given the outbound publisher returns message id "pub-sub-test-id"

  Scenario Outline: <scenarioFolder> - processor output matches the fixture
    Given the scenario is loaded from folder "<scenarioFolder>"
    When the foo created processor processes the file-driven scenario
    Then the published outbound event matches the expected outbound fixture

    Examples:
      | scenarioFolder   |
      | HappyPath-Example |
```

Adding a new file-driven scenario:
1. Create a subfolder under `Scenarios/{FlowName}/<ScenarioName>/`
2. Add `SourceInput.json` (upstream API or enrichment input shape)
3. Add `ExpectedOutbound.json` (expected publish / wire DTO)
4. Add a row to the `Examples` table in the feature file

**Non-deterministic fields** (e.g. generated GUIDs, timestamps) must be added to the `IgnoredJsonProperties` set in the file-driven step definitions class.

### Query endpoint features

GET endpoints live under `Features/{Resource}/` (e.g. `Features/Person/GetPersonFlow.feature`):

| Feature file | Typical pattern |
|---|---|
| `Get{Resource}Flow.feature` | `GET /api/{resources}/{id}` |
| `Get{Resource}By{Key}Flow.feature` | `GET /api/{resources}?{key}=` |

Step definitions: `Get{Resource}FlowStepDefinitions.cs`, etc. Routes are registered in `ApplicationFactory.ConfigureWebHost`.

### Service Bus failure features

`*-ServiceBusFailures.feature` files test processor dead-letter and retry behaviour **without** a real Service Bus. Step definitions invoke the processor's `Run(ServiceBusReceivedMessage, ServiceBusMessageActions, ...)` directly with a mocked `ServiceBusMessageActions` and a synthetic message at a given `DeliveryCount`.

Programmatic happy-path scenarios use debug HTTP routes when available (e.g. `/api/foo/created/process/debug`).

### Step definitions — naming pattern

All step definition classes are in `StepDefinitions/` and shared across features via `[Binding]`. Typical files per concern:

| Pattern | Covers |
|---|---|
| `ReceiverEndpointsStepDefinitions.cs` | Shared HTTP POST/response steps for grouped receivers |
| `{FlowName}ProcessorFlowStepDefinitions.cs` | Programmatic processor steps |
| `{FlowName}ProcessorFlowFilesStepDefinitions.cs` | File-driven processor steps |
| `{FlowName}ProcessorServiceBusFlowStepDefinitions.cs` | Service Bus failure / dead-letter steps |
| `{FlowName}ProcessorUnhappyFlowStepDefinitions.cs` | Error / rejection paths (optional) |
| `Get{Resource}FlowStepDefinitions.cs` | Query endpoint steps |

Reuse existing `[Binding]` steps before adding new classes.

---

## Step 3: Step definitions class structure

```csharp
using System.Text;
using System.Text.Json;
using Reqnroll;
using {ServiceName}.ComponentTests.Support;
// ... other domain usings

namespace {ServiceName}.ComponentTests.StepDefinitions;

[Binding]
public sealed class FooCreatedProcessorFlowStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    // Access the factory from FeatureContext (set in BeforeFeature hook)
    private ApplicationFactory Factory => featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

    [Given(@"...")]
    public void GivenSomething() { ... }

    [When(@"...")]
    public async Task WhenSomething() { ... }

    [Then(@"...")]
    public void ThenSomething() { ... }
}
```

Conventions:
- Primary constructor with `ScenarioContext scenarioContext` and `FeatureContext featureContext`
- Class is `public sealed` with `[Binding]`
- Access `ApplicationFactory` via `featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory)`
- Store intermediate values in `scenarioContext.Set(value, ScenarioContextKeys.SomeKey)`
- Retrieve in later steps with `scenarioContext.Get<T>(ScenarioContextKeys.SomeKey)`
- `[Binding]` steps are globally available — reuse steps from other step-definition classes when possible before writing new ones

---

## Step 4: ApplicationFactory — adding new mocks

Full template: [3_application-factory-snippet.cs](examples/3_application-factory-snippet.cs) (`ApplicationFactory`, `Hooks`, `ScenarioContextKeys`).

The factory extends `WebApplicationFactory<Program>` and:

1. Supplies **in-memory configuration** so the host starts without real Azure resources
2. **Removes** the Functions gRPC worker and Azure SDK singletons (`BlobServiceClient`, `ServiceBusClient`, …)
3. **Replaces** Infra client interfaces with `Mock<>` singletons — blob, Service Bus, downstream HTTP clients, publishers, retry scheduler
4. **Keeps** Api/Infra **mappers** registered from `Program` (real instances, not mocked)
5. Registers function classes as `AddScoped<>` and maps HTTP routes in `ConfigureWebHost` via a shared `Route<THandler>` helper (bypasses Functions middleware)

When a new infrastructure client is introduced:

```csharp
// 1. Declare the mock as a public property on ApplicationFactory
public Mock<INewClient> NewClient { get; } = new();

// 2. In CreateHost → ConfigureServices: remove real registration and inject mock
RemoveAll<INewClient>(services);
services.AddSingleton(NewClient.Object);

// 3. In Hooks.BeforeScenario — reset per scenario
factory.NewClient.Reset();

// 4. In ConfigureWebHost — map new HTTP function route when needed
endpoints.MapPost("/api/foo/created", Route<FooReceiver>((fn, req, ct) => fn.ProcessWebhookAsync(req, ct)));
```

---

## Step 5: ScenarioContextKeys — adding new keys

Add constants to `Support/ScenarioContextKeys.cs` for any new context values:

```csharp
internal const string CapturedOutboundEvents = "CapturedOutboundEvents";
```

---

## Step 6: Hooks and lifecycle

| Hook | Scope | What it does |
|---|---|---|
| `BeforeFeature` | Feature | Creates one `ApplicationFactory` instance; stores in `FeatureContext` |
| `AfterFeature` | Feature | Disposes `ApplicationFactory` |
| `BeforeScenario` | Scenario | Resets **all** mocks on the factory; creates a fresh `HttpClient` |
| `AfterScenario` | Scenario | Disposes the `HttpClient` |

The factory is **shared across scenarios within a feature** but mocks are reset before each scenario.

---

## Step 7: JsonFixtureComparer

`JsonFixtureComparer.AssertEqual(expected, actual, ignoredProperties)` performs a deep JSON comparison:
- Iterates **expected** keys only (actual may have additional properties — they are not flagged as failures)
- Arrays must match length exactly
- Numbers are compared as `decimal` (handles `50.5 == 50.50`)
- Leaf values compared as strings
- Properties listed in `ignoredProperties` (e.g. generated GUIDs, timestamps) are skipped entirely

---

## Step 8: TestDataBuilder (optional)

`TestDataBuilder` (in `Support/`) provides static factory methods for building in-memory domain or upstream models used in programmatic scenarios. Add methods when new shapes are needed — name by scenario intent, not by copying production class names blindly:

```csharp
// Examples — adapt to your domain:
BuildMinimal{Entity}(id)
Build{Entity}With{Variant}(id, ...)
```

Prefer `Fixture` / `FixtureFactory` customizations for simple random data; use `TestDataBuilder` when scenarios need repeatable, named shapes.

---

## Complete examples

### New HTTP receiver (add to ReceiverEndpoints)

When adding a receiver on `/api/foo/created`, add a row to the `Examples` table in `Features/Foo/ReceiverEndpoints.feature`:

```gherkin
      | /api/foo/created | {"id":42,"ref":"99"} | FooCreated | 42 | 42 |
```

Also implement the receiver service, add `EventType` (or equivalent), register in `Program.cs`, and map the route in `ApplicationFactory.ConfigureWebHost`. Standard receivers reuse steps from `ReceiverEndpointsStepDefinitions.cs`.

### New processor file-driven scenario

1. Create `Scenarios/FooCreated/MyNewScenario/SourceInput.json`:
```json
{
  "id": "resource-99999",
  "legacyResourceId": "99999"
}
```

2. Create `Scenarios/FooCreated/MyNewScenario/ExpectedOutbound.json`:
```json
{
  "ResourceId": "99999",
  "OrgId": "...",
  "Lines": [ { } ]
}
```

3. Add to `Features/Foo/FooCreated-Files.feature` Examples table:
```gherkin
    Examples:
      | scenarioFolder    |
      | HappyPath-Example |
      | MyNewScenario     |
```

---

## Checklist before writing component tests

- [ ] Feature file is under `Features/{Resource}/` (or `Features/{Slice}/{Resource}/`)
- [ ] Feature file has **no** `@smoke` / `@integration` tag (component features are untagged)
- [ ] File name and `Feature:` title describe the same Azure Function
- [ ] Processors have separate files for programmatic (`{FlowName}.feature`), file-driven (`{FlowName}-Files.feature`), and Service Bus failure scenarios where applicable
- [ ] `When` steps use the full processor / flow name (e.g. `foo created processor`)
- [ ] Scenario outline titles and descriptions use `-`, not en-dashes
- [ ] Reuse existing steps from other `[Binding]` classes before adding new ones
- [ ] New infrastructure clients are added to `ApplicationFactory.cs` and reset in `Hooks.cs`
- [ ] Mappers stay as **real** registrations — not mocked
- [ ] New context keys are added to `ScenarioContextKeys.cs`
- [ ] File-driven scenarios place fixtures under `Scenarios/{FlowName}/{ScenarioName}/`
- [ ] Non-deterministic fields are added to `IgnoredJsonProperties` in the file-driven step definitions
- [ ] New Azure Function HTTP routes are registered in `ApplicationFactory.ConfigureWebHost`
