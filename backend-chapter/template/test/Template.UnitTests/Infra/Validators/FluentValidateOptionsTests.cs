using Template.Infra.Example.Settings;
using Template.Infra.Example.Settings.Validators;
using Template.Infra.Validators;

namespace Template.UnitTests.Infra.Validators;

public static class FluentValidateOptionsTests
{
    private static readonly FluentValidateOptions<CacheSettings> Sut = new(new CacheSettingsValidator());

    public class Validate
    {
        [Test]
        public void ShouldReturnSuccess_WhenOptionsAreValid()
        {
            // Arrange
            var options = new CacheSettings
            {
                PersonEntryLifetime = TimeSpan.FromMinutes(1),
                VehicleEntryLifetime = TimeSpan.FromMinutes(1),
            };

            // Act
            var result = Sut.Validate(name: null, options);

            // Assert
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void ShouldReturnFail_WhenOptionsAreInvalid()
        {
            // Arrange
            var options = new CacheSettings
            {
                PersonEntryLifetime = TimeSpan.Zero,
                VehicleEntryLifetime = TimeSpan.Zero,
            };

            // Act
            var result = Sut.Validate(name: null, options);

            // Assert
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Is.Not.Empty);
        }

        [Test]
        public void ShouldThrow_WhenOptionsIsNull()
        {
            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => Sut.Validate(name: null, options: null!));
        }
    }
}