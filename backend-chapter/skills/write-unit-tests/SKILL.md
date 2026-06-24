---
name: write-unit-tests
description: >-
  Generate NUnit unit tests following Backend Chapter conventions. Use when
  asked to write, generate, add, or create unit tests, test files, or test
  classes. Enforces folder mirroring, base/derived class pattern, AutoFixture,
  Moq, ArgumentsNullChecker, and Arrange/Act/Assert structure.
---

# Write Unit Tests

## Project conventions

- **Framework**: NUnit 3, Moq, AutoFixture (with AutoMoq and NUnit3 extensions)
- **Global usings** (no need to import): `AutoFixture`, `AutoFixture.AutoMoq`, `AutoFixture.NUnit3`, `Moq`, `NUnit.Framework`
- **Assembly**: Each test class runs with a fresh instance per test (`FixtureLifeCycle.InstancePerTestCase` in `AssemblyInfo.cs`)

See also **write-tests** skill for the testing pyramid and when to add unit vs component vs integration tests.

## FixtureFactory

Always use `FixtureFactory.Create()` in base classes — never `new Fixture()`. `FixtureFactory` applies domain customizations (e.g. `PresentmentMoney` decimal formatting). Shared builders live in `Helpers/FixtureExtensions.cs` (e.g. `CreateMoneySet`, `CreateOrderWithAdyenPsp`).

## Step 1: Determine file location

Mirror the `src` path inside `test/{ServiceName}.UnitTests/`:

| Source path | Test path |
|---|---|
| `src/{ServiceName}.Api/Functions/Receivers/FooReceiver.cs` | `test/{ServiceName}.UnitTests/Api/Functions/Receivers/FooReceiverTests.cs` |
| `src/{ServiceName}.App/Services/Processors/FooService.cs` | `test/{ServiceName}.UnitTests/App/Services/Processors/FooServiceTests.cs` |
| `src/{ServiceName}.App/Services/Processors/TransactionCreatedFlows/FooFlowHandler.cs` | `test/{ServiceName}.UnitTests/App/Services/Processors/Flows/FooFlowHandlerTests.cs` |
| `src/{ServiceName}.Api/Mappers/GetOrderMapper.cs` | `test/{ServiceName}.UnitTests/Api/Mappers/GetOrderMapperTests.cs` |
| `src/{ServiceName}.Infra/Clients/FooClient/FooClient.cs` | `test/{ServiceName}.UnitTests/Infra/Clients/FooClient/FooClientTests.cs` |

The **namespace** matches the folder: `{ServiceName}.UnitTests.Api.Functions.Receivers`.

## Step 2: Class structure

Every test file uses this exact layout:

```csharp
// Only add `using` directives that are NOT already globally available
using {ServiceName}.Api.Functions.Receivers; // src namespace

namespace {ServiceName}.UnitTests.Api.Functions.Receivers; // test namespace

public static class FooReceiverTests
{
    public abstract class FooReceiverTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly Mock<IFooDependency> Dependency;
        protected readonly FooReceiver Sut;

        protected FooReceiverTestsBase()
        {
            Dependency = new Mock<IFooDependency>();
            Sut = new FooReceiver(Dependency.Object);
        }
    }

    // Always include this class
    public class NullArgumentChecks : FooReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    // One class per public endpoint/method
    public class Run : FooReceiverTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturn_WhenCondition(string param)
        {
            // Arrange
            ...
            // Act
            var result = await Sut.Run(param, CancellationToken.None);
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
- Always declare `protected readonly Fixture Fixture = FixtureFactory.Create();` — never use `new Fixture()` or `new()` directly, because `FixtureFactory` applies domain-specific customizations (e.g. `PresentmentMoney` decimal formatting)
- Initialize everything in the constructor
- Add `protected static`/`protected` helper factory methods when multiple tests share complex object construction

### `NullArgumentChecks` (always present)
- Call `ArgumentsNullChecker.CheckMethodParameters(Sut)` for instance methods
- Call `ArgumentsNullChecker.CheckConstructorParameters<FooClass>()` when constructor null checks also need testing
- Call `ArgumentsNullChecker.CheckConstructorAndMethodsParameters(Sut)` to test both at once
- Import: `using {ServiceName}.UnitTests.Helpers;`

### One derived class per public method
- Name the class after the method: `public class Run : FooReceiverTestsBase`
- Each test method must have `// Arrange`, `// Act`, `// Assert` comments

### AutoData usage
- **Prefer `[Test, AutoData]`** when all test parameters are simple types (string, int, Guid, etc.) that AutoFixture can generate. Declare them as method parameters.
- **Use `Fixture.Create<T>()`** in the method body when you need the value before calling Act, or when the type requires configuration (e.g., complex models with constrained properties).
- **Use `Fixture.Build<T>().With(x => x.Prop, value).Create()`** when only specific properties need a controlled value — start from a fully random object and override only what matters for the test.
- **Never manually construct domain objects with hardcoded values** (e.g. `new Order(Id: "123", Name: "#SHA1001", ...)`) — always use `Fixture.Create<T>()` or `Fixture.Build<T>().With(...).Create()`. This applies to both "happy path" and "null/edge case" tests: for the latter, use `.With(x => x.Prop, (Type?)null)` to null out only the fields relevant to the scenario.
- Never use `new Fixture()` inside a test method — always use `Fixture` from the base class.

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
| **App/Enrichment/Steps** | Single-step behaviour with mocked clients |
| **App/Mappers** | Shape translation only — given enriched envelope/domain model; no business rules |
| **App/Extensions** | Pure extension methods on domain models (`{Type}Extensions`) |
| **Infra/Clients** | HTTP client behaviour (mock `HttpMessageHandler` or use test server) |
| **Infra/Validators** | FluentValidation rules for settings records |

## Full example: Receiver

```csharp
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using {ServiceName}.Api.Functions.Receivers;
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

        protected OrderCreatedReceiverTestsBase()
        {
            Logger = new Mock<ILogger<OrderCreatedReceiver>>();
            Service = new Mock<IOrderCreatedReceiverService>();
            Receiver = new OrderCreatedReceiver(Logger.Object, Service.Object);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }
    }

    public class NullArgumentChecks : OrderCreatedReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Receiver);
        }
    }

    public class Run : OrderCreatedReceiverTestsBase
    {
        [Test]
        public async Task ShouldReturnAcceptedResult_WhenServiceProcessesSuccessfully()
        {
            // Arrange
            var rawJson = Fixture.Create<string>();
            var request = CreateRequest(rawJson);

            // Act
            var result = await Receiver.Run(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
            Service.Verify(s => s.ProcessAsync(rawJson, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShouldReturn500_WhenServiceThrows()
        {
            // Arrange
            var rawJson = Fixture.Create<string>();
            var errorMessage = Fixture.Create<string>();
            var request = CreateRequest(rawJson);
            Service
                .Setup(s => s.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await Receiver.Run(request, CancellationToken.None);

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
- [ ] Domain objects are created with `Fixture.Create<T>()` or `Fixture.Build<T>().With(...).Create()` — no hardcoded manual construction
- [ ] `[Test, AutoData]` used when parameters are simple types; otherwise `Fixture.Create<T>()`
- [ ] All tests have `// Arrange`, `// Act`, `// Assert`
- [ ] Test names follow `Should{Outcome}_When{Condition}`
- [ ] No `using` for globally available namespaces (AutoFixture, Moq, NUnit.Framework)


## Examples

- [examples/test-class-layout.cs](examples/test-class-layout.cs)
- [examples/null-argument-checks.cs](examples/null-argument-checks.cs)
