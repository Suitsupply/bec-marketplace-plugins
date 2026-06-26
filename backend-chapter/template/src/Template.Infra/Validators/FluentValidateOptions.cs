using FluentValidation;
using Microsoft.Extensions.Options;

namespace Template.Infra.Validators;

// TODO: Should be coming from a nuget package in the future.
public sealed class FluentValidateOptions<TOptions>(IValidator<TOptions> validator) : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var result = validator.Validate(options);
        return result.IsValid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}