using FluentValidation;
using Template.Infra.Example.Clients.Swapi.Settings;

namespace Template.Infra.Example.Clients.Swapi.Validators;

public sealed class SwapiClientSettingsValidator : AbstractValidator<SwapiClientSettings>
{
    public SwapiClientSettingsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
    }
}