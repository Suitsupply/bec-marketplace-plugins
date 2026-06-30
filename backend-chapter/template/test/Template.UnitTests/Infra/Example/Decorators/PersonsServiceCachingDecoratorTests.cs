using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Models.Example.Models.Persons;
using Template.Infra.Example.Decorators;
using Template.Infra.Example.Settings;

namespace Template.UnitTests.Infra.Example.Decorators;

public static class PersonsServiceCachingDecoratorTests
{
    public abstract class PersonsServiceCachingDecoratorTestsBase
    {
        protected readonly Mock<IPersonsService> Inner;
        protected readonly MemoryCache Cache;
        protected readonly PersonsServiceCachingDecorator Sut;

        protected PersonsServiceCachingDecoratorTestsBase()
        {
            Inner = new Mock<IPersonsService>();
            Cache = new MemoryCache(new MemoryCacheOptions());
            Sut = new PersonsServiceCachingDecorator(Inner.Object, Cache, Options.Create(new CacheSettings()));
        }
    }

    public class GetPersonAsync : PersonsServiceCachingDecoratorTestsBase
    {
        [Test, AutoData]
        public async Task ShouldCacheResult_WhenCalledTwiceWithSameId(int id, Person person)
        {
            // Arrange
            Inner
                .Setup(s => s.GetPersonAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            var first = await Sut.GetPersonAsync(id, CancellationToken.None);
            var second = await Sut.GetPersonAsync(id, CancellationToken.None);

            // Assert
            Assert.That(first, Is.EqualTo(person));
            Assert.That(second, Is.EqualTo(person));
            Inner.Verify(s => s.GetPersonAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, AutoData]
        public async Task ShouldCallInnerForEachId_WhenIdsDiffer(int id1, int id2, Person person1, Person person2)
        {
            // Arrange
            Assume.That(id1, Is.Not.EqualTo(id2));
            Inner
                .Setup(s => s.GetPersonAsync(id1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person1);
            Inner
                .Setup(s => s.GetPersonAsync(id2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person2);

            // Act
            var result1 = await Sut.GetPersonAsync(id1, CancellationToken.None);
            var result2 = await Sut.GetPersonAsync(id2, CancellationToken.None);

            // Assert
            Assert.That(result1, Is.EqualTo(person1));
            Assert.That(result2, Is.EqualTo(person2));
            Inner.Verify(s => s.GetPersonAsync(id1, It.IsAny<CancellationToken>()), Times.Once);
            Inner.Verify(s => s.GetPersonAsync(id2, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class UpdatePersonAsync : PersonsServiceCachingDecoratorTestsBase
    {
        [Test, AutoData]
        public async Task ShouldDelegateToInner_WhenCalled(UpdatePerson message, Person person)
        {
            // Arrange
            Inner
                .Setup(s => s.UpdatePersonAsync(message, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            // Act
            var result = await Sut.UpdatePersonAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(person));
            Inner.Verify(s => s.UpdatePersonAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}