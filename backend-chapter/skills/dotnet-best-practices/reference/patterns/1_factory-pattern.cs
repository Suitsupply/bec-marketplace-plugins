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

public sealed class TransactionFlowHandlerFactory(KlarnaAuthorizationFlowHandler klarnaAuthorization, KlarnaCaptureFlowHandler klarnaCapture, NonKlarnaAuthorizationFlowHandler nonKlarnaAuthorization, NonKlarnaCaptureFlowHandler nonKlarnaCapture, RefundFlowHandler refund) : ITransactionFlowHandlerFactory
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