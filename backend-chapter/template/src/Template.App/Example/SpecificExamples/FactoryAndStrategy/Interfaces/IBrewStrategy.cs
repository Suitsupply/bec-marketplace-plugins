using Template.App.Example.SpecificExamples.FactoryAndStrategy.Models;

namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Interfaces;

public interface IBrewStrategy
{
    BrewMethod Strategy { get; }

    BrewedCoffee Brew(BrewRequest request);
}