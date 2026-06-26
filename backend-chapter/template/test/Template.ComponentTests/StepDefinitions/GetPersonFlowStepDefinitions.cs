using System.Text.Json;
using Reqnroll;
using Template.Api.Models.Example.v1.Persons.Responses;
using Template.App.Models.Example.Models.Persons;
using Template.ComponentTests.Support;

namespace Template.ComponentTests.StepDefinitions;

[Binding]
public sealed class GetPersonFlowStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    private ApplicationFactory Factory => featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

    [Given(@"the SWAPI client returns a person for id (.*)")]
    public void GivenTheSwapiClientReturnsAPersonForId(int id)
    {
        var person = new Person(id, "Luke Skywalker", "172", "77");
        Factory.SwapiClient
            .Setup(c => c.GetPersonAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
    }

    [Given(@"the SWAPI client returns no person for id (.*)")]
    public void GivenTheSwapiClientReturnsNoPersonForId(int id)
    {
        Factory.SwapiClient
            .Setup(c => c.GetPersonAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);
    }

    [Given(@"the SWAPI client throws for id (.*)")]
    public void GivenTheSwapiClientThrowsForId(int id)
    {
        Factory.SwapiClient
            .Setup(c => c.GetPersonAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SWAPI error"));
    }

    [Then(@"the response body contains the person name ""(.*)""")]
    public async Task ThenTheResponseBodyContainsThePersonName(string expectedName)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(ScenarioContextKeys.Response);
        var json = await response.Content.ReadAsStringAsync();
        var personResponse = JsonSerializer.Deserialize<GetPersonResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.That(personResponse, Is.Not.Null);
        Assert.That(personResponse!.Name, Is.EqualTo(expectedName));
    }
}
