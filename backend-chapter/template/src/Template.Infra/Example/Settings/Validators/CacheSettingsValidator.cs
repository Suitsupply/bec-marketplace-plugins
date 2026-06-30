using FluentValidation;

namespace Template.Infra.Example.Settings.Validators;

public sealed class CacheSettingsValidator : AbstractValidator<CacheSettings>
{
    public CacheSettingsValidator()
    {
        RuleFor(x => x.PersonEntryLifetime)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("CacheSettings:PersonEntryLifetime must be greater than zero");

        RuleFor(x => x.VehicleEntryLifetime)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("CacheSettings:VehicleEntryLifetime must be greater than zero");
    }
}