# Configuration validation (fail early)

> Reference **12** — FluentValidation + `ValidateOnStart()` for every settings class bound from config.

**Chapter rules:** [dotnet-best-practices hub §Configuration validation](../SKILL.md#configuration-validation-fail-early). This document is the **implementation template** (artifacts, code, registration).

Every settings class bound from `IConfiguration` must use FluentValidation and `ValidateOnStart()` — the host must not start with missing or invalid config.

Applies to:

- `ServiceSettings` (via `AddServiceInfo`)
- Infra HTTP/blob/queue client settings (`Infra/Clients/.../Settings/`)
- Api-only options (`Api/.../Settings/` — e.g. retry, messaging)
- Any other `IOptions<T>` / `IOptionsSnapshot<T>` bound from config

## Artifacts per settings type

| Artifact | Location |
|----------|----------|
| Settings record | `.../Settings/{Name}.cs` — `[ExcludeFromCodeCoverage]` (no logic) |
| Validator | `.../Validators/{Name}Validator.cs` — `AbstractValidator<T>` (**has logic** — unit-test, do not exclude) |
| `FluentValidateOptions<T>` adapter | `Infra/Validators/FluentValidateOptions.cs` — **has logic** — unit-test, do not exclude |
| Registration | `Program.cs` or `Infra/Extensions/ServiceCollectionExtensions.cs` |

## `FluentValidateOptions<T>` (once per solution)

`Infra/Validators/FluentValidateOptions.cs`:

```csharp
using FluentValidation;
using Microsoft.Extensions.Options;

namespace {ServiceName}.Infra.Validators;

public sealed class FluentValidateOptions<TOptions>(AbstractValidator<TOptions> validator)
    : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var result = validator.Validate(options);

        if (result.IsValid)
            return ValidateOptionsResult.Success;

        var errors = result.Errors.Select(e => e.ErrorMessage);
        return ValidateOptionsResult.Fail(errors);
    }
}
```

## Registration pattern

```csharp
services.AddOptions<FooSettings>()
    .Bind(config.GetSection(nameof(FooSettings)))
    .ValidateOnStart();
services.AddSingleton<IValidateOptions<FooSettings>>(
    _ => new FluentValidateOptions<FooSettings>(new FooSettingsValidator()));
```

Use `nameof(FooSettings)` for the config section key so it matches the settings class name.

## Validator conventions

```csharp
internal sealed class FooSettingsValidator : AbstractValidator<FooSettings>
{
    public FooSettingsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotNull()
            .WithMessage("FooSettings:BaseUrl is required in configuration");

        RuleFor(x => x.MaxRetries)
            .GreaterThan(0)
            .WithMessage("FooSettings:MaxRetries must be greater than 0");
    }
}
```

- Error messages use `"<Section>:<Property> …"` — section name matches the config key / `nameof` binding.
- Validate all required properties and meaningful constraints (URLs, ranges, non-empty strings).
- `internal sealed` validators; unit-test via `AbstractValidator<T>` in `{ServiceName}.UnitTests`.

## Where to register

| Settings scope | Register in |
|----------------|-------------|
| Service-wide / Infra clients | `ServiceCollectionExtensions.AddInfrastructure` |
| Api-only (host concerns) | `Program.cs` `ConfigureServices` |

## Anti-patterns

- Binding config with `Configure<T>()` or `GetSection().Get<T>()` **without** `ValidateOnStart()` and a validator
- Reading config values lazily on first use with manual `if (string.IsNullOrEmpty(...))` throws at runtime
- Validating only some settings while leaving others unvalidated
