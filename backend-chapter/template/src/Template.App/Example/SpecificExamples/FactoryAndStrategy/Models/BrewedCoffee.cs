namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

public sealed record BrewedCoffee(
    string Description,
    int WaterMillilitres,
    TimeSpan SteepTime);