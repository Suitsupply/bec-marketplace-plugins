using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.App.Services;

public static class FooServiceTests
{
    public abstract class FooServiceTestsBase
    {
        protected readonly FooService Sut = new(Mock.Of<IFooDependency>().Object);
    }

    public class NullArgumentChecks : FooServiceTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }
}
