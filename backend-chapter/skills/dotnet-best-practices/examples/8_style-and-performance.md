# Style and performance

> Example **8** — Immutability, readability, LINQ formatting, naming, and performance.

## Immutability

```csharp
// ✗ wrong — mutates parameter
void Enrich(MyModel model) { model.Field = "value"; }

// ✓ correct — return new object (record with expression)
FooWebhookRequest Enrich(FooWebhookRequest request) => request with { Status = "processed" };
```

## Readability over optimisation

```csharp
// ✗ wrong — premature micro-optimisation hurts readability
var sb = new StringBuilder();
for (var i = 0; i < ids.Length; i++) sb.Append(ids[i].ToString("X"));
return sb.ToString();

// ✓ correct — clear unless profiling shows this path is hot
return string.Join(',', ids);
```

## Return directly

```csharp
// ✗ wrong — local used only for return
var names = items.Select(i => i.Name);
return names;

// ✓ correct
return items.Select(i => i.Name);
```

Use a local when the value is reused, the name aids readability, or you are building up a result across steps.

## LINQ formatting

```csharp
// ✓ correct — source on new line, one operator per line
var match =
    items?
        .Where(i => i.IsActive)
        .OrderBy(i => i.Priority)
        .FirstOrDefault(i => i.Code == targetCode)
        ?.Value;

// ✗ wrong — long chain on one line
var match = items?.Where(i => i.IsActive).OrderBy(i => i.Priority).FirstOrDefault(i => i.Code == targetCode)?.Value;

// ✗ wrong — multiple operators on one line
var match = items?.Where(i => i.IsActive).OrderBy(i => i.Priority)
    .FirstOrDefault(i => i.Code == targetCode)?.Value;
```

Short chains that fit within **160 characters** may stay on one line.

## Naming

```csharp
// ✗ wrong — no Async suffix on async method
public Task Process(CancellationToken ct) => ...

// ✓ correct
public Task ProcessAsync(CancellationToken cancellationToken) => ...
```

## Performance

```csharp
// ✗ wrong — multiple enumerations
if (items.Any() && items.Count() > 1) { ... }

// ✓ correct — materialize once when needed twice
var list = items.ToList();
if (list.Count > 1) { ... }
```
