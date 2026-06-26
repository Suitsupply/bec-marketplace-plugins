using System.Text.Json;

namespace Template.IntegrationTests.Support;

/// <summary>
/// Runtime settings for integration tests.
/// Resolution order (first wins):
///   1. Environment variables — set by the selected .runsettings file or the CI pipeline variable group.
///   2. <c>integrationtests.local.json</c> — a gitignored file next to the .csproj for personal overrides/secrets.
///   3. <c>integrationtests.json</c> — committed defaults (localhost) used by default when running in Visual Studio.
/// </summary>
internal sealed record IntegrationTestSettings(Uri FunctionsHostUrl, string? FunctionsCode)
{
    private const string FunctionsHostUrlVar = "FUNCTIONS_HOST_URL";
    private const string FunctionsCodeVar = "FUNCTIONS_CODE";
    private const string LocalFileName = "integrationtests.local.json";
    private const string DefaultFileName = "integrationtests.json";

    internal static IntegrationTestSettings Load()
    {
        var fileValues = LoadFileValues();

        var functionsHostUrl = new Uri(Resolve(FunctionsHostUrlVar, fileValues));
        var functionsCode = TryResolve(FunctionsCodeVar, fileValues);

        return new IntegrationTestSettings(functionsHostUrl, functionsCode);
    }

    private static string Resolve(string name, IReadOnlyDictionary<string, string> fileValues)
    {
        var value = TryResolve(name, fileValues);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"'{name}' is not set. " +
                $"For CI: add it to the pipeline variable group. " +
                $"For local development: set it in '{LocalFileName}' or '{DefaultFileName}' next to the .csproj.");
        }

        return value;
    }

    private static string? TryResolve(string name, IReadOnlyDictionary<string, string> fileValues)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fileValues.TryGetValue(name, out var fileValue) ? fileValue : null;
    }

    private static IReadOnlyDictionary<string, string> LoadFileValues()
    {
        // Lowest precedence first (committed defaults), then personal local overrides on top.
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in new[] { DefaultFileName, LocalFileName })
        {
            var fileValues = TryLoadFile(fileName);
            if (fileValues is null)
            {
                continue;
            }

            foreach (var (key, value) in fileValues)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static IReadOnlyDictionary<string, string>? TryLoadFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
            {
                var json = File.ReadAllText(candidate);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }

            dir = dir.Parent;
        }

        return null;
    }
}
