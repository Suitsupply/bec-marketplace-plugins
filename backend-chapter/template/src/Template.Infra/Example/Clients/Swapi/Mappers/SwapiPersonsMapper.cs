using Template.App.Models.Example.Models.Persons;
using Template.Infra.Example.Clients.Swapi.Models;

namespace Template.Infra.Example.Clients.Swapi.Mappers;

public static class SwapiPersonsMapper
{
    public static Person ToDomain(int id, SwapiPersonWireModel wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return new Person(id, wire.Name, wire.Height, wire.Mass);
    }
}