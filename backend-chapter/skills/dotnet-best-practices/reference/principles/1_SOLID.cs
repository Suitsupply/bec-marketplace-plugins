// SOLID — five design principles for maintainable backend services.
// Chapter default: constructor injection, single-responsibility types, extend via handlers not switches.
// See also: 5_SeparationOfConcerns.cs, 7_CompositionOverInheritance.cs

// --- S — Single Responsibility: one reason to change per class ---
// ✓ ReceiverServiceBase orchestrates receive flow; subclasses only define event-specific hooks.
// ✗ OrderService that validates, enriches, maps, publishes, and backs up in one class.

// --- O — Open/Closed: open for extension, closed for modification ---
// ✓ Add KlarnaCaptureFlowHandler + register in DI — no change to existing handlers.
// ✗ Growing if/else in processor for every new payment gateway.

public interface ITransactionFlowHandler
{
    Task HandleAsync(TransactionContext context, CancellationToken cancellationToken);
}

// --- L — Liskov Substitution: subtypes honour the base contract ---
// ✓ Every ReceiverServiceBase<T> subclass can replace the base without breaking ProcessAsync.
public abstract class ReceiverServiceBase<TModel> where TModel : class
{
    public async Task ProcessAsync(TModel model, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        var rawJson = Serialize(model);
        await BackupAsync(rawJson, model, cancellationToken);
        await SendToQueueAsync(rawJson, model, cancellationToken);
    }

    protected abstract string GetPath(TModel model);
    private string Serialize(TModel model) => default!;
    private Task BackupAsync(string rawJson, TModel model, CancellationToken ct) => Task.CompletedTask;
    private Task SendToQueueAsync(string rawJson, TModel model, CancellationToken ct) => Task.CompletedTask;
}

// --- I — Interface Segregation: small, focused interfaces ---
// ✓ IReceiverService / IProcessorService marker interfaces; IFooClient with only needed methods.
// ✗ IOrderOperations with 30 methods when callers need only GetAsync.

public interface IFooClient
{
    Task<FooResult?> GetAsync(string id, CancellationToken cancellationToken);
}

// --- D — Dependency Inversion: depend on abstractions, not concretions ---
// ✓ App defines IFooClient; Infra implements FooClient; services take IFooClient in constructor.
// ✗ new FooClient() inside a processor service.

public sealed class FooProcessorService(IFooClient fooClient)
{
    public Task ProcessAsync(string id, CancellationToken cancellationToken) =>
        fooClient.GetAsync(id, cancellationToken);
}
