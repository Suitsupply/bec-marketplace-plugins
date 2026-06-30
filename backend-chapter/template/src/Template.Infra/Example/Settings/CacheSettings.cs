using System.Diagnostics.CodeAnalysis;

namespace Template.Infra.Example.Settings;

[ExcludeFromCodeCoverage]
public sealed record CacheSettings
{
    public TimeSpan PersonEntryLifetime { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan VehicleEntryLifetime { get; init; } = TimeSpan.FromMinutes(5);
}