// Factory pattern — centralizes creation or selection of implementations.
// Chapter examples: ITransactionFlowHandlerFactory, test FixtureFactory, StarWarsClientFactory (training shape).

// --- Factory selects strategy implementation (most common in App layer) ---

public interface ITransactionFlowHandler
{
    Task HandleAsync(Transaction tx, CancellationToken cancellationToken);
}

public interface ITransactionFlowHandlerFactory
{
    ITransactionFlowHandler Resolve(Transaction tx);
}

public sealed class TransactionFlowHandlerFactory(
    KlarnaAuthorizationFlowHandler klarnaAuthorization,
    KlarnaCaptureFlowHandler klarnaCapture,
    NonKlarnaAuthorizationFlowHandler nonKlarnaAuthorization,
    NonKlarnaCaptureFlowHandler nonKlarnaCapture,
    RefundFlowHandler refund)
    : ITransactionFlowHandlerFactory
{
    public ITransactionFlowHandler Resolve(Transaction tx)
    {
        ArgumentNullException.ThrowIfNull(tx);
        if (tx.IsRefund()) return refund;
        if (tx.IsKlarna())
            return tx.IsAuthorization() ? klarnaAuthorization : klarnaCapture;
        return tx.IsAuthorization() ? nonKlarnaAuthorization : nonKlarnaCapture;
    }
}

// Registration — factory + all handlers Transient
// services.AddTransient<ITransactionFlowHandlerFactory, TransactionFlowHandlerFactory>();
// services.AddTransient<KlarnaAuthorizationFlowHandler>();
// …

// --- Factory creates configured clients (when construction is non-trivial) ---

public interface IStarWarsClient
{
    Task<Character?> GetCharacterAsync(string name, CancellationToken cancellationToken);
}

public interface IStarWarsClientFactory
{
    IStarWarsClient Create(ToolEnvironment environment);
}

public sealed class StarWarsClientFactory(IHttpClientFactory httpClientFactory, IOptions<StarWarsSettings> settings)
    : IStarWarsClientFactory
{
    public IStarWarsClient Create(ToolEnvironment environment)
    {
        var baseUrl = environment switch
        {
            ToolEnvironment.Tst => settings.Value.TstBaseUrl,
            ToolEnvironment.Prd => settings.Value.PrdBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(environment))
        };
        var httpClient = httpClientFactory.CreateClient(nameof(StarWarsClient));
        httpClient.BaseAddress = baseUrl;
        return new StarWarsClient(httpClient);
    }
}

// services.AddScoped<IStarWarsClientFactory, StarWarsClientFactory>();

// Processor uses factory — no switch in business method
public sealed class TransactionProcessorService(ITransactionFlowHandlerFactory factory)
{
    public Task ProcessAsync(Transaction tx, CancellationToken cancellationToken)
    {
        var handler = factory.Resolve(tx);
        return handler.HandleAsync(tx, cancellationToken);
    }
}

// --- Framework factory (do not wrap unnecessarily) ---
// IHttpClientFactory — register typed clients via AddHttpClient<TInterface, TImplementation>().

enum ToolEnvironment { Tst, Prd }
record Transaction { public bool IsRefund() => false; public bool IsKlarna() => false; public bool IsAuthorization() => false; public bool IsCapture() => false; }
record Character;
record StarWarsSettings { public Uri TstBaseUrl { get; init; } = default!; public Uri PrdBaseUrl { get; init; } = default!; }
sealed class KlarnaAuthorizationFlowHandler : ITransactionFlowHandler { public Task HandleAsync(Transaction tx, CancellationToken ct) => Task.CompletedTask; }
sealed class KlarnaCaptureFlowHandler : ITransactionFlowHandler { public Task HandleAsync(Transaction tx, CancellationToken ct) => Task.CompletedTask; }
sealed class NonKlarnaAuthorizationFlowHandler : ITransactionFlowHandler { public Task HandleAsync(Transaction tx, CancellationToken ct) => Task.CompletedTask; }
sealed class NonKlarnaCaptureFlowHandler : ITransactionFlowHandler { public Task HandleAsync(Transaction tx, CancellationToken ct) => Task.CompletedTask; }
sealed class RefundFlowHandler : ITransactionFlowHandler { public Task HandleAsync(Transaction tx, CancellationToken ct) => Task.CompletedTask; }
sealed class StarWarsClient(HttpClient httpClient) : IStarWarsClient { public Task<Character?> GetCharacterAsync(string name, CancellationToken ct) => Task.FromResult<Character?>(null); }
interface IHttpClientFactory { HttpClient CreateClient(string name); }
class IOptions<T> { public T Value => default!; }
