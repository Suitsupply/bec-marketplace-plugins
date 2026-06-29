using Template.App.Example.SpecificExamples.Async.Models;

namespace Template.App.Example.SpecificExamples.Async.Interfaces;

public interface IReceiptPrinter
{
    Task PrintAsync(PreparedDrink drink, CancellationToken cancellationToken = default);
}