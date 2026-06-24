// Strategy pattern — interchangeable algorithms behind one interface; caller picks (or factory resolves) the right one.
// Chapter: ITransactionFlowHandler — one handler per payment/refund scenario.

public interface ITransactionFlowHandler
{
    Task HandleAsync(TransactionContext context, CancellationToken cancellationToken);
}

// Each strategy = one scenario, single responsibility
public sealed class KlarnaCaptureFlowHandler(
    IOrderTransactionCreatedMapper mapper,
    IMaoPublisher publisher,
    ILogger<KlarnaCaptureFlowHandler> logger)
    : ITransactionFlowHandler
{
    public async Task HandleAsync(TransactionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var payload = mapper.Map(context);
        if (payload is null) return;
        await publisher.PublishAsync(payload, cancellationToken);
        logger.LogInformation("Published Klarna capture for order {OrderId}", context.OrderId);
    }
}

public sealed class RefundFlowHandler(
    IOrderHistoryService orderHistoryService,
    ITransactionToUpdatePaymentTransactionMapper mapper,
    IMaoPublisher publisher)
    : ITransactionFlowHandler
{
    public async Task HandleAsync(TransactionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var history = await orderHistoryService.GetAsync(context.OrderId, cancellationToken);
        var payload = mapper.Map(context, history);
        if (payload is not null)
            await publisher.PublishAsync(payload, cancellationToken);
    }
}

// Context object — data strategies need (avoid passing raw strings everywhere)
public record TransactionContext(string OrderId, string Kind, string Gateway, bool IsKlarna);

// Orchestrator delegates — does not contain gateway-specific rules
public sealed class OrderTransactionProcessorService(ITransactionFlowHandlerFactory factory)
{
    public Task ProcessAsync(TransactionContext context, CancellationToken cancellationToken) =>
        factory.Resolve(context).HandleAsync(context, cancellationToken);
}

// ✗ WRONG — strategy logic as switch in processor (grows without bound)
// ✓ CORRECT — new gateway/kind = new ITransactionFlowHandler + DI registration

// Registration (all Transient):
// services.AddTransient<ITransactionFlowHandlerFactory, TransactionFlowHandlerFactory>();
// services.AddTransient<KlarnaCaptureFlowHandler>();
// services.AddTransient<RefundFlowHandler>();

interface ITransactionFlowHandlerFactory { ITransactionFlowHandler Resolve(TransactionContext context); }
interface IOrderTransactionCreatedMapper { object? Map(TransactionContext context); }
interface ITransactionToUpdatePaymentTransactionMapper { object? Map(TransactionContext context, object history); }
interface IOrderHistoryService { Task<object> GetAsync(string orderId, CancellationToken ct); }
interface IMaoPublisher { Task PublishAsync(object payload, CancellationToken ct); }
interface ILogger<T> { void LogInformation(string message, params object[] args); }
