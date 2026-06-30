using Template.App.Example.SpecificExamples.BaseClass.Models;

namespace Template.App.Example.SpecificExamples.BaseClass;

// Template Method pattern: the shared roasting algorithm lives here. Every roast captures the same
// green beans and runs the same drying phase; each concrete profile overrides only the parts that
// differ — its peak temperature and how long development runs after first crack.
public abstract class RoastProfileBase
{
    public RoastedBatch Roast(GreenBeans beans)
    {
        ArgumentNullException.ThrowIfNull(beans);

        var dryingTime = CalculateDryingTime(beans);
        var developmentTime = ApplyDevelopmentPhase(beans);
        var totalRoastTime = dryingTime + developmentTime;

        return new RoastedBatch(beans.Origin, beans.Grams, RoastLevel, PeakTemperatureCelsius, totalRoastTime);
    }

    protected abstract string RoastLevel { get; }

    protected abstract int PeakTemperatureCelsius { get; }

    protected abstract TimeSpan ApplyDevelopmentPhase(GreenBeans beans);

    private static TimeSpan CalculateDryingTime(GreenBeans beans)
    {
        // Wetter beans need a longer drying phase before first crack — identical for every roast level.
        var dryingSeconds = 240 + (int)(beans.MoisturePercentage * 10);

        return TimeSpan.FromSeconds(dryingSeconds);
    }
}