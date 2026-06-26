using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Template.App.Example.Services.Persons.Interfaces;
using Template.App.Models.Example.Models.Persons;
using Template.Infra.Example.Settings;

namespace Template.Infra.Example.Decorators;

// NOTE: This is a simple caching decorator for demonstration purposes. In a real-world application, consider using a more robust caching strategy, such as distributed caching or a dedicated caching library.
public sealed class PersonsServiceCachingDecorator(IPersonsService inner, IMemoryCache cache, IOptions<CacheSettings> cacheSettings) : IPersonsService
{
    internal static string PersonKey(int id) => $"example:person:{id}";

    public Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateAsync(
            PersonKey(id),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = cacheSettings.Value.PersonEntryLifetime;
                return await inner.GetPersonAsync(id, cancellationToken);
            });
    }

    public Task<Person?> UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default)
    {
        return inner.UpdatePersonAsync(message, cancellationToken);
    }
}