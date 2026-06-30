using System.Text.Json.Serialization;
using Template.Api.Models.Example.v1.Persons.Models;

namespace Template.Api.Models.Example.v1.Persons.Requests;

public sealed record UpdatePersonRequest(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("address")] Address Address); // We're ignoring this in the rest of the code, but it's here for demonstration purposes on where to define the models.