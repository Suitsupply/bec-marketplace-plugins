# DI, configuration, and coverage exclusions

> Example **4** — Constructor injection, fail-early config, and `[ExcludeFromCodeCoverage]` placement.

## Dependency injection

```csharp
// ✗ wrong — service locator in business code
var client = serviceProvider.GetRequiredService<IFooClient>();

// ✓ correct — constructor injection
public sealed class FooService(IFooClient fooClient)
{
    public Task ProcessAsync(CancellationToken ct) => fooClient.GetAsync(ct);
}
```

## Configuration validation (fail early)

```csharp
// ✗ wrong — binds config without startup validation; fails at runtime on first use
services.Configure<FooSettings>(config.GetSection(nameof(FooSettings)));

// ✓ correct — FluentValidation + ValidateOnStart; host refuses to start
services.AddOptions<FooSettings>()
    .Bind(config.GetSection(nameof(FooSettings)))
    .ValidateOnStart();
services.AddSingleton<IValidateOptions<FooSettings>>(
    _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));
```

## Code coverage exclusions

```csharp
// ✓ correct — settings record has no logic; exclude from coverage
[ExcludeFromCodeCoverage]
internal sealed record FooSettings
{
    public Uri BaseUrl { get; init; } = default!;
}

// ✓ correct — Infra client implementation (covered by component/integration tests)
[ExcludeFromCodeCoverage]
internal sealed class FooClient(HttpClient httpClient) : IFooClient { ... }

// ✗ wrong — App validator has rules to test; must not be excluded
[ExcludeFromCodeCoverage]
internal sealed class FooSettingsValidator : AbstractValidator<FooSettings> { ... }
```
