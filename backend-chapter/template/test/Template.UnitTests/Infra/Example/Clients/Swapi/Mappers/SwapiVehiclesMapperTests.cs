using Template.App.Models.Example.Models.Vehicles;
using Template.Infra.Example.Clients.Swapi.Mappers;
using Template.Infra.Example.Clients.Swapi.Models;

namespace Template.UnitTests.Infra.Example.Clients.Swapi.Mappers;

public static class SwapiVehiclesMapperTests
{
    public class NullArgumentChecks
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckStaticMethodParameters(typeof(SwapiVehiclesMapper));
        }
    }

    public class ToDomain
    {
        [Test, AutoData]
        public void ShouldMapWireModel_ToDomain(int id, SwapiVehicleWireModel wire)
        {
            // Act
            var result = SwapiVehiclesMapper.ToDomain(id, wire);

            // Assert
            Assert.That(result, Is.EqualTo(new Vehicle(id, wire.Name, wire.Model, wire.Manufacturer)));
        }
    }
}