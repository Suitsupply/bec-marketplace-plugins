---
name: write-component-tests
description: >-
  Generate Reqnroll/Gherkin component tests following Backend Chapter conventions.
  Use when asked to write component tests, feature files, or step definitions.
  Enforces ApplicationFactory mocking, feature file naming, and Scenarios layout.
---

# Write Component Tests

## Purpose

Component tests verify an Azure Function end-to-end in-process, with all infrastructure dependencies (blob storage, Service Bus, sample GraphQL API, outbound publishers, etc.) replaced by `Mock<>` objects. They run with `WebApplicationFactory<Program>` and use Reqnroll (Gherkin) as the test specification language.

See **write-tests** skill for the function inventory and testing pyramid.

## Project location

All component tests live in `test/{ServiceName}.ComponentTests/`.

## Framework

- **Test runner**: NUnit 3 via `Reqnroll.NUnit` 3.3.4
- **Mocking**: Moq 4
- **Host**: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`)
- **Global usings** already active: `Moq`, `NUnit.Framework` (no need to add `using` for these)

---

## Project structure

```
test/{ServiceName}.ComponentTests/
├── Features/
│   ├── GetOrders/
│   │   ├── GetOrderFlow.feature
│   │   └── GetOrderByInternalOrderIdFlow.feature
│   └── Webhooks/
│       ├── Receivers/
│       │   ├── ReceiverEndpoints.feature              # All HTTP receivers (grouped)
│       │   └── OutboundEventsBackup-Receiver.feature       # Service Bus backup receiver
│       └── Processors/
│           ├── OrderCreated/
│           │   ├── OrderCreated.feature               # Programmatic scenarios
│           │   ├── OrderCreated-Files.feature         # File-driven scenarios
│           │   └── OrderCreated-ServiceBusFailures.feature
│           ├── OrderUpdated/
│           │   ├── OrderUpdated.feature
│           │   ├── OrderUpdated-Files.feature
│           │   └── OrderUpdated-ServiceBusFailures.feature
│           └── OrderTransactionCreated/
│               ├── OrderTransactionCreated.feature
│               ├── OrderTransactionCreated-Files.feature
│               ├── OrderTransactionCreated-UnhappyFlow.feature
│               └── OrderTransactionCreated-ServiceBusFailures.feature
├── StepDefinitions/                   # Shared [Binding] classes (not 1:1 with feature files)
│   ├── ReceiverEndpointsStepDefinitions.cs
│   ├── OutboundEventsBackupReceiverFlowStepDefinitions.cs
│   ├── OrderProcessorFlowStepDefinitions.cs           # Order created processor steps
│   ├── OrderProcessorFlowFilesStepDefinitions.cs
│   ├── OrderUpdatedProcessorFlowStepDefinitions.cs
│   ├── OrderTransactionCreatedProcessorFlowStepDefinitions.cs
│   └── ...
├── Support/
│   ├── ApplicationFactory.cs          # WebApplicationFactory with all mocks
│   ├── Hooks.cs                       # BeforeFeature/BeforeScenario lifecycle
│   ├── ScenarioContextKeys.cs         # Constant keys for ScenarioContext/FeatureContext
│   ├── TestDataBuilder.cs         # sample Order model factory helpers
│   └── JsonFixtureComparer.cs          # JSON fixture deep-equality comparer
└── Scenarios/                         # JSON fixture files for file-driven tests
    ├── OrderCreated/
    │   └── <ScenarioName>/
    │       ├── SourceOrder.json
    │       └── ExpectedOutboundCreatedEvent.json
    ├── OrderUpdated/
    │   └── <ScenarioName>/
    │       ├── SourceOrder.json
    │       └── ExpectedOutboundUpdatedEvent.json
    └── OrderTransactionCreated/
        └── <ScenarioName>/
            ├── webhook.json
            ├── graphql.json
            └── expected-mao-payment.json   # (and other optional fixture files)
