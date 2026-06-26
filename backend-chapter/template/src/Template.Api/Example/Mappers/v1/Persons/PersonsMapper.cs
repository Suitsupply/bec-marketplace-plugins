using Template.Api.Models.Example.v1.Persons.Models;
using Template.Api.Models.Example.v1.Persons.Requests;
using Template.Api.Models.Example.v1.Persons.Responses;
using Template.App.Models.Example.Models.Persons;

namespace Template.Api.Example.Mappers.v1.Persons;

public static class PersonsMapper
{
    public static GetPersonResponse ToDto(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new GetPersonResponse(person.Id, person.Name, person.Height, person.Mass, new Address("myStreet"));
    }

    public static UpdatePerson ToDomain(UpdatePersonRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Id);

        return new UpdatePerson(request.Id);
    }
}