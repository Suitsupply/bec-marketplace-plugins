using Template.Api.Example.Mappers.v1.Persons;
using Template.Api.Models.Example.v1.Persons.Models;
using Template.Api.Models.Example.v1.Persons.Requests;
using Template.Api.Models.Example.v1.Persons.Responses;
using Template.App.Models.Example.Models.Persons;

namespace Template.UnitTests.Api.Example.Mappers.v1.Persons;

public static class PersonsMapperTests
{
    public class NullArgumentChecks
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckStaticMethodParameters(typeof(PersonsMapper));
        }
    }

    public class ToDto
    {
        [Test, AutoData]
        public void ShouldMapPerson_ToResponse(Person person)
        {
            // Act
            var result = PersonsMapper.ToDto(person);

            // Assert
            Assert.That(result, Is.EqualTo(new GetPersonResponse(person.Id, person.Name, person.Height, person.Mass, new Address("myStreet"))));
        }
    }

    public class ToDomain
    {
        [Test, AutoData]
        public void ShouldMapRequest_ToDomain(UpdatePersonRequest request)
        {
            // Act
            var result = PersonsMapper.ToDomain(request);

            // Assert
            Assert.That(result, Is.EqualTo(new UpdatePerson(request.Id)));
        }
    }
}