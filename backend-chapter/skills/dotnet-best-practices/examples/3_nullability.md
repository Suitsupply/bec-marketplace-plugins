# Nullability

> Example **3** — Null guards and nullable reference type usage.

```csharp
// ✗ wrong — manual null check
if (request == null) throw new ArgumentNullException(nameof(request));

// ✓ correct
ArgumentNullException.ThrowIfNull(request);
```

```csharp
// ✗ wrong — unnecessary null-forgiving
var name = order!.Name; // no prior guard

// ✓ correct — guard first, then use
ArgumentNullException.ThrowIfNull(order);
var name = order.Name;
```
