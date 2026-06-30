using Reqnroll;

namespace Template.ComponentTests.Support;

[Binding]
public sealed class Hooks(ScenarioContext scenarioContext, FeatureContext featureContext)
{
    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        featureContext.Set(new ApplicationFactory(), ScenarioContextKeys.Factory);
    }

    [AfterFeature]
    public static void AfterFeature(FeatureContext featureContext)
    {
        if (featureContext.TryGetValue(ScenarioContextKeys.Factory, out ApplicationFactory factory))
        {
            factory.Dispose();
        }
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var factory = featureContext.Get<ApplicationFactory>(ScenarioContextKeys.Factory);
        factory.SwapiClient.Reset();
        factory.RetryScheduler.Reset();
        scenarioContext.Set(factory.CreateClient(), ScenarioContextKeys.HttpClient);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (scenarioContext.TryGetValue(ScenarioContextKeys.HttpClient, out HttpClient client))
        {
            client.Dispose();
        }
    }
}
