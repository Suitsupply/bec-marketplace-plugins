using Template.Infra.Example.Settings;
using Template.Infra.Example.Settings.Validators;

namespace Template.UnitTests.Infra.Example.Settings.Validators;

public static class ServiceBusOptionsValidatorTests
{
    private static readonly ServiceBusOptionsValidator Validator = new();

    public class Validate
    {
        [Test]
        public void ShouldPass_WhenAllRequiredPropertiesAreSet()
        {
            // Arrange
            var options = new ServiceBusOptions
            {
                StoreServiceBus = new StoreServiceBusOptions
                {
                    FullyQualifiedNamespace = "test.servicebus.windows.net",
                    UpdatePersonQueueName = "update-person",
                }
            };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ShouldFail_WhenFullyQualifiedNamespaceIsMissing()
        {
            // Arrange
            var options = new ServiceBusOptions
            {
                StoreServiceBus = new StoreServiceBusOptions
                {
                    UpdatePersonQueueName = "update-person",
                }
            };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("ServiceBusOptions:StoreServiceBus:FullyQualifiedNamespace is required in configuration"));
        }

        [Test]
        public void ShouldFail_WhenUpdatePersonQueueNameIsMissing()
        {
            // Arrange
            var options = new ServiceBusOptions
            {
                StoreServiceBus = new StoreServiceBusOptions
                {
                    FullyQualifiedNamespace = "test.servicebus.windows.net",
                    UpdatePersonQueueName = string.Empty,
                }
            };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("ServiceBusOptions:StoreServiceBus:UpdatePersonQueueName is required in configuration"));
        }
    }
}