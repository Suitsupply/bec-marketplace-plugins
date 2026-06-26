using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Template.Api.Example.Mappers.v1.Vehicles;
using Template.Api.Models.Example.v1.Vehicles.Responses;
using Template.App.Example.Services.Vehicles.Interfaces;

namespace Template.Api.Example.Functions.v1.Vehicles;

public sealed class GetVehicleFunction(ILogger<GetVehicleFunction> logger, IVehiclesService vehiclesService)
{
    [Function("GetVehicle")]
    [OpenApiOperation("GetVehicle", "Vehicle")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(int))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(GetVehicleResponse))]
    [OpenApiResponseWithoutBody(System.Net.HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetVehicleAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vehicles/{id:int}")] HttpRequest request,
        int id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("{Function} invoked for vehicle {VehicleId}.", nameof(GetVehicleAsync), id);

        try
        {
            var vehicle = await vehiclesService.GetVehicleAsync(id, cancellationToken);

            if (vehicle is null)
            {
                logger.LogWarning("Vehicle {VehicleId} not found.", id);
                return new NotFoundResult();
            }

            return new OkObjectResult(VehiclesMapper.ToDto(vehicle));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Function} failed for vehicle {VehicleId}.", nameof(GetVehicleAsync), id);
            return new ObjectResult("An unexpected error occurred while processing the request.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}