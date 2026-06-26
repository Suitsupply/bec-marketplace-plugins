using FluentValidation;
using Template.Api.Messaging.Settings;

namespace Template.Api.Messaging.Validators;

internal sealed class MessageRetryOptionsValidator : AbstractValidator<MessageRetryOptions>
{
    public MessageRetryOptionsValidator()
    {
        RuleFor(x => x.MaxDeliveryCount)
            .GreaterThan(0)
            .WithMessage("MessageRetryOptions:MaxDeliveryCount must be greater than 0")
            .LessThanOrEqualTo(10)
            .WithMessage("MessageRetryOptions:MaxDeliveryCount must be less than or equal to 10");

        RuleFor(x => x.RetryDelay)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("MessageRetryOptions:RetryDelay must be greater than zero");

        RuleFor(x => x.BackoffMultiplier)
            .GreaterThanOrEqualTo(1)
            .WithMessage("MessageRetryOptions:BackoffMultiplier must be 1 or greater (1 = fixed delay, 2 = double each attempt)");
    }
}