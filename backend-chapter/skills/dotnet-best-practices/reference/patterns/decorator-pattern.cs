// Decorator pattern — wrap an existing service to add cross-cutting behaviour without modifying the inner class.
// Chapter: BulkReplayServiceLoggingDecorator (ItFfTools).
// Registration uses Scrutor: services.Decorate<TInterface, TDecorator>().

// 1. Core service (single responsibility — business logic only)
public interface IStarWarsService
{
    Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken);
}

public sealed class StarWarsService(IStarWarsClient apiClient) : IStarWarsService
{
    public Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken) =>
        apiClient.GetCharacterAsync(name, cancellationToken);
}

// 2. Decorator — same interface, delegates to inner, adds logging/metrics/retry
public sealed class StarWarsServiceLoggingDecorator(
    IStarWarsService inner,
    ILogger<StarWarsServiceLoggingDecorator> logger) : IStarWarsService
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

public sealed class BulkReplayServiceLoggingDecorator(
    IBulkReplayService inner,
    ILogger<BulkReplayServiceLoggingDecorator> logger) : IBulkReplayService
{
    public async Task<ReplayExecutionResult> ReplayAsync(
        ToolEnvironment source,
        ToolEnvironment target,
        string orderId,
        string? suffix,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Replaying order {OrderId} from {Source} to {Target}.", orderId, source, target);
        return await inner.ReplayAsync(source, target, orderId, suffix, cancellationToken);
    }
}

// Guidelines:
// - Decorator implements the SAME interface as inner service
// - Inject inner as constructor parameter (composition)
// - Use for logging, timing, caching, PII sanitization — not core business rules
// - Prefer one decorator per concern; chain multiple Decorate<> calls if needed
// - Do not subclass StarWarsService to add logging — use decorator

interface IStarWarsClient { Task<Character?> GetCharacterAsync(string name, CancellationToken ct); }
interface IBulkReplayService
{
    Task<ReplayExecutionResult> ReplayAsync(ToolEnvironment source, ToolEnvironment target, string orderId, string? suffix, CancellationToken ct);
}
enum ToolEnvironment { Tst, Prd }
record ReplayExecutionResult;
record Character;
interface ILogger<T> { void LogInformation(string message, params object[] args); }
