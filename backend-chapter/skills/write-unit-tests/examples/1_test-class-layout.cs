using {ServiceName}.Api.Functions.Receivers;
using {ServiceName}.Api.Mappers.v1;
using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.Api.Functions.Receivers;

public static class FooReceiverTests
{
    public abstract class FooReceiverTestsBase
    {
        protected readonly Fixture Fixture = FixtureFactory.Create();
        protected readonly Mock<IFooDependency> Dependency;
        protected readonly FooWebhookMapper WebhookMapper = new();
        protected readonly FooReceiver Sut;

        protected FooReceiverTestsBase()
        {
            Dependency = new Mock<IFooDependency>();
            Sut = new FooReceiver(Dependency.Object, WebhookMapper);
        }
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : FooReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            ArgumentsNullChecker.CheckMethodParameters(Sut);
        }
    }

    // One derived class per public method — name the class after the method ({MethodName})
    public class ProcessWebhookAsync : FooReceiverTestsBase
    {
        [Test, AutoData]
        public async Task ShouldReturnAccepted_WhenServiceSucceeds(string rawJson)
        {
            // Arrange
            var request = CreateRequest(rawJson);

            // Act
            var result = await Sut.ProcessWebhookAsync(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<AcceptedResult>());
        }
    }
}
