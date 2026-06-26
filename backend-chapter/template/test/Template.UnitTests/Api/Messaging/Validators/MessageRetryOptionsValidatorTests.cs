using Template.Api.Messaging.Settings;
using Template.Api.Messaging.Validators;

namespace Template.UnitTests.Api.Messaging.Validators;

public static class MessageRetryOptionsValidatorTests
{
    private static readonly MessageRetryOptionsValidator Validator = new();

    private static MessageRetryOptions ValidOptions() =>
        new() { MaxDeliveryCount = 3, RetryDelay = TimeSpan.FromSeconds(30), BackoffMultiplier = 1 };

    public class Validate
    {
        [Test]
        public void ShouldPass_WhenAllValuesAreValid()
        {
            // Act
            var result = Validator.Validate(ValidOptions());

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ShouldFail_WhenMaxDeliveryCountIsZero()
        {
            // Arrange
            var options = ValidOptions() with { MaxDeliveryCount = 0 };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("MessageRetryOptions:MaxDeliveryCount must be greater than 0"));
        }

        [Test]
        public void ShouldFail_WhenMaxDeliveryCountExceedsTen()
        {
            // Arrange
            var options = ValidOptions() with { MaxDeliveryCount = 11 };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("MessageRetryOptions:MaxDeliveryCount must be less than or equal to 10"));
        }

        [Test]
        public void ShouldFail_WhenRetryDelayIsNotPositive()
        {
            // Arrange
            var options = ValidOptions() with { RetryDelay = TimeSpan.Zero };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("MessageRetryOptions:RetryDelay must be greater than zero"));
        }

        [Test]
        public void ShouldFail_WhenBackoffMultiplierBelowOne()
        {
            // Arrange
            var options = ValidOptions() with { BackoffMultiplier = 0.5 };

            // Act
            var result = Validator.Validate(options);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.ErrorMessage), Does.Contain("MessageRetryOptions:BackoffMultiplier must be 1 or greater (1 = fixed delay, 2 = double each attempt)"));
        }
    }
}