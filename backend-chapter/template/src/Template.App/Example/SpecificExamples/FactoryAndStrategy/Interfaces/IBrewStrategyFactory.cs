namespace Template.App.Example.SpecificExamples.FactoryAndStrategy.Interfaces;

public interface IBrewStrategyFactory
{
    IBrewStrategy Resolve(BrewMethod method);
}