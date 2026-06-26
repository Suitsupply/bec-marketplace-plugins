using Template.App.Models.Example.Models.Persons;
using Template.App.Models.Example.Models.Vehicles;

namespace Template.App.Example.Clients.Interfaces;

public interface ISwapiClient
{
    Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default);

    Task UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetVehicleAsync(int id, CancellationToken cancellationToken = default);

    Task CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}