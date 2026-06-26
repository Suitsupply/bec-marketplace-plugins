using System.Text.Json.Serialization;

namespace Template.Api.Models.Example.v1.Vehicles.Models;

public sealed record Owner(
    [property: JsonPropertyName("name")] string Name);