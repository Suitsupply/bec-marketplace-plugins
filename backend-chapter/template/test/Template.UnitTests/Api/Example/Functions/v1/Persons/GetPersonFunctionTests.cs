using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Template.Api.Example.Functions.v1.Persons;
using Template.Api.Example.Mappers.v1.Persons;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Models.Example.Models.Persons;

namespace Template.UnitTests.Api.Example.Functions.v1.Persons;

public static class GetPersonFunctionTests
{
    public abstract class GetPersonFunctionTestsBase
    {
        protected readonly Mock<ILogger<GetPersonFunction>> Logger;
        protected readonly Mock<IPersonsService> PersonService;
        protected readonly GetPersonFunction Sut;

        protected GetPersonFunctionTestsBase()
        {
            Logger = new Mock<ILogger<GetPersonFunction>>();
            PersonService = new Mock<IPersonsService>();
            Sut = new GetPersonFunction(Logger.Object, PersonService.Object);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }
    }

    public class NullArgumentChecks : GetPersonFunctionTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class GetPersonAsync : GetPersonFunctionTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnOk_WhenPersonExists(int id, Person person)
        {
            // Arrange
            var request = CreateRequest("{}");
            PersonService
                .Setup(s => s.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);
            var expected = PersonsMapper.ToDto(person);

            // Act
            var result = await Sut.GetPersonAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.EqualTo(expected));
        }

        [Test, AutoData]
        public async Task ShouldReturnNotFound_WhenPersonMissing(int id)
        {
            // Arrange
            var request = CreateRequest("{}");
            PersonService
                .Setup(s => s.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person?)null);

            // Act
            var result = await Sut.GetPersonAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test, AutoData]
        public async Task ShouldReturn500_WhenServiceThrows(int id, string errorMessage)
        {
            // Arrange
            var request = CreateRequest("{}");
            PersonService
                .Setup(s => s.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(errorMessage));

            // Act
            var result = await Sut.GetPersonAsync(request, id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }
    }
}