# Async

> Example **2** — Async/await and parallel task patterns.

```csharp
// ✗ wrong — blocks the thread
var result = service.ProcessAsync(id).Result;

// ✓ correct
var result = await service.ProcessAsync(id, cancellationToken);
```

```csharp
// ✗ wrong — CancellationToken not forwarded
public async Task RunAsync(CancellationToken cancellationToken)
{
    await inner.ProcessAsync(); // drops token
}

// ✓ correct
public async Task RunAsync(CancellationToken cancellationToken)
{
    await inner.ProcessAsync(cancellationToken);
}
```

## Parallel async (independent tasks only)

```csharp
// ✗ wrong — sequential when calls are independent
await orderClient.GetByIdAsync(orderId, cancellationToken);
await historyClient.GetByOrderIdAsync(orderId, cancellationToken);

// ✓ correct — start both, then await each (no Task.WhenAll needed)
var orderTask = orderClient.GetByIdAsync(orderId, cancellationToken);
var historyTask = historyClient.GetByOrderIdAsync(orderId, cancellationToken);
var order = await orderTask;
var history = await historyTask;
```

Parallelism comes from **starting** both tasks before any `await`. When tasks return `Task<T>`, await each task for its result — `Task.WhenAll` is not required.

Use **`Task.WhenAll`** when tasks return `Task` (no result) and you only need to wait for all to finish:

```csharp
// ✗ wrong — sequential when both are independent side effects
await blobClient.UploadBackupAsync(backup, cancellationToken);
await publisher.PublishOutboundAsync(outbound, cancellationToken);

// ✓ correct — start both, then WhenAll (no return values to read)
var backupTask = blobClient.UploadBackupAsync(backup, cancellationToken);
var publishTask = publisher.PublishOutboundAsync(outbound, cancellationToken);
await Task.WhenAll(backupTask, publishTask);
```

Only parallelize when tasks are **independent** — do not start the second call until you have the first result when it depends on it.

```csharp
// ✓ correct — second call needs first result; stay sequential
var order = await orderClient.GetByIdAsync(orderId, cancellationToken);
var history = await historyClient.GetByOrderIdAsync(order!.Id, cancellationToken);
```
