using Template.App.Example.SpecificExamples.Async.Models;

namespace Template.App.Example.SpecificExamples.Async.Interfaces;

public interface IMilkSteamer
{
    Task<SteamedMilk> SteamAsync(int millilitres, CancellationToken cancellationToken = default);
}