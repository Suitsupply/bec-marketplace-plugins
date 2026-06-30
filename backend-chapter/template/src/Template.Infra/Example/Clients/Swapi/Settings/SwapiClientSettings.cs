using System.Diagnostics.CodeAnalysis;

namespace Template.Infra.Example.Clients.Swapi.Settings;

[ExcludeFromCodeCoverage]
public sealed record SwapiClientSettings
{
    public string BaseUrl { get; init; } = "https://swapi.info/api/";
}