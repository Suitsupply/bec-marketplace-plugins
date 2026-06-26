# Program registration and host bootstrap

> Reference **18** — `Program.cs` order, DI lifetimes, Web App vs Functions.

**Chapter rules:** [§6 Dependency injection](../SKILL.md#6-dependency-injection), [Chapter common packages](../SKILL.md#chapter-common-packages). This document is the **bootstrap checklist** when adding or extending an Api host.

---

## DI lifetimes (summary)

| Lifetime | What goes here |
|----------|----------------|
| `AddSingleton` | Azure SDK clients (`BlobServiceClient`, `ServiceBusClient`), Infra typed wrappers, outbound publishers |
| `AddScoped` | App domain services (typical default) |
| `AddTransient` | Enrichment steps, validators, lightweight stateless helpers |

> Boundary mappers are `static class`es — **not registered**. Call them directly. Only register a mapper (`AddTransient`) when it needs injected collaborators — see [17_models-and-mappers.md](17_models-and-mappers.md).

Register App services in `Program.ConfigureServices`; Infra wiring in `AddInfrastructure`.

---

## `Program.cs` registration order

```csharp
// 0. Service info (required on every Api host)
services.AddServiceInfo(config.GetSection(nameof(ServiceSettings)));
```

Then App-layer services (lifetime from table above), then `AddInfrastructure(config)`.

Configure `ServiceSettings:ServiceName` before local runs:

| Host | Location |
|------|----------|
| Azure Functions | `local.settings.json` → `ServiceSettings__ServiceName` |
| ASP.NET Web App | `appsettings.json` → `ServiceSettings:ServiceName` |

**NuGet (Api project):**

| Host | Package |
|------|---------|
| Azure Functions | `Suitsupply.Common.ServiceInfo.Functions` |
| ASP.NET Web App | `Suitsupply.Common.ServiceInfo.AspNet` |

Full ServiceInfo shape and validator: [Chapter common packages](../SKILL.md#chapter-common-packages), [12_configuration-validation.md](12_configuration-validation.md).

Integration-specific registration examples: [14_integration-service-patterns.md](14_integration-service-patterns.md).

---

## Web App host

ASP.NET Web App services use Controllers or minimal APIs in `{ServiceName}.Api` but share the same App, Infra, and App.Models layers. Register `Suitsupply.Common.ServiceInfo.AspNet` and map controllers; boundary mappers live in `Api/Mappers/`.

**Vertical feature slices** suit services with multiple unrelated tools (`Api/{Feature}/Controllers/` mirrored in App and Infra). See [1_src-folder-structure.md](1_src-folder-structure.md).

Azure Functions patterns apply **only when the service uses that flow** — [15_azure-functions.md](15_azure-functions.md).

---

## Implementation notes (templates only)

Practical details when applying chapter templates:

- **Primary constructors** everywhere; **exception:** types extending framework bases (e.g. `DelegatingHandler`) may use a classic constructor when `ArgumentNullException.ThrowIfNull` guards are needed on injected parameters.
- **No redundant `using`** for namespaces already in `<ImplicitUsings>` or `<Using>` in the `.csproj`.
- **Framework immutability exemptions** — acceptable mutation when the .NET API requires it:
  - `IMemoryCache.GetOrCreateAsync` — configure `ICacheEntry` expiry inside the factory.
  - `DelegatingHandler` — mutate `HttpRequestMessage.Headers` for auth.
  - `.ConfigureHttpClient((sp, client) => { client.BaseAddress = … })` — `HttpClient` is passed for configuration.

```csharp
// ✗ wrong — mutates a parameter
void Enrich(MyModel model) { model.Field = "value"; }

// ✓ correct — return a new object
MyModel Enrich(MyModel model) => model with { Field = "value" };
```

Parameter immutability, blank lines before `return`, LINQ layout, signature length: hub §8, [8_style-and-performance.md](../examples/8_style-and-performance.md).