```

---

## Step 1: Feature file naming convention

Feature files live under `Features/` grouped by concern. The **file name** and **`Feature:` title** must describe the same Azure Function.

| Function type | Path | Feature file(s) | `Feature:` title |
|---|---|---|---|
| HTTP Receivers (grouped) | `Webhooks/Receivers/` | `ReceiverEndpoints.feature` | `Receiver Endpoints` |
| Service Bus Receiver | `Webhooks/Receivers/` | `OutboundEventsBackup-Receiver.feature` | `Outbound Events Backup Receiver` |
| Processor (programmatic) | `Webhooks/Processors/{Name}/` | `{Name}.feature` | `{Name} Processor` |
| Processor (file-driven) | `Webhooks/Processors/{Name}/` | `{Name}-Files.feature` | `{Name} Processor (File-Driven)` |
| Processor (Service Bus failures) | `Webhooks/Processors/{Name}/` | `{Name}-ServiceBusFailures.feature` | `{Name} Processor Service Bus Failures` |
| Processor (unhappy path) | `Webhooks/Processors/{Name}/` | `{Name}-UnhappyFlow.feature` | `{Name} Processor Unhappy Flow` |
| Query endpoints | `GetOrders/` | `GetOrderFlow.feature` | `Get Order Flow` |

**Examples:** `OrderCreated.feature` → `Feature: Order Created Processor`; `OrderTransactionCreated-Files.feature` → `Feature: Order Transaction Created Processor (File-Driven)`.

**Grouping rule**: HTTP receivers that share identical behaviour (accept POST → blob → Service Bus → 202) are covered together in `ReceiverEndpoints.feature` using a `Scenario Outline`.

**One-per-function rule**: processors and Service Bus receivers with distinct retry/dead-letter logic each get their own feature file(s) under the matching processor folder.

**Step definitions file rule**: step definition classes are shared across features. Create a new `{Flow}StepDefinitions.cs` only when new steps are needed — reuse existing `[Binding]` classes first.

**Gherkin punctuation**: use a regular hyphen `-` in scenario outline titles and fixture descriptions, not an en-dash.

### Processor `When` step wording

Use the full processor name in steps so scenarios read unambiguously:

| Processor | Programmatic | File-driven | Service Bus |
|---|---|---|---|
| Order created | `the order created processor receives order {id}` | `the order created processor processes the file-driven scenario order` | `the order created processor receives order {id} at Service Bus delivery count {n}` |
| Order updated | `the order updated processor receives order {id}` | `the order updated processor processes the file-driven scenario order` | `the order updated processor receives order {id} at Service Bus delivery count {n}` |
| Order transaction created | `the order transaction created processor receives transaction {txId} for order {orderId}` | `the order transaction created processor processes the file-driven scenario` | `the order transaction created processor receives transaction {txId} for order {orderId} at Service Bus delivery count {n}` |

---

## Step 2: Feature file structure

### HTTP Receiver features

HTTP receivers share identical behaviour and are tested together in `Features/Webhooks/Receivers/ReceiverEndpoints.feature` (not one feature file per receiver):

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
      | route                            | payload                 | eventType               | messageId | resourceId |
      | /api/orders/created              | {"id":1,"name":"1001"}  | OrderCreated            | 1         | 1              |
      | /api/orders/transactions/created | {"id":1,"order_id":100} | OrderTransactionCreated | 1         | 100            |
```

**Payload shapes by receiver:**

| Receiver | Route | Payload | `resourceId` source |
|---|---|---|---|
| `OrderCreatedReceiver` | `/api/orders/created` | `{"id":1,"name":"#1001"}` | `id` field |
| `OrderUpdatedReceiver` | `/api/orders/updated` | `{"id":1,"name":"#1001"}` | `id` field |
| `OrderTransactionCreatedReceiver` | `/api/orders/transactions/created` | `{"id":1,"order_id":100}` | `order_id` field |
| `RefundsCreatedReceiver` | `/api/refunds/created` | `{"id":1,"order_id":100}` | `order_id` field |

All four use the same set of reusable steps defined in `ReceiverEndpointsStepDefinitions.cs` — no new step definition file is needed for these.

### Processor features (programmatic style)

Programmatic processor scenarios build sample order objects in-memory using `TestDataBuilder` and verify the published outbound payload field by field.

```gherkin
Feature: Order Created Processor
  ...

  Background:
    Given the outbound publisher returns message id "pub-sub-test-id"

  Scenario: RTW order is processed, published and backed up
    Given the sample order 10001 contains a RTW line shipped to US with shipping code "UPS_SAVER"
    And the Shipment Method API returns "UPS_SAVER" for code "UPS_SAVER"
    When the order created processor receives order 10001
    Then the outbound publisher is called once
    And the published outbound order has 1 order line(s)
    And the first outbound order line has shipping method id "UPS_SAVER"
    And Service Bus receives a backup message with message id "pub-sub-test-id"
```

Key conventions:
- One `Background` step sets up the publisher mock and captures the published model in a `List<T>`
- `scenarioContext.Set(captured, ScenarioContextKeys.CapturedOutboundOrders)` in the Background step saves the captured list for `Then` assertions
- The `When` step posts to the `/api/orders/created/process/debug` (or `updated/process/debug`) debug route
- `Then` steps assert Moq `Verify` counts and field values on the captured list

