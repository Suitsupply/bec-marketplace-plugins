using Template.App.Example.SpecificExamples.FactoryAndStrategy.Interfaces;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Factories;

// Receives every registered strategy via DI and returns the one whose Strategy matches the request.
// Register the strategies in Program.cs / ServiceCollectionExtensions, e.g.:
//   services.AddTransient<IBrewStrategy, EspressoBrewStrategy>();
//   services.AddTransient<IBrewStrategy, FrenchPressBrewStrategy>();
//   services.AddTransient<IBrewStrategy, ColdBrewStrategy>();
//   services.AddSingleton<IBrewStrategyFactory, BrewStrategyFactory>();
public sealed class BrewStrategyFactory(IEnumerable<IBrewStrategy> strategies) : IBrewStrategyFactory
{
    public IBrewStrategy Resolve(BrewMethod method)
    {
        var matches = strategies
            .Where(strategy => strategy.Strategy == method)
            .ToList();

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"No brew strategy is registered for method '{method}'.");
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException($"More than one brew strategy is registered for method '{method}'.");
        }

        return matches[0];
    }
}