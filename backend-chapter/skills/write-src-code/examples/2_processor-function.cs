// Queue listener — Service Bus processor. Retry/dead-letter via IServiceBusRetryScheduler (Api/Messaging/Interfaces/).
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
            await processorService.ProcessAsync(message.Body.ToString(), cancellationToken);
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
            var rawJson = await request.Body.ReadStreamAsString();
            await processorService.ProcessAsync(rawJson, cancellationToken);
            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} debug failed.", nameof(FooProcessor));
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
