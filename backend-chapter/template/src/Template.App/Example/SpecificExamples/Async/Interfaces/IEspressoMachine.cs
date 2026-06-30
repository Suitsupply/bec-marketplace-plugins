using Template.App.Example.SpecificExamples.Async.Models;

namespace Template.App.Example.SpecificExamples.Async.Interfaces;

public interface IEspressoMachine
{
    Task<EspressoShot> PullShotsAsync(int shotCount, CancellationToken cancellationToken = default);
}