using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Models.Example.Models.Vehicles;
using Template.Infra.Example.Decorators;
using Template.Infra.Example.Settings;

namespace Template.UnitTests.Infra.Example.Decorators;

public static class VehiclesServiceCachingDecoratorTests
{
    public abstract class VehiclesServiceCachingDecoratorTestsBase
    {
        protected readonly Mock<IVehiclesService> Inner;
        protected readonly MemoryCache Cache;
        protected readonly VehiclesServiceCachingDecorator Sut;

        protected VehiclesServiceCachingDecoratorTestsBase()
        {
            Inner = new Mock<IVehiclesService>();
            Cache = new MemoryCache(new MemoryCacheOptions());
            Sut = new VehiclesServiceCachingDecorator(Inner.Object, Cache, Options.Create(new CacheSettings()));
        }
    }

    public class GetVehicleAsync : VehiclesServiceCachingDecoratorTestsBase
    {
        [Test, AutoData]
        public async Task ShouldCacheResult_WhenCalledTwiceWithSameId(int id, Vehicle vehicle)
        {
            // Arrange
            Inner
                .Setup(s => s.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            // Act
            var first = await Sut.GetVehicleAsync(id, CancellationToken.None);
            var second = await Sut.GetVehicleAsync(id, CancellationToken.None);

            // Assert
            Assert.That(first, Is.EqualTo(vehicle));
            Assert.That(second, Is.EqualTo(vehicle));
            Inner.Verify(s => s.GetVehicleAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class CreateVehicleAsync : VehiclesServiceCachingDecoratorTestsBase
    {
        [Test, AutoData]
        public async Task ShouldDelegateToInner_WhenCalled(Vehicle vehicle)
        {
            // Arrange
            Inner
                .Setup(s => s.CreateVehicleAsync(vehicle, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await Sut.CreateVehicleAsync(vehicle, CancellationToken.None);

            // Assert
            Inner.Verify(s => s.CreateVehicleAsync(vehicle, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}