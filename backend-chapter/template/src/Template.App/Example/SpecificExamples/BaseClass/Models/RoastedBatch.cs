namespace Template.App.Example.SpecificExamples.BaseClass.Models;

public sealed record RoastedBatch(
    string Origin,
    int Grams,
    string RoastLevel,
    int PeakTemperatureCelsius,
    TimeSpan TotalRoastTime);