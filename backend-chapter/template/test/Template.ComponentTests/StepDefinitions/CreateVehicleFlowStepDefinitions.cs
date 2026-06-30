using Reqnroll;
using Template.App.Models.Example.Models.Vehicles;
using Template.ComponentTests.Support;

namespace Template.ComponentTests.StepDefinitions;

[Binding]
public sealed class CreateVehicleFlowStepDefinitions(FeatureContext featureContext)
{
    private ApplicationFactory Factory => featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

    [Given(@"the SWAPI client accepts create vehicle requests")]
    public void GivenTheSwapiClientAcceptsCreateVehicleRequests()
    {
        Factory.SwapiClient
            .Setup(c => c.CreateVehicleAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Given(@"the SWAPI client throws when creating a vehicle")]
    public void GivenTheSwapiClientThrowsWhenCreatingAVehicle()
    {
        Factory.SwapiClient
            .Setup(c => c.CreateVehicleAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SWAPI error"));
    }
}
