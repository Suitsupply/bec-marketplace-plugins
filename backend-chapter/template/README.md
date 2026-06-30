# Template

A **Backend Chapter reference solution** — a small, runnable Azure Functions app that teaches layer boundaries, decorators, and the test pyramid through one vertical `Example/` feature slice (SWAPI person lookup).

> This is a teaching project, not production code. It will become a `dotnet new` template later.

## Quick start

**Prerequisites:** .NET 10 SDK, Azure Functions Core Tools (optional, for local host).

```bash
cd backend-chapter/template
cp src/Template.Api/local.settings.json.example src/Template.Api/local.settings.json
dotnet restore --configfile nuget.config
dotnet build
dotnet test test/Template.UnitTests
dotnet test test/Template.ComponentTests
```

**Run locally (live SWAPI):**

```bash
dotnet run --project src/Template.Api
# or: cd src/Template.Api && func start
```

Copy `local.settings.json.example` to `local.settings.json` first (sets `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`).

```bash
# GET http://localhost:7071/api/people/1?code=<function-key>
# GET http://localhost:7071/api/vehicles/4?code=<function-key>
# POST http://localhost:7071/api/vehicles?code=<function-key>  body: {"name":"Sand Crawler","model":"Digger Crawler","manufacturer":"Corellia Mining Corporation"}
# POST http://localhost:7071/api/people/requested/process/debug  body: {"id":1}
```

Copy `local.settings.json.example` and set `ServiceSettings__ServiceName`. SWAPI needs no API key.

## Copy a resource (e.g. Vehicle)

1. Add `VehicleFunctions.cs` in `Example/Functions/` and `VehicleServices.cs` in `Example/Services/`, plus models, mappers, and tests.
2. Wire DI in `ServiceCollectionExtensions` and `Program.cs`.
3. Add unit tests mirroring `src/` and component tests under `Features/Example/Vehicle/`.

See [docs/02-example-feature.md](./docs/02-example-feature.md).

## Skills map

| What you see | Chapter skill |
|--------------|---------------|
| Five-project layout, `docs/`, `devops/` | [dotnet-best-practices](../skills/dotnet-best-practices/SKILL.md) |
| Unit test structure, AutoData, real mappers | [write-unit-tests](../skills/write-unit-tests/SKILL.md) |
| Reqnroll + `ApplicationFactory` | [write-component-tests](../skills/write-component-tests/SKILL.md) |
| `@smoke` / `@integration`, runsettings | [write-integration-tests](../skills/write-integration-tests/SKILL.md) |

## Further reading

- [Technical overview](./docs/01-technical-overview.md) — layers, projects, test pyramid
- [Example feature](./docs/02-example-feature.md) — query + processor flows, sequence diagram

## Audabit vs chapter template

Same **vertical slice** and **decorator** idea as Audabit’s Star Wars example. Differences in this template:

- Azure Functions **Api** host project (not Web App)
- Logging decorator uses **`ILogger` + Scrutor** (not `IEmitter`)
- **NUnit** + Reqnroll component tests
- **`docs/`** and **`devops/`** at repo root per chapter standard
- **No HTTP receiver** in v1 — processor only (debug route for local/component tests)

## CI/CD

Build and test pipeline: [devops/azurepipelines/azure-pipeline.yaml](./devops/azurepipelines/azure-pipeline.yaml) (unit + component; deploy stage stubbed).

Infrastructure skeleton: [devops/bicep/azuredeploy.bicep](./devops/bicep/azuredeploy.bicep).
