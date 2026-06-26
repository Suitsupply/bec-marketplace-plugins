using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Template.Api.Example.Functions.v1.Vehicles;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.UnitTests.Api.Example.Functions.v1.Vehicles;

public static class CreateVehicleFunctionTests
{
    public abstract class CreateVehicleFunctionTestsBase
    {
        protected readonly Mock<ILogger<CreateVehicleFunction>> Logger;
        protected readonly Mock<IVehiclesService> VehicleService;
        protected readonly CreateVehicleFunction Sut;

        protected CreateVehicleFunctionTestsBase()
        {
            Logger = new Mock<ILogger<CreateVehicleFunction>>();
            VehicleService = new Mock<IVehiclesService>();
            Sut = new CreateVehicleFunction(Logger.Object, VehicleService.Object);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }
    }

    public class NullArgumentChecks : CreateVehicleFunctionTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class CreateVehicleAsync : CreateVehicleFunctionTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnAccepted_WhenCreateSucceeds(string name, string model, string manufacturer)
        {
            // Arrange
            var request = CreateRequest($$"""{"name":"{{name}}","model":"{{model}}","manufacturer":"{{manufacturer}}"}""");
            VehicleService
                .Setup(s => s.CreateVehicleAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await Sut.CreateVehicleAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
            VehicleService.Verify(
                s => s.CreateVehicleAsync(
                    It.Is<Vehicle>(v => v.Name == name && v.Model == model && v.Manufacturer == manufacturer),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldReturn500_WhenServiceThrows(string name, string model, string manufacturer)
        {
            // Arrange
            var request = CreateRequest($$"""{"name":"{{name}}","model":"{{model}}","manufacturer":"{{manufacturer}}"}""");
            VehicleService
                .Setup(s => s.CreateVehicleAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            // Act
            var result = await Sut.CreateVehicleAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }
    }
}