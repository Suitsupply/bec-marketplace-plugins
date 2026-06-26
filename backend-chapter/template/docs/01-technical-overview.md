# Template — technical overview

`Template` is a **chapter reference solution** for Backend Chapter skills. It is a runnable Azure Functions app (`net10.0`) with one vertical feature slice — **`Example/`** — that calls the public [SWAPI](https://swapi.info) API.

Use it to learn layer boundaries, test patterns, and project layout before scaffolding a real service (or a future `dotnet new` template).

## Solution layout

```
template/
├── src/
│   ├── Template.Api           # Azure Functions host, HTTP + Service Bus triggers
│   ├── Template.Api.Models    # Transport DTOs (requests/responses)
│   ├── Template.App           # Application services and client interfaces
│   ├── Template.App.Models    # Domain models
│   └── Template.Infra         # HTTP clients, decorators, DI wiring
├── test/
│   ├── Template.UnitTests
│   ├── Template.ComponentTests
│   └── Template.IntegrationTests
├── docs/
└── devops/
```

## Dependency direction

```
Api → App → App.Models
Api → Api.Models
Infra → App (+ App.Models)
```

- **Api.Models** — wire shapes at the HTTP boundary (`GetPersonResponse`, `UpdatePersonRequest`).
- **App.Models** — domain models (`Person`, `UpdatePersonMessage`) without JSON attributes.
- **Api mappers** — DTO ↔ domain at the function boundary.
- **Infra mappers** — external wire ↔ domain at the client boundary.

## Example feature map

| Layer | Folder | Responsibility |
|-------|--------|----------------|
| Api | `Example/Functions` | `PersonFunctions.cs`, `VehicleFunctions.cs` |
| Api | `Example/Mappers/v1` | `PersonMapper`, `VehicleMapper` |
| App | `Example/Services` | `PersonService.cs`, `VehicleService.cs` |
| App | `Example/Clients/Interfaces` | `ISwapiClient` |
| Infra | `Example/Clients/Swapi` | `SwapiClient`, `SwapiPersonMapper`, settings |
| Infra | `Example/Decorators` | `PersonServiceCachingDecorator`, `VehicleServiceCachingDecorator` (Scrutor) |

## Test pyramid

| Project | Scope | External deps |
|---------|-------|---------------|
| UnitTests | Services, mappers, functions | None — mock `ISwapiClient` / inner services |
| ComponentTests | HTTP flows in memory | None — `ApplicationFactory` mocks `ISwapiClient` |
| IntegrationTests | Deployed host (`@smoke`, `@integration`) | TST/PRD URL + secrets via runsettings |

**Unit test rules:** mirror `src/` under `test/…UnitTests/Example/`; use real mapper instances; never `Mock<I*Mapper>`.

## Skills index

Chapter skills live under `backend-chapter/skills/`:

| Topic | Skill |
|-------|-------|
| Project structure | `dotnet-best-practices` |
| Unit tests | `write-unit-tests` |
| Component tests | `write-component-tests` |
| Integration tests | `write-integration-tests` |

See [02-example-feature.md](./02-example-feature.md) for flow-level detail.
