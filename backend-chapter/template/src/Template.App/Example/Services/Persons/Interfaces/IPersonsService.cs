using Template.App.Models.Example.Models.Persons;

namespace Template.App.Example.Services.Persons.Interfaces;

public interface IPersonsService
{
    Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default);

    Task<Person?> UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default);
}