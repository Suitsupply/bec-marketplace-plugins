using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.Api.Example.Functions.v1.Persons;
using Template.Api.Messaging;
using Template.Api.Messaging.Interfaces;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Models.Example.Models.Persons;
using Template.Infra.Example.Settings;

namespace Template.UnitTests.Api.Example.Functions.v1.Persons;

public static class UpdatePersonFunctionTests
{
    public abstract class UpdatePersonFunctionTestsBase
    {
        protected readonly Mock<ILogger<UpdatePersonFunction>> Logger;
        protected readonly Mock<IPersonsService> PersonService;
        protected readonly Mock<IServiceBusRetryScheduler> RetryScheduler;
        protected readonly UpdatePersonFunction Sut;

        protected UpdatePersonFunctionTestsBase()
        {
            Logger = new Mock<ILogger<UpdatePersonFunction>>();
            PersonService = new Mock<IPersonsService>();
            RetryScheduler = new Mock<IServiceBusRetryScheduler>();
            var serviceBusOptions = Options.Create(new ServiceBusOptions
            {
                StoreServiceBus = new StoreServiceBusOptions
                {
                    FullyQualifiedNamespace = "test.servicebus.windows.net",
                    UpdatePersonQueueName = "update-person",
                }
            });

            Sut = new UpdatePersonFunction(Logger.Object, PersonService.Object, RetryScheduler.Object, serviceBusOptions);
        }

        protected static HttpRequest CreateRequest(string body)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            return request.Object;
        }

        protected static ServiceBusReceivedMessage CreateMessage(int id) =>
            ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(Encoding.UTF8.GetBytes($"{{\"id\":{id}}}")));
    }

    public class NullArgumentChecks : UpdatePersonFunctionTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class UpdatePersonMessageAsync : UpdatePersonFunctionTestsBase
    {
        protected readonly Mock<ServiceBusMessageActions> MessageActions = new();

        [Test, AutoData]
        public async Task ShouldCompleteMessage_WhenProcessingSucceeds(int id, Person person)
        {
            // Arrange
            var message = CreateMessage(id);
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.Is<UpdatePerson>(m => m.Id == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            await Sut.UpdatePersonMessageAsync(message, MessageActions.Object, CancellationToken.None);

            // Assert
            PersonService.Verify(s => s.UpdatePersonAsync(It.Is<UpdatePerson>(m => m.Id == id), It.IsAny<CancellationToken>()), Times.Once);
            MessageActions.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
            RetryScheduler.Verify(s => s.RescheduleOrDeadLetterAsync(It.IsAny<ServiceBusMessageActions>(), It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, AutoData]
        public async Task ShouldCallRetryScheduler_WhenProcessingThrows(int id)
        {
            // Arrange
            var message = CreateMessage(id);
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("transient"));

            // Act
            await Sut.UpdatePersonMessageAsync(message, MessageActions.Object, CancellationToken.None);

            // Assert
            RetryScheduler.Verify(s => s.RescheduleOrDeadLetterAsync(MessageActions.Object, message, "update-person", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldLogWarning_WhenSchedulerReturnsRescheduled(int id)
        {
            // Arrange
            var message = CreateMessage(id);
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("transient"));
            RetryScheduler
                .Setup(s => s.RescheduleOrDeadLetterAsync(It.IsAny<ServiceBusMessageActions>(), It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RetryOutcome.Rescheduled);

            // Act
            await Sut.UpdatePersonMessageAsync(message, MessageActions.Object, CancellationToken.None);

            // Assert
            Logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldLogError_WhenSchedulerReturnsDeadLettered(int id)
        {
            // Arrange
            var message = CreateMessage(id);
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fatal"));
            RetryScheduler
                .Setup(s => s.RescheduleOrDeadLetterAsync(It.IsAny<ServiceBusMessageActions>(), It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RetryOutcome.DeadLettered);

            // Act
            await Sut.UpdatePersonMessageAsync(message, MessageActions.Object, CancellationToken.None);

            // Assert
            Logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }

    public class UpdatePersonMessageDebugAsync : UpdatePersonFunctionTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnAccepted_WhenProcessingSucceeds(int id, Person person)
        {
            // Arrange
            var request = CreateRequest($"{{\"id\":{id}}}");
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.Is<UpdatePerson>(m => m.Id == id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            var result = await Sut.UpdatePersonMessageDebugAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
        }

        [Test, AutoData]
        public async Task ShouldReturn500_WhenProcessingThrows(int id)
        {
            // Arrange
            var request = CreateRequest($"{{\"id\":{id}}}");
            PersonService
                .Setup(s => s.UpdatePersonAsync(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            // Act
            var result = await Sut.UpdatePersonMessageDebugAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }
    }
}