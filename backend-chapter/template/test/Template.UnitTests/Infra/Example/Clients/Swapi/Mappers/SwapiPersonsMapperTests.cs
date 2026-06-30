using Template.Infra.Example.Clients.Swapi.Mappers;
using Template.Infra.Example.Clients.Swapi.Models;

namespace Template.UnitTests.Infra.Example.Clients.Swapi.Mappers;

public static class SwapiPersonsMapperTests
{
    public class NullArgumentChecks
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckStaticMethodParameters(typeof(SwapiPersonsMapper));
        }
    }

    public class ToDomain
    {
        [Test, AutoData]
        public void ShouldMapWireModel_ToDomain(int id, SwapiPersonWireModel wire)
        {
            // Act
            var result = SwapiPersonsMapper.ToDomain(id, wire);

            // Assert
            Assert.That(result.Id, Is.EqualTo(id));
            Assert.That(result.Name, Is.EqualTo(wire.Name));
            Assert.That(result.Height, Is.EqualTo(wire.Height));
            Assert.That(result.Mass, Is.EqualTo(wire.Mass));
        }
    }
}