using System.Diagnostics.CodeAnalysis;

namespace Template.Infra.Example.Settings;

[ExcludeFromCodeCoverage]
public sealed record StoreServiceBusOptions
{
    public string FullyQualifiedNamespace { get; init; } = string.Empty;

    public string UpdatePersonQueueName { get; init; } = "update-person";
}