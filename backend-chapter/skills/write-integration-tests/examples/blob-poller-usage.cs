// Poll deployed environment for observable side effects (e.g. backup blob).
var poller = new SideEffectBlobPoller(settings.BlobConnectionString, containerName);
var blobContent = await poller.PollForContentAsync(
    resourceId,
    eventTypeTag,
    testStartedAt,
    timeout: TimeSpan.FromSeconds(120),
    cancellationToken);

// testStartedAt must be captured immediately before the webhook POST, not during Given steps.
