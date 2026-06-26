using Microsoft.Extensions.Logging;
using Template.App.Example.Clients.Interfaces;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.App.Example.Services.Vehicles;

public sealed class VehiclesService(ISwapiClient swapiClient, ILogger<VehiclesService> logger) : IVehiclesService
{
    public async Task<Vehicle?> GetVehicleAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        logger.LogInformation("Retrieving vehicle with id {VehicleId}.", id);

        var vehicle = await swapiClient.GetVehicleAsync(id, cancellationToken);

        logger.LogInformation("Retrieved vehicle with id {VehicleId}.", id);
        return vehicle;
    }

    public async Task CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        logger.LogInformation("Creating vehicle with id {VehicleId}.", vehicle.Id);

        await swapiClient.CreateVehicleAsync(vehicle, cancellationToken);

        logger.LogInformation("Created vehicle with id {VehicleId}.", vehicle.Id);
    }
}