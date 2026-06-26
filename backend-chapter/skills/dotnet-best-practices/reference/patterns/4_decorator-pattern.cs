// Decorator pattern — wrap an existing service to add cross-cutting behaviour without modifying the inner class.
// Chapter: BulkReplayServiceLoggingDecorator (ItFfTools).
// Registration uses Scrutor: services.Decorate<TInterface, TDecorator>().

// 1. Core service (single responsibility — business logic only)
public interface IStarWarsService
{
    Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken);
}

public sealed class StarWarsService(IStarWarsClient starWarsClient) : IStarWarsService
{
    public Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken) =>
        starWarsClient.GetCharacterAsync(name, cancellationToken);
}

// 2. Decorator — same interface, delegates to inner, adds logging/metrics/retry
public sealed class StarWarsServiceLoggingDecorator(IStarWarsService inner, ILogger<StarWarsServiceLoggingDecorator> logger) : IStarWarsService
{
    public async Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching Star Wars character {Name}", name);
        var result = await inner.GetCharacterAsync(name, cancellationToken);
        logger.LogInformation("Fetched character {Name}: found={Found}", name, result is not null);
        return result;
    }
}

// 3. Registration — register concrete service first, then decorate
// Requires NuGet: Scrutor
//
// builder.Services.AddScoped<IStarWarsService, StarWarsService>();
// builder.Services.Decorate<IStarWarsService, StarWarsServiceLoggingDecorator>();

// Real chapter example (ItFfTools.Api/Program.cs):
// builder.Services.AddScoped<IBulkReplayService, BulkReplayService>();
// builder.Services.Decorate<IBulkReplayService, BulkReplayServiceLoggingDecorator>();

public sealed class BulkReplayServiceLoggingDecorator(IBulkReplayService inner, ILogger<BulkReplayServiceLoggingDecorator> logger) : IBulkReplayService
{
    public async Task<ReplayExecutionResult> ReplayAsync(ToolEnvironment source, ToolEnvironment target, string orderId, string? suffix, CancellationToken cancellationToken)
    {
        logger.LogInformation("Replaying order {OrderId} from {Source} to {Target}.", orderId, source, target);
        return await inner.ReplayAsync(source, target, orderId, suffix, cancellationToken);
    }
}

// 4. Caching decorator — an infrastructure concern layered onto an App service interface.
//    Lives in Infra (Infra/.../Decorators/), decorates an App I*Service, and reads a validated
//    IOptions<CacheSettings> (FluentValidation + ValidateOnStart — see 12_configuration-validation.md).
//    Registration (Program.cs): register the concrete App service first, then decorate.
//
//    services.AddScoped<IPersonsService, PersonsService>();
//    services.Decorate<IPersonsService, PersonsServiceCachingDecorator>();

public sealed class PersonsServiceCachingDecorator(IPersonsService inner, IMemoryCache cache, IOptions<CacheSettings> cacheSettings)
    : IPersonsService
{
    internal static string PersonKey(int id) => $"example:person:{id}";

    public Task<Person?> GetPersonAsync(int id, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync(
            PersonKey(id),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = cacheSettings.Value.PersonEntryLifetime;
                return await inner.GetPersonAsync(id, cancellationToken);
            });

    // Write paths delegate straight through — only reads are cached.
    public Task<Person?> UpdatePersonAsync(UpdatePerson message, CancellationToken cancellationToken = default) =>
        inner.UpdatePersonAsync(message, cancellationToken);
}

// Guidelines:
// - Decorator implements the SAME interface as inner service
// - Inject inner as constructor parameter (composition)
// - Use for logging, timing, caching, PII sanitization — not core business rules
// - Caching decorators belong in Infra (caching is infrastructure), decorate an App I*Service,
//   and drive expiry from a validated IOptions<CacheSettings>; only cache reads, delegate writes
// - Prefer one decorator per concern; chain multiple Decorate<> calls if needed
// - Do not subclass StarWarsService to add logging — use decorator
