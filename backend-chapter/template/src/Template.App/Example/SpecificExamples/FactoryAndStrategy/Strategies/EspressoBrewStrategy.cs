using Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Strategies;

public sealed class EspressoBrewStrategy : BrewStrategyBase
{
    public override BrewMethod Strategy => BrewMethod.Espresso;

    public override BrewedCoffee Brew(BrewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // High pressure, very short contact time produces a concentrated shot.
        return new BrewedCoffee("Concentrated espresso", request.WaterMillilitres, TimeSpan.FromSeconds(25));
    }
}