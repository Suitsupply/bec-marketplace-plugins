using System.Text.Json;
using Reqnroll;
using Template.IntegrationTests.Support;

namespace Template.IntegrationTests.StepDefinitions;

[Binding]
public sealed class GetPersonStepDefinitions(FeatureContext featureContext, ScenarioContext scenarioContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private IntegrationTestSettings Settings => featureContext.Get<IntegrationTestSettings>(Hooks.SettingsKey);
    private HttpClient HttpClient => scenarioContext.Get<HttpClient>(Hooks.HttpClientKey);

    [When(@"I send a GET request to the person endpoint for id (\d+)")]
    public async Task WhenISendAGetRequestToThePersonEndpointForId(int id)
    {
        var route = $"/api/person/{id}";
        if (!string.IsNullOrWhiteSpace(Settings.FunctionsCode))
        {
            route += $"?code={Settings.FunctionsCode}";
        }

        var response = await HttpClient.GetAsync(route);
        scenarioContext.Set(response, Hooks.ResponseKey);
    }

    [Then(@"the person name should be ""(.*)""")]
    public async Task ThenThePersonNameShouldBe(string expectedName)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(Hooks.ResponseKey);
        var body = await response.Content.ReadAsStringAsync();
        var person = JsonSerializer.Deserialize<PersonResponse>(body, JsonOptions);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo(expectedName));
    }

    private sealed record PersonResponse(string Name);
}
