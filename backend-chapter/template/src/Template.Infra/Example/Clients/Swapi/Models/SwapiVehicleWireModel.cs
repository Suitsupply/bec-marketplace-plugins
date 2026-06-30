using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Template.Infra.Example.Clients.Swapi.Models;

[ExcludeFromCodeCoverage]
public sealed record SwapiVehicleWireModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("manufacturer")] string Manufacturer);