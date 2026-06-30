namespace Template.App.Example.SpecificExamples.Async.Models;

public sealed record PreparedDrink(
    string CustomerName,
    string DrinkName,
    TimeSpan PreparationTime);