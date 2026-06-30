using System.Text;
using Reqnroll;
using Template.ComponentTests.Support;

namespace Template.ComponentTests.StepDefinitions;

[Binding]
public sealed class CommonStepDefinitions(ScenarioContext scenarioContext)
{
    [Given(@"the request body is '(.*)'")]
    public void GivenTheRequestBodyIs(string payload)
    {
        scenarioContext.Set(payload, ScenarioContextKeys.RequestBody);
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string route)
    {
        var client = scenarioContext.Get<HttpClient>(ScenarioContextKeys.HttpClient);
        var response = await client.GetAsync(route);
        scenarioContext.Set(response, ScenarioContextKeys.Response);
    }

    [When(@"I send a POST request to ""(.*)""")]
    public async Task WhenISendAPostRequestTo(string route)
    {
        var client = scenarioContext.Get<HttpClient>(ScenarioContextKeys.HttpClient);
        var body = scenarioContext.Get<string>(ScenarioContextKeys.RequestBody);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(route, content);
        scenarioContext.Set(response, ScenarioContextKeys.Response);
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedCode)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(ScenarioContextKeys.Response);
        Assert.That((int)response.StatusCode, Is.EqualTo(expectedCode));
    }
}
