using Microsoft.Extensions.Logging;
using Template.App.Example.Clients.Interfaces;
using Template.App.Example.Services.Persons;
using Template.App.Models.Example.Models.Persons;

namespace Template.UnitTests.App.Example.Services.Persons;

public static class PersonsServiceTests
{
    public abstract class PersonsServiceTestsBase
    {
        protected readonly Mock<ISwapiClient> SwapiClient;
        protected readonly Mock<ILogger<PersonsService>> Logger;
        protected readonly PersonsService Sut;

        protected PersonsServiceTestsBase()
        {
            SwapiClient = new Mock<ISwapiClient>();
            Logger = new Mock<ILogger<PersonsService>>();
            Sut = new PersonsService(SwapiClient.Object, Logger.Object);
        }
    }

    public class NullArgumentChecks : PersonsServiceTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class GetPersonAsync : PersonsServiceTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnPerson_WhenClientReturnsPerson(int id, Person person)
        {
            // Arrange
            SwapiClient
                .Setup(c => c.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            var result = await Sut.GetPersonAsync(id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(person));
        }
    }

    public class UpdatePersonAsync : PersonsServiceTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnPerson_WhenSwapiReturnsPerson(Person person)
        {
            // Arrange
            var message = new UpdatePerson(person.Id);
            SwapiClient
                .Setup(c => c.GetPersonAsync(person.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            var result = await Sut.UpdatePersonAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(person));
            SwapiClient.Verify(c => c.UpdatePersonAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldReturnNull_WhenSwapiReturnsNoPerson(int id)
        {
            // Arrange
            var message = new UpdatePerson(id);
            SwapiClient
                .Setup(c => c.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person?)null);

            // Act
            var result = await Sut.UpdatePersonAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Null);
            SwapiClient.Verify(c => c.UpdatePersonAsync(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}