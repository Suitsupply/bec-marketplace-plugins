using {ServiceName}.Api.Functions.Person;
using {ServiceName}.Api.Mappers.v1;
using {ServiceName}.UnitTests.Helpers;

namespace {ServiceName}.UnitTests.Api.Functions.Person;

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
            // FooMapper is static — not injected. The SUT calls FooMapper.ToDomain(...) directly.
            Sut = new FooReceiver(Dependency.Object);
        }
    }

    // Verifies all public method parameters are null-guarded via ArgumentsNullChecker.
    public class NullArgumentChecks : FooReceiverTestsBase
    {
        [Test]
        public void ShouldEnforceNullChecksOnMethods()
        {
            // Act & Assert
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
