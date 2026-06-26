using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.App.Services;

public static class FooServiceTests
{
    public abstract class FooServiceTestsBase
    {
        protected readonly FooService Sut = new(new Mock<IFooDependency>().Object);
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : FooServiceTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }
}

// Static class (e.g. a mapper): pass the type — there is no instance, so no base class is needed.
public static class FooMapperTests
{
    public class NullArgumentChecks
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
            ArgumentsNullChecker.CheckStaticMethodParameters(typeof(FooMapper));
        }
    }
}
