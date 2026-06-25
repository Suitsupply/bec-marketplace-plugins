# Infra Clients

> Reference **13** — Interface, implementation, settings, validator, and DI registration for a new downstream client.

**Chapter rules:** [4_downstream-clients.md](4_downstream-clients.md), [2_layer-boundaries.md](2_layer-boundaries.md). This document is the **step-by-step template**.

**One client per downstream component.** Each external HTTP API, queue, blob store, or pub/sub target gets its own `I*` interface, `Infra/Clients/{Name}/` folder, settings, validator, and `Add*Client` registration.

Adding a **new** downstream dependency requires these artifacts:

1. **Interface** — `App/Clients/Interfaces/IFooClient.cs` — **domain types only** in signature
2. **Implementation** — `Infra/Clients/FooClient/FooClient.cs` (`internal sealed`, `[ExcludeFromCodeCoverage]`)
3. **Wire DTOs** — `Infra/Clients/FooClient/Models/` — external API JSON shapes (**Infra only**)
4. **Mapping** — wire DTO → `App.Models` domain in client (or `Mappers/` subfolder)
5. **Settings** — `Infra/Clients/FooClient/Settings/FooSettings.cs` (`[ExcludeFromCodeCoverage]` record)
6. **Validator** — `Infra/Clients/FooClient/Validators/FooSettingsValidator.cs` (FluentValidation, **required** — fail early at startup)

**Infra receives wire DTOs from external APIs and converts to domain models before returning to App.** App must never reference `Infra/.../Models/`. See [2_layer-boundaries.md](2_layer-boundaries.md).

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

Example implementation: [7_infra-client.cs](../examples/production/7_infra-client.cs)

## Settings record

```csharp
// Infra/Clients/FooClient/Settings/FooSettings.cs
[ExcludeFromCodeCoverage]
public record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
    public string ClientId { get; init; } = default!;
}
```

## Validator

```csharp
// Infra/Clients/FooClient/Validators/FooSettingsValidator.cs
internal sealed class FooSettingsValidator : AbstractValidator<FooSettings>
{
    public FooSettingsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotNull()
            .WithMessage("FooSettings:BaseUrl is required in configuration");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("FooSettings:ClientId is required in configuration");
    }
}
```

## Registration

Every settings record follows [12_configuration-validation.md](12_configuration-validation.md). Registration **must** include `ValidateOnStart()`.

In `Infra/Extensions/ServiceCollectionExtensions.cs`:

```csharp
private static IServiceCollection AddFooClient(this IServiceCollection services, IConfiguration config)
{
    services.AddOptions<FooSettings>()
        .Bind(config.GetSection(nameof(FooSettings)))
        .ValidateOnStart();
    services.AddSingleton<IValidateOptions<FooSettings>>(
        _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));

    services.AddHttpClient<IFooClient, FooClient>()
        .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() // internal SSO-protected APIs only
        .ConfigureHttpClient((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<FooSettings>>().Value;
            client.BaseAddress = opts.BaseUrl;
            client.DefaultRequestHeaders.Add("Scope", $"{opts.ClientId}/.default");
        });

    return services;
}
```

Call from `AddInfrastructure`: `services.AddFooClient(config);`
