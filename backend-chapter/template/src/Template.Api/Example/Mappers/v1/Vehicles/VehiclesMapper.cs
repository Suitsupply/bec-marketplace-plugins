using Template.Api.Models.Example.v1.Vehicles.Models;
using Template.Api.Models.Example.v1.Vehicles.Requests;
using Template.Api.Models.Example.v1.Vehicles.Responses;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.Api.Example.Mappers.v1.Vehicles;

public static class VehiclesMapper
{
    public static GetVehicleResponse ToDto(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        return new GetVehicleResponse(vehicle.Id, vehicle.Name, vehicle.Model, vehicle.Manufacturer, new Owner("John Doe"));
    }

    public static Vehicle ToDomain(CreateVehicleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Vehicle(0, request.Name, request.Model, request.Manufacturer);
    }
}