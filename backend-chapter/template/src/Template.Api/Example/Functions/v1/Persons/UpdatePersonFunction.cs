using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Template.Api.Example.Mappers.v1.Persons;
using Template.Api.Messaging;
using Template.Api.Messaging.Interfaces;
using Template.Api.Models.Example.v1.Persons.Requests;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Extensions;
using Template.Infra.Example.Settings;

namespace Template.Api.Example.Functions.v1.Persons;

public sealed class UpdatePersonFunction(ILogger<UpdatePersonFunction> logger, IPersonsService personService, IServiceBusRetryScheduler retryScheduler, IOptions<ServiceBusOptions> serviceBusOptions)
{
    [Function("UpdatePersonMessage")]
    public async Task UpdatePersonMessageAsync(
        [ServiceBusTrigger("%ServiceBusOptions:StoreServiceBus:UpdatePersonQueueName%", Connection = "ServiceBusOptions:StoreServiceBus")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageActions);

        logger.LogInformation("{Function} message {MessageId} received.", nameof(UpdatePersonMessageAsync), message.MessageId);

        try
        {
            var requestDto = JsonSerializer.Deserialize<UpdatePersonRequest>(message.Body.ToString());
            ArgumentNullException.ThrowIfNull(requestDto);

            var domain = PersonsMapper.ToDomain(requestDto);
            await personService.UpdatePersonAsync(domain, cancellationToken);

            await messageActions.CompleteMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            var outcome = await retryScheduler.RescheduleOrDeadLetterAsync(messageActions, message, serviceBusOptions.Value.StoreServiceBus.UpdatePersonQueueName, ex, cancellationToken);

            if (outcome == RetryOutcome.DeadLettered)
                logger.LogError(ex, "UpdatePerson message {MessageId} dead-lettered.", message.MessageId);
            else
                logger.LogWarning(ex, "UpdatePerson processing failed for {MessageId}, rescheduled for retry.", message.MessageId);
        }
    }

    [Function("UpdatePersonMessageDebug")]
    [OpenApiOperation("UpdatePerson", "Person")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(UpdatePersonRequest), Required = true)]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.Accepted)]
    public async Task<IActionResult> UpdatePersonMessageDebugAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "person/update/debug")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogWarning("{Function} debug route invoked.", nameof(UpdatePersonMessageDebugAsync));

        try
        {
            var rawJson = await request.Body.ReadStreamAsStringAsync();
            var requestDto = JsonSerializer.Deserialize<UpdatePersonRequest>(rawJson);
            ArgumentNullException.ThrowIfNull(requestDto);

            var domain = PersonsMapper.ToDomain(requestDto);
            await personService.UpdatePersonAsync(domain, cancellationToken);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} debug route failed.", nameof(UpdatePersonMessageDebugAsync));
            return new ObjectResult("An unexpected error occurred while processing the request.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}