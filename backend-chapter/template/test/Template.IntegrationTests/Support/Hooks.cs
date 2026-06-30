using Reqnroll;

namespace Template.IntegrationTests.Support;

[Binding]
public sealed class Hooks(FeatureContext featureContext, ScenarioContext scenarioContext)
{
    internal const string SettingsKey = "IntegrationTestSettings";
    internal const string HttpClientKey = "HttpClient";
    internal const string ResponseKey = "Response";

    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        featureContext.Set(IntegrationTestSettings.Load(), SettingsKey);
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var settings = featureContext.Get<IntegrationTestSettings>(SettingsKey);
        scenarioContext.Set(new HttpClient { BaseAddress = settings.FunctionsHostUrl }, HttpClientKey);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (scenarioContext.TryGetValue(HttpClientKey, out HttpClient client))
        {
            client.Dispose();
        }
    }
}