### Processor features (file-driven style)

File-driven tests compare the full outbound JSON output against a fixture in `Scenarios/{Domain}/{ScenarioName}/`.

```gherkin
Feature: Order Created Processor (File-Driven)
  ...

  Background:
    Given the outbound publisher returns message id "pub-sub-test-id"

  Scenario Outline: <scenarioFolder> - sample order is processed and outbound output matches the fixture
    Given the order scenario is loaded from folder "<scenarioFolder>"
    When the order created processor processes the file-driven scenario order
    Then the published outbound order matches the expected outbound fixture

    Examples:
      | scenarioFolder              |
      | ShipToStore-ReadyToWearItem |
```

Adding a new file-driven scenario:
1. Create a subfolder under `Scenarios/OrderCreated/<ScenarioName>/`
2. Add `SourceOrder.json` (GraphQL shape: `{ "data": { "order": { ... } } }`)
3. Add `ExpectedOutboundCreatedEvent.json` (the expected `CreateOrderOutboundModel` as JSON)
4. Add a row to the `Examples` table in the feature file

**Non-deterministic fields** (e.g. `TaxDetailId` which is `Guid.NewGuid()`) must be added to the `IgnoredJsonProperties` set in the step definitions class.

### Order Transaction Created processor

Programmatic and file-driven scenarios live under `Features/Webhooks/Processors/OrderTransactionCreated/`. Use the full name in steps:

```gherkin
When the order transaction created processor receives transaction 50001 for order 30001
```

File-driven scenarios load fixtures from `Scenarios/OrderTransactionCreated/<ScenarioName>/` (see `OrderTransactionCreated-Files.feature` for the full fixture file list). The `When` step is:

```gherkin
When the order transaction created processor processes the file-driven scenario
```

Unhappy-path and Service Bus failure scenarios use `{Name}-UnhappyFlow.feature` and `{Name}-ServiceBusFailures.feature` respectively.

### Query endpoint features

GET endpoints live under `Features/GetOrders/`:

| Feature file | Endpoints tested |
|---|---|
| `GetOrderFlow.feature` | `GET /api/orders/{resourceId}` |
| `GetOrderByInternalOrderIdFlow.feature` | `GET /api/orders?byInternalOrderId=` |

Step definitions: `GetOrderFlowStepDefinitions.cs`, `GetOrderByInternalOrderIdFlowStepDefinitions.cs`. Routes are registered in `ApplicationFactory.ConfigureWebHost`.

### Service Bus failure features

`*-ServiceBusFailures.feature` files test processor dead-letter and retry behaviour **without** a real Service Bus. Step definitions invoke the processor's `Run(ServiceBusReceivedMessage, ServiceBusMessageActions, ...)` directly with a mocked `ServiceBusMessageActions` and a synthetic message at a given `DeliveryCount`. See `OrderProcessorsServiceBusFlowStepDefinitions.cs` and `OrderTransactionCreatedProcessorServiceBusFlowStepDefinitions.cs`.

Programmatic happy-path scenarios use the `_Debug` HTTP routes instead (e.g. `/api/orders/created/process/debug`).

### Step definitions inventory

All step definition classes are in `StepDefinitions/` and shared across features via `[Binding]`:

| File | Covers |
|---|---|
| `ReceiverEndpointsStepDefinitions.cs` | Shared HTTP POST/response steps for receivers |
| `OutboundEventsBackupReceiverFlowStepDefinitions.cs` | outbound backup receiver Service Bus scenarios |
| `OrderProcessorFlowStepDefinitions.cs` | Order created processor programmatic steps |
| `OrderProcessorFlowFilesStepDefinitions.cs` | Order created file-driven steps |
| `OrderProcessorsServiceBusFlowStepDefinitions.cs` | Order created/updated Service Bus failure steps |
| `OrderUpdatedProcessorFlowStepDefinitions.cs` | Order updated programmatic steps |
| `OrderUpdatedProcessorFlowFilesStepDefinitions.cs` | Order updated file-driven steps |
| `OrderTransactionCreatedProcessorFlowStepDefinitions.cs` | Order transaction created programmatic steps |
| `OrderTransactionCreatedProcessorFlowFilesStepDefinitions.cs` | Order transaction created file-driven steps |
| `OrderTransactionCreatedProcessorUnhappyFlowStepDefinitions.cs` | Failed payment / orderCapture error steps |
| `OrderTransactionCreatedProcessorServiceBusFlowStepDefinitions.cs` | Transaction Service Bus failure steps |
| `GetOrderFlowStepDefinitions.cs` | GET order by sample ID |
| `GetOrderByInternalOrderIdFlowStepDefinitions.cs` | GET order by internal outbound name |

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
public sealed class OrderCreatedReceiverFlowStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
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

