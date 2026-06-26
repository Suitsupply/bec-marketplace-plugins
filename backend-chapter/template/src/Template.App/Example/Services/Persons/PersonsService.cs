using Microsoft.Extensions.Logging;
using Template.App.Example.Clients.Interfaces;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Models.Example.Models.Persons;

namespace Template.App.Example.Services.Persons;

public sealed class PersonsService(ISwapiClient swapiClient, ILogger<PersonsService> logger) : IPersonsService
{
    public async Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        logger.LogInformation("Retrieving person with id {PersonId}.", id);

        var person = await swapiClient.GetPersonAsync(id, cancellationToken);

        logger.LogInformation("Retrieved person with id {PersonId}.", id);
        return person;
    }

    public async Task<Person?> UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(message.Id);

        logger.LogInformation("Processing person requested message for id {PersonId}.", message.Id);

        var person = await swapiClient.GetPersonAsync(message.Id, cancellationToken);

        if (person is null)
        {
            logger.LogWarning("Person {PersonId} was not found in SWAPI.", message.Id);
            return null;
        }

        await swapiClient.UpdatePersonAsync(message, cancellationToken);

        logger.LogInformation("Resolved person {PersonId} to {PersonName}.", person.Id, person.Name);
        return person;
    }
}