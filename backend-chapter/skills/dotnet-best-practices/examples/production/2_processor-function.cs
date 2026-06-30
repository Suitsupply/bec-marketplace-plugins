using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common.ServiceBusRetryScheduler;
using Common.ServiceBusRetryScheduler.Interfaces;
using {ServiceName}.Api.Mappers.v1;
using {ServiceName}.Api.Models.v1.Foo.Requests;
using {ServiceName}.App.Extensions;
using {ServiceName}.App.Models.Foo.Models.Webhooks;
using {ServiceName}.App.Services.Foo.Interfaces;

namespace {ServiceName}.Api.Functions.Foo;

// Queue listener — Service Bus processor. Retry/dead-letter via IServiceBusRetryScheduler (Suitsupply.Common.ServiceBusRetryScheduler).
// Entry log at Function: MessageId. Entry log with OrderId in App processor service (processor-service.cs).

public class FooProcessor(
    ILogger<FooProcessor> logger,
    IFooProcessorService processorService,
    IServiceBusRetryScheduler retryScheduler,
    IOptions<ServiceBusOptions> serviceBusOptions)
{
    [Function(nameof(FooProcessor))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusOptions:StoreServiceBus:FooQueueName%", Connection = "ServiceBusOptions:StoreServiceBus")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{Function} message {MessageId} received.", nameof(FooProcessor), message.MessageId);

        try
        {
            var domain = JsonSerializer.Deserialize<FooCreatedWebhook>(message.Body.ToString());
            ArgumentNullException.ThrowIfNull(domain);

            await processorService.ProcessAsync(domain, cancellationToken);
            await messageActions.CompleteMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            var outcome = await retryScheduler.RescheduleOrDeadLetterAsync(messageActions, message, serviceBusOptions.Value.StoreServiceBus.FooQueueName, ex, cancellationToken);

            if (outcome == RetryOutcome.DeadLettered)
                logger.LogError(ex, "{Function} message {MessageId} dead-lettered.", nameof(FooProcessor), message.MessageId);
            else
                logger.LogWarning(ex, "{Function} message {MessageId} rescheduled for retry.", nameof(FooProcessor), message.MessageId);
        }
    }

    [Function($"{nameof(FooProcessor)}_Debug")]
    public async Task<IActionResult> Run_Debug(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "foo/process/debug")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.LogWarning("{Function} debug invoked.", nameof(FooProcessor));

        try
        {
            var rawJson = await request.Body.ReadStreamAsStringAsync();
            var requestDto = JsonSerializer.Deserialize<FooCreatedRequest>(rawJson);
            ArgumentNullException.ThrowIfNull(requestDto);

            var domain = FooMapper.ToDomain(requestDto);
            await processorService.ProcessAsync(domain, cancellationToken);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} debug failed.", nameof(FooProcessor));
            return new ObjectResult("An unexpected error occurred while processing the request.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
