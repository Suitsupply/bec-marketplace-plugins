namespace Template.App.Example.SpecificExamples.Async.Models;

public sealed record DrinkOrder(
    string CustomerName,
    string DrinkName,
    int ShotCount,
    int MilkMillilitres);