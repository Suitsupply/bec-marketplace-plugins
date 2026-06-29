using Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Strategies;

public sealed class ColdBrewStrategy : BrewStrategyBase
{
    public override BrewMethod Strategy => BrewMethod.ColdBrew;

    public override BrewedCoffee Brew(BrewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Cold water extraction needs a very long steep, typically overnight.
        return new BrewedCoffee("Smooth cold brew", request.WaterMillilitres, TimeSpan.FromHours(12));
    }
}