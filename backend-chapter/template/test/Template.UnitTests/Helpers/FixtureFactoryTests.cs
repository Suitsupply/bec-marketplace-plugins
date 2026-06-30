namespace Template.UnitTests.Helpers;

public static class FixtureFactoryTests
{
    public abstract class FixtureFactoryTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
    }

    public class Create : FixtureFactoryTestsBase
    {
        [Test]
        public void ShouldApplyDoubleCustomization_WhenCreatingDouble()
        {
            // Act
            var value = Fixture.Create<double>();

            // Assert
            Assert.That(value, Is.EqualTo(1.333d));
        }
    }
}