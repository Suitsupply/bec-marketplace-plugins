using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using {ServiceName}.App.Extensions;
using {ServiceName}.App.Services.Receivers.Interfaces;

namespace {ServiceName}.Api.Functions.Receivers;

public class FooCreatedReceiver(ILogger<FooCreatedReceiver> logger, IFooCreatedReceiverService fooCreatedReceiverService)
{
    [Function(nameof(FooCreatedReceiver))]
    [OpenApiOperation(nameof(FooCreatedReceiver), "Foo Receivers")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(string), Required = true)]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.Accepted)]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "foo/created")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked.", nameof(FooCreatedReceiver));

        try
        {
            var rawJson = await request.Body.ReadStreamAsString();
            await fooCreatedReceiverService.ProcessAsync(rawJson, cancellationToken);
            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed.", nameof(FooCreatedReceiver));
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
