using Template.App.Models.Example.Models.Persons;

namespace Template.UnitTests.Example;

// Demonstration only: shows the FixtureFactory.Create() path for the rare test that needs the
// shared AutoFixture customizations or explicit Build<T>().With(...) shaping.
// Prefer [Test, AutoData] for everything else — [AutoData] builds its own fixture and does NOT
// apply the customizations registered in FixtureFactory.
public static class FixtureCustomizationExampleTests
{
    // Stand-in type with a double, used purely to demonstrate the customization registered in
    // FixtureFactory (every generated double becomes the fixed 1.333 value).
    private sealed record ExchangeRateSample(string Currency, double Rate);

    public abstract class FixtureCustomizationExampleTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
    }

    public class Create : FixtureCustomizationExampleTestsBase
    {
        [Test]
        public void ShouldApplyDoubleCustomization_WhenBuildingTypeWithDouble()
        {
            // Act
            var sample = Fixture.Create<ExchangeRateSample>();

            // Assert
            Assert.That(sample.Rate, Is.EqualTo(1.333d));
        }

        [Test]
        public void ShouldOverrideOnlyTheSpecifiedProperty_WhenUsingBuildWith()
        {
            // Arrange
            const string expectedName = "Luke Skywalker";

            // Act
            var person = Fixture.Build<Person>().With(p => p.Name, expectedName).Create();

            // Assert
            Assert.That(person.Name, Is.EqualTo(expectedName));
            Assert.That(person.Id, Is.Not.Zero);
        }
    }
}
