using System.Text.Json.Serialization;

namespace Template.Api.Models.Example.v1.Persons.Models;

public sealed record Address(
    [property: JsonPropertyName("street")] string Street);