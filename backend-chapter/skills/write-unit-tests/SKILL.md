---
name: write-unit-tests
description: >-
  Generate NUnit unit tests following Backend Chapter conventions. Use when
  asked to write, generate, add, or create unit tests, test files, or test
  classes. Enforces folder mirroring, base/derived class pattern, AutoFixture,
  Moq, ArgumentsNullChecker, real mappers (not mocked), and Arrange/Act/Assert structure.
---

# Write Unit Tests

See **write-tests** for the testing pyramid and when to add unit vs component vs integration tests.

**Coverage goal:** unit tests should cover close to **100%** of testable production code and branching logic (services, validators, mappers, enrichment steps, flow handlers). **Exclude** framework wiring, DI registration, and Infra client implementations — those are proven by component and integration tests.

## Examples

| # | File | Topic |
|---|------|-------|
| 1 | [1_test-class-layout.cs](examples/1_test-class-layout.cs) | Base/derived test class layout |
| 2 | [2_null-argument-checks.cs](examples/2_null-argument-checks.cs) | `ArgumentsNullChecker` usage |

---

## Project conventions

- **Framework**: NUnit 3, Moq, AutoFixture (with AutoMoq and NUnit3 extensions)
- **Global usings** (no need to import): `AutoFixture`, `AutoFixture.AutoMoq`, `AutoFixture.NUnit3`, `Moq`, `NUnit.Framework`
- **Assembly**: Each test class runs with a fresh instance per test (`FixtureLifeCycle.InstancePerTestCase` in `AssemblyInfo.cs`)

## FixtureFactory and customizations

Always use `FixtureFactory.Create()` in base classes — never `new Fixture()`.

**Domain shaping belongs in AutoFixture customizations** registered in `FixtureFactory` (e.g. `ICustomization`, `ISpecimenBuilder`, or `fixture.Customize<T>()`). Examples: decimal formatting on `PresentmentMoney`, sensible defaults for nested records.

**Never use shared builder helpers** — no `Helpers/FixtureExtensions.cs`, no `CreateMoneySet()`, `CreateOrderWithAdyenPsp()`, or similar `Create*` methods on the test project. Those hide setup in shared helpers instead of customizations.

**In tests**, use only:

- `Fixture.Create<T>()`
- `Fixture.Build<T>().With(x => x.Prop, value).Create()` to override specific properties for the scenario

```csharp
// test/{ServiceName}.UnitTests/FixtureFactory.cs — register customizations once
public static class FixtureFactory
{
    public static Fixture Create()
    {
        var fixture = new Fixture();
        fixture.Customize(new PresentmentMoneyCustomization());
        fixture.Customize(new OrderCustomization());
        return fixture;
    }
}
```

## Step 1: Determine file location

**Mirror `src/` exactly** inside `test/{ServiceName}.UnitTests/` — same layer folders (`Api/`, `App/`, `Infra/`), same relative path; test file name is `{Class}Tests.cs`. Namespace: `{ServiceName}.UnitTests.{mirrored path}`.

Component and integration tests use a **feature/flow layout** instead — do not mirror `src/` there.

| Source path | Test path |
|---|---|
| `src/{ServiceName}.Api/Functions/Receivers/FooReceiver.cs` | `test/{ServiceName}.UnitTests/Api/Functions/Receivers/FooReceiverTests.cs` |
| `src/{ServiceName}.App/Services/Processors/FooService.cs` | `test/{ServiceName}.UnitTests/App/Services/Processors/FooServiceTests.cs` |
| `src/{ServiceName}.App/Services/Processors/TransactionCreatedFlows/FooFlowHandler.cs` | `test/{ServiceName}.UnitTests/App/Services/Processors/Flows/FooFlowHandlerTests.cs` |
| `src/{ServiceName}.Api/Mappers/GetOrderMapper.cs` | `test/{ServiceName}.UnitTests/Api/Mappers/GetOrderMapperTests.cs` |
| `src/{ServiceName}.Infra/Clients/FooClient/Mappers/FooOrderMapper.cs` | `test/{ServiceName}.UnitTests/Infra/Clients/FooClient/Mappers/FooOrderMapperTests.cs` |

