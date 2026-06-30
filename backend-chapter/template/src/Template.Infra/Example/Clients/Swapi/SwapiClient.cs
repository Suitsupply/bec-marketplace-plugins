using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.App.Example.Clients.Interfaces;
using Template.App.Models.Example.Models.Persons;
using Template.App.Models.Example.Models.Vehicles;
using Template.Infra.Example.Clients.Swapi.Mappers;
using Template.Infra.Example.Clients.Swapi.Models;
using Template.Infra.Example.Clients.Swapi.Settings;

namespace Template.Infra.Example.Clients.Swapi;

[ExcludeFromCodeCoverage]
public sealed class SwapiClient(HttpClient httpClient, IOptions<SwapiClientSettings> settings, ILogger<SwapiClient> logger)
    : ISwapiClient
{
    public async Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var requestUri = new Uri(new Uri(settings.Value.BaseUrl), $"people/{id}/");
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("SWAPI returned 404 for person {PersonId}.", id);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<SwapiPersonWireModel>(cancellationToken);
        ArgumentNullException.ThrowIfNull(wire);

        return SwapiPersonsMapper.ToDomain(id, wire);
    }

    public Task UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Template stub — SWAPI is read-only; PUT/PATCH to your downstream write API here in production.
        return Task.CompletedTask;
    }

    public async Task<Vehicle?> GetVehicleAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var requestUri = new Uri(new Uri(settings.Value.BaseUrl), $"vehicles/{id}/");
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("SWAPI returned 404 for vehicle {VehicleId}.", id);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<SwapiVehicleWireModel>(cancellationToken);
        ArgumentNullException.ThrowIfNull(wire);

        return SwapiVehiclesMapper.ToDomain(id, wire);
    }

    public Task CreateVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        // Template stub — SWAPI is read-only; POST to your downstream write API here in production.
        return Task.CompletedTask;
    }
}