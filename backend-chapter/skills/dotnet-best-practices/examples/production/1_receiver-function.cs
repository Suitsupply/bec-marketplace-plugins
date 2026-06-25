using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using {ServiceName}.Api.Mappers.v1.Interfaces;
using {ServiceName}.Api.Models.Foo.Transport.Requests;
using {ServiceName}.App.Extensions;
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.Api.Functions.Receivers;

public class FooReceiver(ILogger<FooReceiver> logger, IFooReceiverService fooReceiverService, IFooWebhookMapper fooWebhookMapper)
{
    [Function(nameof(FooReceiver))]
    [OpenApiOperation(nameof(FooReceiver), "Foo Receivers")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(FooCreatedRequest), Required = true)]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.Accepted)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "foo/created")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked.", nameof(FooReceiver));

        try
        {
            var rawJson = await request.Body.ReadStreamAsString();
            var requestDto = JsonSerializer.Deserialize<FooCreatedRequest>(rawJson);
            ArgumentNullException.ThrowIfNull(requestDto);

            var domain = fooWebhookMapper.ToDomain(requestDto);
            await fooReceiverService.ProcessAsync(domain, cancellationToken);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed.", nameof(FooReceiver));
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
