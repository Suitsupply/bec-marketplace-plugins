using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.Api.Functions.Receivers;

public static class FooReceiverTests
{
    public abstract class FooReceiverTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly Mock<IFooDependency> Dependency;
        protected readonly FooReceiver Sut;

        protected FooReceiverTestsBase()
        {
            Dependency = new Mock<IFooDependency>();
            Sut = new FooReceiver(Dependency.Object);
        }
    }

    public class NullArgumentChecks : FooReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    public class Run : FooReceiverTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnAccepted_WhenServiceSucceeds(string rawJson)
        {
            // Arrange
            var request = CreateRequest(rawJson);

            // Act
            var result = await Sut.Run(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
        }
    }
}
