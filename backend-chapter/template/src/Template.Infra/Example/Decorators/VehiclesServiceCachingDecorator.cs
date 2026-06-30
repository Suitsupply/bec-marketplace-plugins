using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Models.Example.Models.Vehicles;
using Template.Infra.Example.Settings;

namespace Template.Infra.Example.Decorators;

// NOTE: This is a simple caching decorator for demonstration purposes. In a real-world application, consider using a more robust caching strategy, such as distributed caching or a dedicated caching library.
public sealed class VehiclesServiceCachingDecorator(IVehiclesService inner, IMemoryCache cache, IOptions<CacheSettings> cacheSettings) : IVehiclesService
{
    internal static string VehicleKey(int id) => $"example:vehicle:{id}";

    public Task<Vehicle?> GetVehicleAsync(int id, CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateAsync(
            VehicleKey(id),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = cacheSettings.Value.VehicleEntryLifetime;
                return await inner.GetVehicleAsync(id, cancellationToken);
            });
    }

    public Task CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        return inner.CreateVehicleAsync(vehicle, cancellationToken);
    }
}