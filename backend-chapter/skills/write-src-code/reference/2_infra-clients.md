# Infra Clients

> Reference **2** — Interface, implementation, settings, validator, and DI registration for a new downstream client.

**One client per downstream component.** Each external HTTP API, queue, blob store, or pub/sub target gets its own `I*` interface, `Infra/Clients/{Name}/` folder, settings, validator, and `Add*Client` registration. Do not combine unrelated downstreams into a god-client. See [../../dotnet-best-practices/reference/4_downstream-clients.md](../../dotnet-best-practices/reference/4_downstream-clients.md).

Adding a **new** downstream dependency requires these artifacts:
1. **Interface** — `App/Clients/Interfaces/IFooClient.cs` — **domain types only** in signature
2. **Implementation** — `Infra/Clients/FooClient/FooClient.cs` (`internal sealed`, `[ExcludeFromCodeCoverage]`)
3. **Wire DTOs** — `Infra/Clients/FooClient/Models/` — external API JSON shapes (**Infra only**)
4. **Mapping** — wire DTO → `App.Models` domain in client (or `Mappers/` subfolder)
5. **Settings** — `Infra/Clients/FooClient/Settings/FooSettings.cs` (`[ExcludeFromCodeCoverage]` record)
6. **Validator** — `Infra/Clients/FooClient/Validators/FooSettingsValidator.cs` (FluentValidation, **required** — fail early at startup)

**Infra receives wire DTOs from external APIs and converts to domain models before returning to App.** App must never reference `Infra/.../Models/`. See [../../dotnet-best-practices/reference/2_layer-boundaries.md](../../dotnet-best-practices/reference/2_layer-boundaries.md).

## Interface (App — domain only)

```csharp
// App/Clients/Interfaces/IFooClient.cs
namespace {ServiceName}.App.Clients.Interfaces;

public interface IFooClient
{
    Task<FooOrder?> GetOrderAsync(string id, CancellationToken cancellationToken);
}
```

## Implementation (Infra — map wire DTO → domain)

```csharp
// Infra/Clients/FooClient/FooClient.cs
[ExcludeFromCodeCoverage]
internal sealed class FooClient(HttpClient httpClient) : IFooClient
{
    public async Task<FooOrder?> GetOrderAsync(string id, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"orders/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<FooOrderWireDto>(cancellationToken);
        return wire?.ToDomain();
    }
}
```

## Registration

Every settings record follows [1_configuration-validation.md](1_configuration-validation.md). Registration **must** include `ValidateOnStart()`:

In `Infra/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddOptions<FooSettings>()
    .Bind(config.GetSection(nameof(FooSettings)))
    .ValidateOnStart();
services.AddSingleton<IValidateOptions<FooSettings>>(
    _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));
services.AddHttpClient<IFooClient, FooClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<FooSettings>>().Value;
        client.BaseAddress = opts.BaseUrl;
    });
```

Example implementation: [../examples/7_infra-client.cs](../examples/7_infra-client.cs)
