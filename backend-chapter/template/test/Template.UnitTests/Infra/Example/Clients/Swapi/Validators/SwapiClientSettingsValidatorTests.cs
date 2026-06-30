using Template.Infra.Example.Clients.Swapi.Settings;
using Template.Infra.Example.Clients.Swapi.Validators;

namespace Template.UnitTests.Infra.Example.Clients.Swapi.Validators;

public static class SwapiClientSettingsValidatorTests
{
    private static readonly SwapiClientSettingsValidator Validator = new();

    public class Validate
    {
        [Test]
        public void ShouldPass_WhenBaseUrlIsAbsoluteUri()
        {
            // Arrange
            var settings = new SwapiClientSettings { BaseUrl = "https://swapi.info/api/" };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ShouldFail_WhenBaseUrlIsEmpty()
        {
            // Arrange
            var settings = new SwapiClientSettings { BaseUrl = string.Empty };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void ShouldFail_WhenBaseUrlIsNotAnAbsoluteUri()
        {
            // Arrange
            var settings = new SwapiClientSettings { BaseUrl = "not-a-valid-uri" };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }
    }
}