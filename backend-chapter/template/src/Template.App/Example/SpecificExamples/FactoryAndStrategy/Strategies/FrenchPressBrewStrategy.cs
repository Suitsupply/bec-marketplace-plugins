using Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Strategies;

public sealed class FrenchPressBrewStrategy : BrewStrategyBase
{
    public override BrewMethod Strategy => BrewMethod.FrenchPress;

    public override BrewedCoffee Brew(BrewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Full immersion: steep the grounds for a few minutes before pressing.
        return new BrewedCoffee("Full-bodied French press", request.WaterMillilitres, TimeSpan.FromMinutes(4));
    }
}