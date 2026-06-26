using Reqnroll;
using Template.IntegrationTests.Support;

namespace Template.IntegrationTests.StepDefinitions;

[Binding]
public sealed class EndpointAuthenticationStepDefinitions(ScenarioContext scenarioContext)
{
    private HttpClient HttpClient => scenarioContext.Get<HttpClient>(Hooks.HttpClientKey);

    [When(@"I send a (GET|POST) request to ""(.*)""")]
    public async Task WhenISendARequestTo(string method, string route)
    {
        var response = method == "POST"
            ? await HttpClient.PostAsync(route, content: null)
            : await HttpClient.GetAsync(route);

        scenarioContext.Set(response, Hooks.ResponseKey);
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedCode)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(Hooks.ResponseKey);
        Assert.That((int)response.StatusCode, Is.EqualTo(expectedCode));
    }
}
