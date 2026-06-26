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
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Scheduler);
        }
    }

    public class RescheduleOrDeadLetterAsync : ServiceBusRetrySchedulerTestsBase
    {
        [Test, AutoData]
        public async Task ShouldRescheduleMessage_WhenDeliveryCountBelowMax(string body)
        {
            // Arrange
            var deliveryCount = 2;
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

        [Test, AutoData]
        public async Task ShouldDeadLetter_WhenDeliveryCountEqualsMax(string body)
        {
            // Arrange
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

        [Test, AutoData]
        public async Task ShouldFallBackToBrokerDeliveryCount_WhenPropertyMissing(string body)
        {
            // Arrange — no DeliveryCount application property, so the broker's native count is used.
            var message = CreateMessage(body, deliveryCount: null, nativeDeliveryCount: 1);

            ServiceBusMessage? capturedRetryMessage = null;
            Sender
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedRetryMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, new InvalidOperationException("transient"), CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.Rescheduled));
            Assert.That(capturedRetryMessage, Is.Not.Null);
            Assert.That(capturedRetryMessage!.ApplicationProperties["DeliveryCount"], Is.EqualTo(2));
        }

        [Test, AutoData]
        public async Task ShouldFallBackToBrokerDeliveryCount_WhenPropertyIsNotAnInteger(string body)
        {
            // Arrange — DeliveryCount property present but not an int, so the broker's native count is used.
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Encoding.UTF8.GetBytes(body)),
                deliveryCount: 1,
                properties: new Dictionary<string, object> { ["DeliveryCount"] = "not-an-int" });

            ServiceBusMessage? capturedRetryMessage = null;
            Sender
                .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedRetryMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, new InvalidOperationException("transient"), CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(RetryOutcome.Rescheduled));
            Assert.That(capturedRetryMessage, Is.Not.Null);
            Assert.That(capturedRetryMessage!.ApplicationProperties["DeliveryCount"], Is.EqualTo(2));
        }
    }

    public class DisposeAsync : ServiceBusRetrySchedulerTestsBase
    {
        [Test, AutoData]
        public async Task ShouldDisposeCreatedSenders(string body)
        {
            // Arrange — reschedule once so the scheduler creates and caches a sender for the queue.
            var message = CreateMessage(body, deliveryCount: 1);
            await Scheduler.RescheduleOrDeadLetterAsync(MessageActions.Object, message, QueueName, new InvalidOperationException("transient"), CancellationToken.None);

            // Act
            await Scheduler.DisposeAsync();

            // Assert
            Sender.Verify(s => s.DisposeAsync(), Times.Once);
        }
    }
}