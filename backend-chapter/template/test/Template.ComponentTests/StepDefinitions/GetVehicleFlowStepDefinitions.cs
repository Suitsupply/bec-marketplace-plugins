using System.Text.Json;
using Reqnroll;
using Template.Api.Models.Example.v1.Vehicles.Responses;
using Template.App.Models.Example.Models.Vehicles;
using Template.ComponentTests.Support;

namespace Template.ComponentTests.StepDefinitions;

[Binding]
public sealed class GetVehicleFlowStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    private ApplicationFactory Factory => featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

    [Given(@"the SWAPI client returns a vehicle for id (.*)")]
    public void GivenTheSwapiClientReturnsAVehicleForId(int id)
    {
        var vehicle = new Vehicle(id, "Sand Crawler", "Digger Crawler", "Corellia Mining Corporation");
        Factory.SwapiClient
            .Setup(c => c.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
    }

    [Given(@"the SWAPI client returns no vehicle for id (.*)")]
    public void GivenTheSwapiClientReturnsNoVehicleForId(int id)
    {
        Factory.SwapiClient
            .Setup(c => c.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);
    }

    [Given(@"the SWAPI client throws for vehicle id (.*)")]
    public void GivenTheSwapiClientThrowsForVehicleId(int id)
    {
        Factory.SwapiClient
            .Setup(c => c.GetVehicleAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SWAPI error"));
    }

    [Then(@"the response body contains the vehicle name ""(.*)""")]
    public async Task ThenTheResponseBodyContainsTheVehicleName(string expectedName)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(ScenarioContextKeys.Response);
        var json = await response.Content.ReadAsStringAsync();
        var vehicleResponse = JsonSerializer.Deserialize<GetVehicleResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.That(vehicleResponse, Is.Not.Null);
        Assert.That(vehicleResponse!.Name, Is.EqualTo(expectedName));
    }
}
