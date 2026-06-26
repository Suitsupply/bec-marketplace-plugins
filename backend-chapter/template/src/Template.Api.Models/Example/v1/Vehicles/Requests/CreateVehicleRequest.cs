using System.Text.Json.Serialization;
using Template.Api.Models.Example.v1.Vehicles.Models;

namespace Template.Api.Models.Example.v1.Vehicles.Requests;

public sealed record CreateVehicleRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("manufacturer")] string Manufacturer,
    [property: JsonPropertyName("owner")] Owner Owner); // We're ignoring this in the rest of the code, but it's here for demonstration purposes on where to define the models.