**No mirrored unit test** for `Infra/Clients/{Name}/{Name}Client.cs` (client implementations), `Program.cs`, or `ServiceCollectionExtensions.cs` — see [What **not** to unit-test](#what-not-to-unit-test).

The **namespace** matches the folder: `{ServiceName}.UnitTests.Api.Functions.Receivers`.

## Step 2: Class structure

Every test file uses this exact layout:

```csharp
// Only add `using` directives that are NOT already globally available
using {ServiceName}.Api.Functions.Receivers; // src namespace
using {ServiceName}.Api.Mappers.v1;

namespace {ServiceName}.UnitTests.Api.Functions.Receivers; // test namespace

public static class FooReceiverTests
{
    public abstract class FooReceiverTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly Mock<IFooDependency> Dependency;
        protected readonly FooWebhookMapper WebhookMapper = new();
        protected readonly FooReceiver Sut;

        protected FooReceiverTestsBase()
        {
            Dependency = new Mock<IFooDependency>();
            Sut = new FooReceiver(Dependency.Object, WebhookMapper);
        }
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : FooReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    // One derived class per public method — name the class after the method ({MethodName})
    public class ProcessWebhookAsync : FooReceiverTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturn_WhenCondition(string param)
        {
            // Arrange
            ...

            // Act
            var result = await Sut.ProcessWebhookAsync(param, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
        }
    }
}
```

## Step 3: Rules for each section

### Base class (`{ClassName}TestsBase`)
- Declare all mocks as `protected readonly Mock<IFoo> Foo;`
- Declare SUT as `protected readonly FooClass Sut;`
- **Use real mapper instances** (`protected readonly FooWebhookMapper WebhookMapper = new();`) — never `Mock<I*Mapper>`; mappers are stateless and have their own dedicated test files
- Always declare `protected readonly Fixture Fixture = FixtureFactory.Create();` — never use `new Fixture()` or `new()` directly; domain rules live in `FixtureFactory` customizations
- Initialize everything in the constructor
- Do **not** add shared `Create*` builder helpers on test classes — use `Fixture.Build<T>().With(...)` in the test when a scenario needs specific values

### `NullArgumentChecks` (always present)

One derived class per test file. **`ArgumentsNullChecker`** asserts every public method (and constructor, when configured) has null/whitespace guards on its parameters.
- Call `ArgumentsNullChecker.CheckMethodParameters(Sut)` for instance methods
- Call `ArgumentsNullChecker.CheckConstructorParameters<FooClass>()` when constructor null checks also need testing
- Call `ArgumentsNullChecker.CheckConstructorAndMethodsParameters(Sut)` to test both at once
- Import: `using {ServiceName}.UnitTests.Helpers;`

### One derived class per public method
- Name the derived class after the public method under test: `public class {MethodName} : FooReceiverTestsBase` (e.g. `ProcessWebhookAsync`, `GetOrderAsync` — not a vague name like `Run`)
- Each test method must have `// Arrange`, `// Act`, `// Assert` comments, each preceded by a blank line (except `// Arrange` at the start of the method)

### AutoData usage
- **Prefer `[Test, AutoData]`** when all test parameters are simple types (string, int, Guid, etc.) that AutoFixture can generate. Declare them as method parameters.
- **Use `Fixture.Create<T>()`** in the method body when you need the value before calling Act, or when the type requires configuration (e.g., complex models with constrained properties).
- **Use `Fixture.Build<T>().With(x => x.Prop, value).Create()`** when only specific properties need a controlled value — start from a fully random object and override only what matters for the test.
- **Never manually construct domain objects with hardcoded values** (e.g. `new Order(Id: "123", Name: "#SHA1001", ...)`) — always use `Fixture.Create<T>()` or `Fixture.Build<T>().With(...).Create()`. This applies to both "happy path" and "null/edge case" tests: for the latter, use `.With(x => x.Prop, (Type?)null)` to null out only the fields relevant to the scenario.
- Never use `new Fixture()` inside a test method — always use `Fixture` from the base class.

### Mappers — real instances, not mocks

**Api** and **Infra** mappers are pure, stateless shape translation — instantiate the concrete class (`new FooWebhookMapper()`), never `Mock<I*Mapper>`.

| Context | Pattern |
|---|---|
| **Mapper unit tests** | Mapper is the SUT: `protected readonly FooWebhookMapper Sut = new();` |
| **Function / service tests** | Inject the real mapper into the SUT; mock services and client interfaces only |
| **Edge cases** | Test mapping branches in `{Mapper}Tests.cs`, not by stubbing `ToDomain` / `Map` in another test |

When a function test verifies a service call, assert against the **mapped domain object** (call the same real mapper in Arrange for the expected value), not a value returned from a mocked mapper.

### Test naming
Follow the pattern: `Should{Outcome}_When{Condition}`

Examples:
- `ShouldReturnAcceptedResult_WhenServiceProcessesSuccessfully`
- `ShouldReturn500_WhenServiceThrows`
- `ShouldThrowInvalidOperationException_WhenOrderNotFound`
- `ShouldDeadLetterMessage_WhenDeliveryCountExceedsMax`

### What to unit-test by layer

| Layer | Typical test targets |
|---|---|
| **Api/Functions** | HTTP status codes, exception mapping, argument validation |
| **Api/Mappers** | Domain → API response DTO mapping |
| **App/Services** | Business orchestration with mocked dependencies |
| **App/Services/Processors/Flows** | Individual transaction flow handlers (Klarna, refund, etc.) |
| **App/Enrichment/Steps** | Single-step behaviour with mocked `I*` client interfaces |
| **Infra/Clients/…/Mappers** | Shape translation only — given domain or enriched context; no business rules |
| **App/Extensions** | Pure extension methods on domain models (`{Type}Extensions`) |
| **Infra/Validators** | FluentValidation rules for settings records |

### What **not** to unit-test

| Exclude | Why | Covered by |
|---|---|---|
| Framework wiring (`Program.cs`, `ServiceCollectionExtensions`) | No business logic; `[ExcludeFromCodeCoverage]` | Component tests (in-process host) |
| DI registration | Boilerplate binding | Component tests |
| **Infra client implementations** (`FooClient`, publishers, blob/queue wrappers) | Thin HTTP/SDK adapters; `[ExcludeFromCodeCoverage]` | Component tests (mocked) and integration tests (live) |

Unit-test **against mocked `I*` service and client interfaces** in App services — not the Infra client class itself, and **not mappers** (use real mapper instances).

## Full example: Receiver

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using {ServiceName}.Api.Functions.Receivers;
using {ServiceName}.Api.Mappers.v1;
using {ServiceName}.Api.Models.Foo.Transport.Requests;
using {ServiceName}.App.Models;
using {ServiceName}.App.Services.Receivers.Interfaces;
using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.Api.Functions.Receivers;

public static class OrderCreatedReceiverTests
{
    public abstract class OrderCreatedReceiverTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly OrderCreatedReceiver Receiver;
        protected readonly Mock<ILogger<OrderCreatedReceiver>> Logger;
        protected readonly Mock<IOrderCreatedReceiverService> Service;
        protected readonly FooWebhookMapper WebhookMapper = new();

        protected OrderCreatedReceiverTestsBase()
        {
            Logger = new Mock<ILogger<OrderCreatedReceiver>>();
            Service = new Mock<IOrderCreatedReceiverService>();
            Receiver = new OrderCreatedReceiver(Logger.Object, Service.Object, WebhookMapper);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : OrderCreatedReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Receiver);
        }
    }

    public class ProcessWebhookAsync : OrderCreatedReceiverTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnAcceptedResult_WhenServiceProcessesSuccessfully(FooCreatedRequest requestDto)
        {
            // Arrange
            var rawJson = JsonSerializer.Serialize(requestDto);
            var request = CreateRequest(rawJson);
            var expectedDomain = WebhookMapper.ToDomain(requestDto);

            // Act
            var result = await Receiver.ProcessWebhookAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
            Service.Verify(s => s.ProcessAsync(expectedDomain, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldReturn500_WhenServiceThrows(FooCreatedRequest requestDto, string errorMessage)
        {
            // Arrange
            var rawJson = JsonSerializer.Serialize(requestDto);
            var request = CreateRequest(rawJson);
            Service
                .Setup(s => s.ProcessAsync(It.IsAny<FooCreatedWebhook>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await Receiver.ProcessWebhookAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }
    }
}
```

## Full example: Step / Service (with AutoData)

```csharp
using {ServiceName}.App.Enrichment.Steps;
using {ServiceName}.App.Clients.Interfaces;

namespace {ServiceName}.UnitTests.App.Enrichment.Steps;

public static class FetchOrderStepTests
{
    public abstract class FetchOrderStepTestsBase
    {
        protected readonly Mock<IExternalGraphQLClient> GraphQLClient;
        protected readonly FetchOrderStep Step;

        protected FetchOrderStepTestsBase()
        {
            GraphQLClient = new Mock<IExternalGraphQLClient>();
            Step = new FetchOrderStep(GraphQLClient.Object);
        }
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : FetchOrderStepTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Step);
        }
    }

    public class ExecuteAsync : FetchOrderStepTestsBase
    {
        [Test]
        [AutoData]
        public async Task ShouldReturnOrder_WhenOrderExists(Order order, string orderGid)
        {
            // Arrange
            GraphQLClient
                .Setup(c => c.GetOrderAsync(orderGid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var result = await Step.ExecuteAsync(orderGid, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(order));
        }

        [Test]
        [AutoData]
        public async Task ShouldThrowInvalidOperationException_WhenOrderNotFound(string orderGid)
        {
            // Arrange
            GraphQLClient
                .Setup(c => c.GetOrderAsync(orderGid, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            // Act
            var act = () => Step.ExecuteAsync(orderGid, CancellationToken.None);

            // Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => act());
            Assert.That(exception!.Message, Does.Contain(orderGid));
        }
    }
}
```

## Checklist before writing tests

- [ ] **`dotnet format`** run on the solution; **Code Cleanup** in Visual Studio if applicable
- [ ] File is placed in the mirrored folder under `test/{ServiceName}.UnitTests/`
- [ ] Namespace mirrors folder path using `{ServiceName}.UnitTests.*`
- [ ] Outer class is `public static class {Name}Tests`
- [ ] One `abstract` base class: `{Name}TestsBase`
- [ ] `NullArgumentChecks` class always present, using `ArgumentsNullChecker`
- [ ] One derived class per public method/endpoint
- [ ] `Fixture` is declared as `FixtureFactory.Create()`, never `new Fixture()` or `new()`
- [ ] Domain rules are in `FixtureFactory` **customizations** — no `FixtureExtensions` / shared `Create*` builders
- [ ] Domain objects are created with `Fixture.Create<T>()` or `Fixture.Build<T>().With(...).Create()` — no hardcoded manual construction
- [ ] `[Test, AutoData]` used when parameters are simple types; otherwise `Fixture.Create<T>()`
- [ ] **Mappers** use real instances (`new FooWebhookMapper()`), never `Mock<I*Mapper>`
- [ ] All tests have `// Arrange`, `// Act`, `// Assert` with a blank line before each (except `// Arrange` at method start)
- [ ] Test names follow `Should{Outcome}_When{Condition}`
- [ ] No `using` for globally available namespaces (AutoFixture, Moq, NUnit.Framework)
