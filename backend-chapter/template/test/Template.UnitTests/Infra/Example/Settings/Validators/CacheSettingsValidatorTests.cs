using Template.Infra.Example.Settings;
using Template.Infra.Example.Settings.Validators;

namespace Template.UnitTests.Infra.Example.Settings.Validators;

public static class CacheSettingsValidatorTests
{
    private static readonly CacheSettingsValidator Validator = new();

    public class Validate
    {
        [Test]
        public void ShouldPass_WhenLifetimesArePositive()
        {
            // Arrange
            var settings = new CacheSettings
            {
                PersonEntryLifetime = TimeSpan.FromMinutes(2),
                VehicleEntryLifetime = TimeSpan.FromMinutes(5),
            };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ShouldFail_WhenPersonEntryLifetimeIsNotPositive()
        {
            // Arrange
            var settings = new CacheSettings
            {
                PersonEntryLifetime = TimeSpan.Zero,
                VehicleEntryLifetime = TimeSpan.FromMinutes(5),
            };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("CacheSettings:PersonEntryLifetime must be greater than zero"));
        }

        [Test]
        public void ShouldFail_WhenVehicleEntryLifetimeIsNotPositive()
        {
            // Arrange
            var settings = new CacheSettings
            {
                PersonEntryLifetime = TimeSpan.FromMinutes(2),
                VehicleEntryLifetime = TimeSpan.Zero,
            };

            // Act
            var result = Validator.Validate(settings);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("CacheSettings:VehicleEntryLifetime must be greater than zero"));
        }
    }
}