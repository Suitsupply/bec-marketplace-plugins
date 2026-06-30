using Template.App.Models.Example.Models.Vehicles;
using Template.Infra.Example.Clients.Swapi.Models;

namespace Template.Infra.Example.Clients.Swapi.Mappers;

public static class SwapiVehiclesMapper
{
    public static Vehicle ToDomain(int id, SwapiVehicleWireModel wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return new Vehicle(id, wire.Name, wire.Model, wire.Manufacturer);
    }
}