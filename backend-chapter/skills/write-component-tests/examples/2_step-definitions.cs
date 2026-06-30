using System.Text;
using Reqnroll;
using {ServiceName}.ComponentTests.Support;

namespace {ServiceName}.ComponentTests.StepDefinitions;

[Binding]
public sealed class FooProcessorFlowStepDefinitions(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    private ApplicationFactory Factory => featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);

    [Given(@"the outbound publisher returns message id ""(.*)""")]
    public void GivenPublisherReturnsMessageId(string messageId)
    {
        Factory.OutboundPublisher
            .Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageId);
    }

    [When(@"the foo created processor receives order (.*)")]
    public async Task WhenProcessorReceivesOrder(long orderId)
    {
        var client = scenarioContext.Get<HttpClient>(ScenarioContextKeys.HttpClient);
        var response = await client.PostAsync($"/api/foo/created/process/debug", BuildPayload(orderId));
        scenarioContext.Set(response, ScenarioContextKeys.Response);
    }

    [Then(@"the response status code should be (\d+)")]
    public void ThenResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        var response = scenarioContext.Get<HttpResponseMessage>(ScenarioContextKeys.Response);
        Assert.That((int)response.StatusCode, Is.EqualTo(expectedStatusCode));
    }

    private static StringContent BuildPayload(long orderId) =>
        new($@"{{""id"":{orderId}}}", Encoding.UTF8, "application/json");
}
