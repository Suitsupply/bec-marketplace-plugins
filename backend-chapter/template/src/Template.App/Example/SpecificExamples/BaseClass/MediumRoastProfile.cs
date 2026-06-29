using Template.App.Example.SpecificExamples.BaseClass.Models;

namespace Template.App.Example.SpecificExamples.BaseClass;

public sealed class MediumRoastProfile : RoastProfileBase
{
    protected override string RoastLevel => "Medium";

    protected override int PeakTemperatureCelsius => 210;

    protected override TimeSpan ApplyDevelopmentPhase(GreenBeans beans)
    {
        ArgumentNullException.ThrowIfNull(beans);

        // A balanced development window rounds off the acidity without going oily.
        return TimeSpan.FromSeconds(90);
    }
}