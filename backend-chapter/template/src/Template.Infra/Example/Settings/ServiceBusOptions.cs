using System.Diagnostics.CodeAnalysis;

namespace Template.Infra.Example.Settings;

[ExcludeFromCodeCoverage]
public sealed record ServiceBusOptions
{
    public StoreServiceBusOptions StoreServiceBus { get; init; } = new();
}