using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Template.Api.Example.Functions.v1.Vehicles;
using Template.Api.Example.Mappers.v1.Vehicles;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.UnitTests.Api.Example.Functions.v1.Vehicles;

public static class GetVehicleFunctionTests
{
    public abstract class GetVehicleFunctionTestsBase
    {
        protected readonly Mock<ILogger<GetVehicleFunction>> Logger;
        protected readonly Mock<IVehiclesService> VehicleService;
        protected readonly GetVehicleFunction Sut;

        protected GetVehicleFunctionTestsBase()
        {
            Logger = new Mock<ILogger<GetVehicleFunction>>();
            VehicleService = new Mock<IVehiclesService>();
            Sut = new GetVehicleFunction(Logger.Object, VehicleService.Object);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }
    }

    public class NullArgumentChecks : GetVehicleFunctionTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class GetVehicleAsync : GetVehicleFunctionTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnOk_WhenVehicleExists(int id, Vehicle vehicle)
        {
            // Arrange
            var request = CreateRequest("{}");
            VehicleService
                .Setup(s => s.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);
            var expected = VehiclesMapper.ToDto(vehicle);

            // Act
            var result = await Sut.GetVehicleAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.EqualTo(expected));
        }

        [Test, AutoData]
        public async Task ShouldReturnNotFound_WhenVehicleMissing(int id)
        {
            // Arrange
            var request = CreateRequest("{}");
            VehicleService
                .Setup(s => s.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var result = await Sut.GetVehicleAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test, AutoData]
        public async Task ShouldReturn500_WhenServiceThrows(int id, string errorMessage)
        {
            // Arrange
            var request = CreateRequest("{}");
            VehicleService
                .Setup(s => s.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(errorMessage));

            // Act
            var result = await Sut.GetVehicleAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }
    }
}