namespace Template.UnitTests.Helpers;

public static class FixtureFactory
{
    public static Fixture Create()
    {
        var fixture = new Fixture();

        // Domain shaping lives here as AutoFixture customizations so every test shares the same rules.
        // Demonstration: force every generated double to a fixed value (e.g. a fixed ratio/exchange rate).
        // Customizations apply to Fixture.Create<T>()/Build<T>() — not to [AutoData], which uses its own fixture.
        fixture.Register(() => 1.333d);

        return fixture;
    }
}