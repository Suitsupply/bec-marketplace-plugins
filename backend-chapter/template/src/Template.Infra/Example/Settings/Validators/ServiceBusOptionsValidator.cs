using FluentValidation;

namespace Template.Infra.Example.Settings.Validators;

public sealed class ServiceBusOptionsValidator : AbstractValidator<ServiceBusOptions>
{
    public ServiceBusOptionsValidator()
    {
        RuleFor(x => x.StoreServiceBus.FullyQualifiedNamespace)
            .NotEmpty()
            .WithMessage("ServiceBusOptions:StoreServiceBus:FullyQualifiedNamespace is required in configuration");

        RuleFor(x => x.StoreServiceBus.UpdatePersonQueueName)
            .NotEmpty()
            .WithMessage("ServiceBusOptions:StoreServiceBus:UpdatePersonQueueName is required in configuration");
    }
}