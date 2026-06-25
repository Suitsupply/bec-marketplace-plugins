namespace {ServiceName}.App.Enrichment;

public sealed class FooEnrichmentPipeline(FetchOrderStep fetchOrderStep, FetchMetadataStep fetchMetadataStep)
{
    public async Task RunAsync(EnrichmentEnvelope<FooCreatedWebhook> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        envelope.Order = await fetchOrderStep.ExecuteAsync(envelope.Source.Id, cancellationToken);
        envelope.Metadata = await fetchMetadataStep.ExecuteAsync(envelope.Source.Id, envelope.Order, cancellationToken);
    }
}