When a new infrastructure client is introduced, add it to `ApplicationFactory.cs`:

```csharp
// 1. Declare the mock as a public property
public Mock<INewClient> NewClient { get; } = new();

// 2. In CreateHost → ConfigureServices: remove real registration and inject mock
RemoveAll<INewClient>(services);
services.AddSingleton(NewClient.Object);
```

And in `Hooks.cs`, add a reset in `BeforeScenario`:
```csharp
factory.NewClient.Reset();
```

When a new Azure Function is added that uses HTTP triggers, also add its route in `ConfigureWebHost`:
```csharp
endpoints.MapPost("/api/new/route", Route<NewReceiver>((fn, req, ct) => fn.Run(req, ct)));
```

---

## Step 5: ScenarioContextKeys — adding new keys

Add constants to `Support/ScenarioContextKeys.cs` for any new context values:

```csharp
internal const string CapturedNewEvents = "CapturedNewEvents";
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

## Step 8: TestDataBuilder

`TestDataBuilder` (in `Support/`) provides static factory methods for building `Order` objects. Add new builder methods here when new scenarios require order shapes not covered by existing helpers:

```csharp
// Existing helpers:
BuildRtwOrder(orderId, country, shippingCode)
BuildCustomMadeOrder(orderId, country, shippingCode)
BuildOnlineAlterationOrder(orderId, country, shippingCode)
BuildShipToStoreOrder(orderId, storeCode)
BuildOrderWithAdyenPsp(orderId, country, pspReference)
BuildOrderWithoutAdyenPsp(orderId, country)
```

---

## Complete examples

### New HTTP receiver (add to ReceiverEndpoints)

When adding a receiver on `/api/foo/created`, add a row to the `Examples` table in `Features/Webhooks/Receivers/ReceiverEndpoints.feature`:

```gherkin
      | /api/foo/created | {"id":42,"order_id":99} | FooCreated | 42 | 99 |
```

Also implement the receiver service, add `EventType`, register in `Program.cs`, and map the route in `ApplicationFactory.ConfigureWebHost`. Standard receivers reuse steps from `ReceiverEndpointsStepDefinitions.cs`.

### New processor file-driven scenario

1. Create `Scenarios/OrderCreated/MyNewScenario/SourceOrder.json`:
```json
{
  "data": {
    "order": {
      "id": "gid://example/Order/99999",
      "legacyResourceId": "99999",
      ...
    }
  }
}
```

2. Create `Scenarios/OrderCreated/MyNewScenario/ExpectedOutboundCreatedEvent.json`:
```json
{
  "OrderId": "ORDER-99999",
  "OrgId": "...",
  "OrderLine": [ { ... } ]
}
```

3. Add to `Features/Webhooks/Processors/OrderCreated/OrderCreated-Files.feature` Examples table:
```gherkin
    Examples:
      | scenarioFolder              |
      | ShipToStore-ReadyToWearItem |
      | MyNewScenario               |
```

---

## Checklist before writing component tests

- [ ] Feature file is under the correct folder (`Webhooks/Receivers/`, `Webhooks/Processors/{Name}/`, or `GetOrders/`)
- [ ] File name and `Feature:` title describe the same Azure Function
- [ ] Processors have separate files for programmatic (`{Name}.feature`), file-driven (`{Name}-Files.feature`), and Service Bus failure scenarios where applicable
- [ ] `When` steps use the full processor name (`order created processor`, `order updated processor`, `order transaction created processor`)
- [ ] Scenario outline titles and descriptions use `-`, not en-dashes
- [ ] Reuse existing steps from other `[Binding]` classes before adding new ones
- [ ] New infrastructure clients are added to `ApplicationFactory.cs` and reset in `Hooks.cs`
- [ ] New context keys are added to `ScenarioContextKeys.cs`
- [ ] File-driven scenarios place fixtures under `Scenarios/{Domain}/{ScenarioName}/`
- [ ] Non-deterministic fields are added to `IgnoredJsonProperties` in the file-driven step definitions
- [ ] New Azure Function HTTP routes are registered in `ApplicationFactory.ConfigureWebHost`


## Examples

- [examples/feature-file.feature](examples/feature-file.feature)
- [examples/application-factory-snippet.cs](examples/application-factory-snippet.cs)
