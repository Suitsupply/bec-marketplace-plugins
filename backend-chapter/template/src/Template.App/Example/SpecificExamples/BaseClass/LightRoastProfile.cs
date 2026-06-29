using Template.App.Example.SpecificExamples.BaseClass.Models;

namespace Template.App.Example.SpecificExamples.BaseClass;

public sealed class LightRoastProfile : RoastProfileBase
{
    protected override string RoastLevel => "Light";

    protected override int PeakTemperatureCelsius => 196;

    protected override TimeSpan ApplyDevelopmentPhase(GreenBeans beans)
    {
        ArgumentNullException.ThrowIfNull(beans);

        // Dropped right after first crack — minimal development keeps the acidity bright.
        return TimeSpan.FromSeconds(45);
    }
}