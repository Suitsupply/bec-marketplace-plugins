using Microsoft.Extensions.Logging;
using Template.App.Example.Clients.Interfaces;
using Template.App.Example.Services.Vehicles;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.UnitTests.App.Example.Services.Vehicles;

public static class VehiclesServiceTests
{
    public abstract class VehiclesServiceTestsBase
    {
        protected readonly Mock<ISwapiClient> SwapiClient;
        protected readonly Mock<ILogger<VehiclesService>> Logger;
        protected readonly VehiclesService Sut;

        protected VehiclesServiceTestsBase()
        {
            SwapiClient = new Mock<ISwapiClient>();
            Logger = new Mock<ILogger<VehiclesService>>();
            Sut = new VehiclesService(SwapiClient.Object, Logger.Object);
        }
    }

    public class NullArgumentChecks : VehiclesServiceTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class GetVehicleAsync : VehiclesServiceTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnVehicle_WhenClientReturnsVehicle(int id, Vehicle vehicle)
        {
            // Arrange
            SwapiClient
                .Setup(c => c.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            // Act
            var result = await Sut.GetVehicleAsync(id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(vehicle));
        }
    }

    public class CreateVehicleAsync : VehiclesServiceTestsBase
    {
        [Test, AutoData]
        public async Task ShouldCallClient_WhenVehicleIsValid(Vehicle vehicle)
        {
            // Arrange
            SwapiClient
                .Setup(c => c.CreateVehicleAsync(vehicle, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await Sut.CreateVehicleAsync(vehicle, CancellationToken.None);

            // Assert
            SwapiClient.Verify(
                c => c.CreateVehicleAsync(vehicle, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}