using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.Api.Messaging;
using Template.Api.Messaging.Settings;

namespace Template.UnitTests.Api.Messaging;

public static class ServiceBusRetrySchedulerTests
{
    public abstract class ServiceBusRetrySchedulerTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly Mock<ServiceBusClient> BusClient;
        protected readonly Mock<ServiceBusSender> Sender;
        protected readonly Mock<ServiceBusMessageActions> MessageActions;
        protected readonly Mock<ILogger<ServiceBusRetryScheduler>> Logger;
        protected readonly ServiceBusRetryScheduler Scheduler;

        protected const string QueueName = "test-queue";

        protected ServiceBusRetrySchedulerTestsBase()
        {
            BusClient = new Mock<ServiceBusClient>();
            Sender = new Mock<ServiceBusSender>();
            MessageActions = new Mock<ServiceBusMessageActions>();
            Logger = new Mock<ILogger<ServiceBusRetryScheduler>>();

            BusClient
                .Setup(c => c.CreateSender(It.IsAny<string>()))
                .Returns(Sender.Object);

            Sender
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Sender
                .Setup(s => s.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            Scheduler = new ServiceBusRetryScheduler(
                BusClient.Object,
                Options.Create(new MessageRetryOptions { MaxDeliveryCount = 3, RetryDelay = TimeSpan.FromSeconds(5), BackoffMultiplier = 1 }),
                Logger.Object);
        }

        protected static ServiceBusReceivedMessage CreateMessage(string body, int? deliveryCount = null, int nativeDeliveryCount = 0)
        {
            var properties = deliveryCount.HasValue
                ? new Dictionary<string, object> { ["DeliveryCount"] = deliveryCount.Value }
                : null;

            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Encoding.UTF8.GetBytes(body)),
                deliveryCount: nativeDeliveryCount,
                properties: properties);
        }
    }

    public class NullArgumentChecks : ServiceBusRetrySchedulerTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act
            ArgumentsNullChecker.CheckMethodParameters(Scheduler);
        }
    }

    public class RescheduleOrDeadLetterAsync : ServiceBusRetrySchedulerTestsBase
    {
        [Test]
        public async Task ShouldRescheduleMessage_WhenDeliveryCountBelowMax()
        {
            // Arrange
            var deliveryCount = 2;
            var body = Fixture.Create<string>();
            var exception = new InvalidOperationException("transient");
            var message = CreateMessage(body, deliveryCount);

            ServiceBusMessage? capturedRetryMessage = null;
            Sender
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedRetryMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.Rescheduled));
            Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
            MessageActions.Verify(a => a.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(capturedRetryMessage, Is.Not.Null);
            Assert.That(capturedRetryMessage!.ApplicationProperties["DeliveryCount"], Is.EqualTo(deliveryCount + 1));
        }

        [Test]
        public async Task ShouldDeadLetter_WhenDeliveryCountEqualsMax()
        {
            // Arrange
            var body = Fixture.Create<string>();
            var exception = new InvalidOperationException("simulated failure");
            var message = CreateMessage(body, 3);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.DeadLettered));
            Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.DeadLetterMessageAsync(message, null, exception.Message, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShouldFallBackToBrokerDeliveryCount_WhenApplicationPropertyIsAbsent()
        {
            // Arrange
            var body = Fixture.Create<string>();
            var exception = new InvalidOperationException("simulated failure");
            var message = CreateMessage(body, nativeDeliveryCount: 3);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.DeadLettered));
            Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.DeadLetterMessageAsync(message, null, exception.Message, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShouldDeadLetterImmediately_WhenExceptionTypeIsConfiguredForImmediateDeadLettering()
        {
            // Arrange
            var body = Fixture.Create<string>();
            var exception = new ArgumentException("permanent validation failure");
            var message = CreateMessage(body, deliveryCount: 1);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, [typeof(ArgumentException)], CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.DeadLettered));
            Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
            MessageActions.Verify(a => a.DeadLetterMessageAsync(message, null, exception.Message, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ShouldReschedule_WhenExceptionTypeIsNotConfiguredForImmediateDeadLettering()
        {
            // Arrange
            var body = Fixture.Create<string>();
            var exception = new InvalidOperationException("transient");
            var message = CreateMessage(body, deliveryCount: 1);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, [typeof(ArgumentException)], CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.Rescheduled));
            Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
            MessageActions.Verify(a => a.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task ShouldApplyExponentialBackoff_WhenBackoffMultiplierIsGreaterThanOne()
        {
            // Arrange
            var body = Fixture.Create<string>();
            var exception = new InvalidOperationException("transient");
            const int deliveryCount = 2;
            var retryDelay = TimeSpan.FromSeconds(10);
            var scheduler = new ServiceBusRetryScheduler(BusClient.Object,
                Options.Create(new MessageRetryOptions { MaxDeliveryCount = 5, RetryDelay = retryDelay, BackoffMultiplier = 2 }),
                Logger.Object);

            var message = CreateMessage(body, deliveryCount);

            ServiceBusMessage? capturedRetryMessage = null;
            Sender
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedRetryMessage = msg)
                .Returns(Task.CompletedTask);

            var before = DateTimeOffset.UtcNow;

            // Act
            await scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, exception, CancellationToken.None);

            var after = DateTimeOffset.UtcNow;

            // Assert — delay = RetryDelay × BackoffMultiplier^(deliveryCount − 1) = 10s × 2^1 = 20s
            var expectedDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * Math.Pow(2, deliveryCount - 1));
            Assert.That(capturedRetryMessage, Is.Not.Null);
            Assert.That(capturedRetryMessage!.ScheduledEnqueueTime,
                Is.GreaterThanOrEqualTo(before.Add(expectedDelay))
                .And.
                LessThanOrEqualTo(after.Add(expectedDelay).AddSeconds(2)));
        }
    }
}