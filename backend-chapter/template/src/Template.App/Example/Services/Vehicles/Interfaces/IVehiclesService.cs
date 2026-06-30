using Template.App.Models.Example.Models.Vehicles;

namespace Template.App.Example.Services.Vehicles.Interfaces;

public interface IVehiclesService
{
    Task<Vehicle?> GetVehicleAsync(int id, CancellationToken cancellationToken = default);

    Task CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}