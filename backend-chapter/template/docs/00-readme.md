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
# GET http://localhost:7071/api/people/1?code=<function-key>
# POST http://localhost:7071/api/people/requested/process/debug  body: {"id":1}
```

Copy `local.settings.json.example` and set `ServiceSettings__ServiceName`. SWAPI needs no API key.

## Further reading

- [Technical overview](./01-technical-overview.md) — layers, projects, test pyramid
- [Example feature](./02-example-feature.md) — query + processor flows, sequence diagram
