# Principles and patterns reference

Illustrative `.cs` files for chapter coding standards. Each file is **documentation** — not a compilable project. Examples use `{ServiceName}` placeholders and cite real patterns from chapter repos (`ReceiverServiceBase`, `TransactionFlowHandlerFactory`, `BulkReplayServiceLoggingDecorator`, etc.).

## Principles

| File | Principle | Chapter application |
|------|-----------|---------------------|
| [principles/1_SOLID.cs](principles/1_SOLID.cs) | SOLID | DIP via App/Infra; SRP in services; OCP via handlers |
| [principles/2_DRY.cs](principles/2_DRY.cs) | Don't Repeat Yourself | Base classes, factories, shared mappers |
| [principles/3_KISS.cs](principles/3_KISS.cs) | Keep It Simple | Clear flow over clever abstractions |
| [principles/4_YAGNI.cs](principles/4_YAGNI.cs) | You Aren't Gonna Need It | Extract on real duplication, not speculation |
| [principles/5_SeparationOfConcerns.cs](principles/5_SeparationOfConcerns.cs) | Separation of concerns | Api / App / Infra layers |
| [principles/6_Encapsulation.cs](principles/6_Encapsulation.cs) | Encapsulation | `internal` clients, interfaces in App |
| [principles/7_CompositionOverInheritance.cs](principles/7_CompositionOverInheritance.cs) | Composition over inheritance | Inject dependencies; shallow inheritance hierarchies |

## Patterns

| File | Pattern | Chapter application |
|------|---------|---------------------|
| [patterns/factory-pattern.cs](patterns/factory-pattern.cs) | Factory | `ITransactionFlowHandlerFactory`, `IStarWarsClientFactory` |
| [patterns/strategy-pattern.cs](patterns/strategy-pattern.cs) | Strategy | `ITransactionFlowHandler` per payment scenario |
| [patterns/template-method-pattern.cs](patterns/template-method-pattern.cs) | Template method | `ReceiverServiceBase<T>` |
| [patterns/decorator-pattern.cs](patterns/decorator-pattern.cs) | Decorator | `StarWarsServiceLoggingDecorator`, `BulkReplayServiceLoggingDecorator`, Scrutor `Decorate<>` |
| [layer-boundaries.md](layer-boundaries.md) | DTO vs domain | Api and Infra convert at edges; App uses `App.Models` only |
| [layer-boundaries.cs](layer-boundaries.cs) | DTO vs domain (examples) | Api `Mappers/`, Infra wire `Models/` → domain |
| [observability-logging.md](observability-logging.md) | Logging | Entry log with ids; log all errors; sparse info |
| [extensions-vs-helpers.md](extensions-vs-helpers.md) | Extensions | `{Type}Extensions` for model logic — no `*Helper` classes |
| [interfaces.md](interfaces.md) | Interfaces | Every `I*` in `Interfaces/` subfolder |
| [downstream-clients.md](downstream-clients.md) | Clients | One client per downstream component |
| [named-private-methods.md](named-private-methods.md) | Readability | Extract distinct actions into named private methods |

Hub skill: **dotnet-best-practices** — Design principles section links here.
