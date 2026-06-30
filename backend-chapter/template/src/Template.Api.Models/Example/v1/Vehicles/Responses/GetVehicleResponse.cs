using Template.Api.Models.Example.v1.Vehicles.Models;

namespace Template.Api.Models.Example.v1.Vehicles.Responses;

public sealed record GetVehicleResponse(
    int Id,
    string Name,
    string Model,
    string Manufacturer,
    Owner Owner); // We're ignoring this in the rest of the code, but it's here for demonstration purposes on where to define the models.