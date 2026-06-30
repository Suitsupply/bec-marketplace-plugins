using Template.App.Example.SpecificExamples.BaseClass.Models;

namespace Template.App.Example.SpecificExamples.BaseClass;

public sealed class DarkRoastProfile : RoastProfileBase
{
    protected override string RoastLevel => "Dark";

    protected override int PeakTemperatureCelsius => 225;

    protected override TimeSpan ApplyDevelopmentPhase(GreenBeans beans)
    {
        ArgumentNullException.ThrowIfNull(beans);

        // A long, hot development phase pushes the beans to second crack for bold, smoky notes.
        return TimeSpan.FromSeconds(150);
    }
}