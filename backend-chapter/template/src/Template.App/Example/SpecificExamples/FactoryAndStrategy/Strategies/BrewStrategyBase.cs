using Template.App.Example.SpecificExamples.FactoryAndStrategy.Interfaces;
using Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Strategies;

// Base for every brew strategy. It exposes the BrewMethod the strategy handles so the factory can
// select the right one, and forces each concrete strategy to implement its own brew behaviour.
public abstract class BrewStrategyBase : IBrewStrategy
{
    public abstract BrewMethod Strategy { get; }

    public abstract BrewedCoffee Brew(BrewRequest request);
}