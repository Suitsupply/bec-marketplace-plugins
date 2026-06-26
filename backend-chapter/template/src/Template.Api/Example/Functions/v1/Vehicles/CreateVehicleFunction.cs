using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Template.Api.Example.Mappers.v1.Vehicles;
using Template.Api.Models.Example.v1.Vehicles.Requests;
using Template.App.Example.Services.Vehicles.Interfaces;
using Template.App.Extensions;

namespace Template.Api.Example.Functions.v1.Vehicles;

public sealed class CreateVehicleFunction(ILogger<CreateVehicleFunction> logger, IVehiclesService vehiclesService)
{
    [Function("CreateVehicle")]
    [OpenApiOperation("CreateVehicle", "Vehicle")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(CreateVehicleRequest), Required = true)]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.Accepted)]
    public async Task<IActionResult> CreateVehicleAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "vehicles")] HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked.", nameof(CreateVehicleAsync));

        try
        {
            var rawJson = await request.Body.ReadStreamAsStringAsync();
            var requestDto = JsonSerializer.Deserialize<CreateVehicleRequest>(rawJson);
            ArgumentNullException.ThrowIfNull(requestDto);

            var domain = VehiclesMapper.ToDomain(requestDto);
            await vehiclesService.CreateVehicleAsync(domain, cancellationToken);

            return new AcceptedResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed.", nameof(CreateVehicleAsync));
            return new ObjectResult("An unexpected error occurred while processing the request.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}