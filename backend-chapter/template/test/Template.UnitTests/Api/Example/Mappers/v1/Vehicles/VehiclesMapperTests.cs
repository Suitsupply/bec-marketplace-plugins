using Template.Api.Example.Mappers.v1.Vehicles;
using Template.Api.Models.Example.v1.Vehicles.Models;
using Template.Api.Models.Example.v1.Vehicles.Requests;
using Template.Api.Models.Example.v1.Vehicles.Responses;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.UnitTests.Api.Example.Mappers.v1.Vehicles;

public static class VehiclesMapperTests
{
    public class NullArgumentChecks
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckStaticMethodParameters(typeof(VehiclesMapper));
        }
    }

    public class ToDto
    {
        [Test, AutoData]
        public void ShouldMapVehicle_ToResponse(Vehicle vehicle)
        {
            // Act
            var result = VehiclesMapper.ToDto(vehicle);

            // Assert
            Assert.That(result, Is.EqualTo(new GetVehicleResponse(vehicle.Id, vehicle.Name, vehicle.Model, vehicle.Manufacturer, new Owner("John Doe"))));
        }
    }

    public class ToDomain
    {
        [Test, AutoData]
        public void ShouldMapRequest_ToDomain(CreateVehicleRequest request)
        {
            // Act
            var result = VehiclesMapper.ToDomain(request);

            // Assert
            Assert.That(result, Is.EqualTo(new Vehicle(0, request.Name, request.Model, request.Manufacturer)));
        }
    }
}