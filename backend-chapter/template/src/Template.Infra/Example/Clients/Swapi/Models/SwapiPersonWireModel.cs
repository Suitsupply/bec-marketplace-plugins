using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Template.Infra.Example.Clients.Swapi.Models;

[ExcludeFromCodeCoverage]
public sealed record SwapiPersonWireModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("height")] string Height,
    [property: JsonPropertyName("mass")] string Mass);