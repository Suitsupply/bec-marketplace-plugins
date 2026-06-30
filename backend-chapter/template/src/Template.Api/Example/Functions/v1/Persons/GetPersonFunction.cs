using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Template.Api.Example.Mappers.v1.Persons;
using Template.Api.Models.Example.v1.Persons.Responses;
using Template.App.Example.Services.Persons.Interfaces;

namespace Template.Api.Example.Functions.v1.Persons;

public sealed class GetPersonFunction(ILogger<GetPersonFunction> logger, IPersonsService personService)
{
    [Function("GetPerson")]
    [OpenApiOperation("GetPerson", "Person")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(GetPersonResponse))]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetPersonAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/{id:int}")] HttpRequest request,
        int id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked for person {PersonId}.", nameof(GetPersonAsync), id);

        try
        {
            var person = await personService.GetPersonAsync(id, cancellationToken);

            if (person is null)
            {
                logger.LogWarning("Person {PersonId} not found.", id);
                return new NotFoundResult();
            }

            return new OkObjectResult(PersonsMapper.ToDto(person));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed for person {PersonId}.", nameof(GetPersonAsync), id);
            return new ObjectResult("An unexpected error occurred while processing the request.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}