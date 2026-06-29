using Template.App.Example.SpecificExamples.Async.Models;

namespace Template.App.Example.SpecificExamples.Async.Interfaces;

public interface ISalesLogger
{
    Task RecordAsync(PreparedDrink drink, CancellationToken cancellationToken = default);
}