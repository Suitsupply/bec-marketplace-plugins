// KISS — Keep It Simple, Stupid.
// Prefer the straightforward solution that the next developer understands in one read.
// Pair with 4_YAGNI.cs: simple now; add complexity only when requirements demand it.

// ✗ WRONG — generic pipeline framework for a one-step enrichment
public interface IEnrichmentStep<TIn, TOut> { Task<TOut> ExecuteAsync(TIn input, CancellationToken ct); }
public sealed class EnrichmentPipelineBuilder<TIn, TOut> { /* 200 lines of fluent API */ }

// ✓ CORRECT — direct pipeline when steps are fixed and few
public sealed class OrderEnrichmentPipeline(FetchOrderStep fetchOrder, ResolveLocationStep resolveLocation)
{
    public async Task<EnrichmentEnvelope<OrderWebhookRequest>> RunAsync(EnrichmentEnvelope<OrderWebhookRequest> envelope, CancellationToken cancellationToken)
    {
        await fetchOrder.ExecuteAsync(envelope, cancellationToken);
        await resolveLocation.ExecuteAsync(envelope, cancellationToken);
        return envelope;
    }
}

// ✗ WRONG — clever one-liner that hides failure modes
var result = items?.Where(x => x.Active)?.GroupBy(x => x.Type)?.ToDictionary(g => g.Key, g => g.Sum(x => x.Amt)) ?? [];

// ✓ CORRECT — named steps, obvious control flow
var activeItems = items.Where(x => x.Active);
var totalsByType = activeItems
    .GroupBy(x => x.Type)
    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

// ✗ WRONG — custom Result<T,E> monad for simple null checks
// ✓ CORRECT — return null when enriched input incomplete (precondition, not business rule)
public FooMaoModel? Map(FooEnrichmentEnvelope envelope)
{
    ArgumentNullException.ThrowIfNull(envelope);
    if (envelope.Order is null)
        return null;
    return new FooMaoModel(envelope.ResolvedMaoOrderId);
}