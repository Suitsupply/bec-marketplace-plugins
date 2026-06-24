# Extensions — not Helper classes

Do **not** create `*Helper` classes in production code (`src/`). Prefer **extension methods** for small, pure logic that operates on a model or type.

---

## When to use extensions

| Use case | Example |
|----------|---------|
| Validation on a model | `order.IsShipToStore()` |
| Extracting / resolving fields | `order.ResolveAlternateOrderId()` |
| Aggregations on a collection | `lines.SumPresentmentAmount()` |
| String / ID parsing | `gid.GetShopifyNumericId()` |
| Money / address shaping | `money.ToMaoDecimal()`, `address.FormatSingleLine()` |
| Logging templates (optional) | `ILogger` prefix patterns, or `{Type}LoggingExtensions` in `Extensions/Logging/` when many call sites |
| DI registration | `services.AddInfrastructure(config)` in `Infra/Extensions/` |

**Rule:** if the logic is naturally phrased as “do something **with** or **to** this `Order` / `Money` / `Stream`”, make it an extension on that type.

---

## Naming and placement

| Rule | Detail |
|------|--------|
| Class name | `{Type}Extensions` — e.g. `ShopifyOrderExtensions`, `MoneyExtensions` |
| Method name | Verb phrase describing behaviour — `ResolveCustomerId`, `SumLineTotals` |
| Location | `App/Extensions/` for domain models; co-locate in `App.Models/` when the extended type lives there; `Infra/Extensions/` for DI; `Extensions/Logging/` for log templates |
| Shape | `public static class FooExtensions` with `public static TResult MethodName(this Foo foo, …)` |
| Purity | Prefer pure functions — no hidden I/O; inject services in enrichment steps or services instead |

```csharp
// ✓ correct — extension on the model
public static class OrderExtensions
{
    public static string? ResolveAlternateOrderId(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.Id.GetShopifyNumericId() ?? order.LegacyResourceId;
    }

    public static decimal SumLineTotals(this IReadOnlyList<OrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines.Sum(l => l.PresentmentAmount);
    }
}

// usage
var orderId = order.ResolveAlternateOrderId();
var total = envelope.Source.Lines.SumLineTotals();
```

---

## Do not create Helper classes

```csharp
// ✗ wrong — grab-bag Helper with unrelated static methods
public static class OrderHelper
{
    public static string? GetAlternateId(Order order) => …;
    public static decimal SumLines(IReadOnlyList<OrderLine> lines) => …;
    public static bool IsValidAddress(Address address) => …;
}

// usage — noisy, no discoverability on the type
var id = OrderHelper.GetAlternateId(order);
```

**Why extensions win:**
- Discoverable via IntelliSense on the instance (`order.…`)
- One file per extended concept (`OrderExtensions`, not `OrderHelper`)
- Clear receiver type — easier to test and review
- Avoids “junk drawer” static classes that violate SRP

---

## Allowed exceptions (not `*Helper`)

| Pattern | OK because |
|---------|------------|
| `{Type}Extensions` | Chapter standard for pure model logic |
| `ServiceCollectionExtensions` | DI registration — `AddInfrastructure`, `AddServiceInfo` |
| `*Constants` | Named constant holders — no behaviour |
| `UnitTests/Helpers/` | Test fixtures only (`FixtureFactory`, `FixtureExtensions`) — not shipped in `src/` |

---

## When **not** to use an extension

| Situation | Prefer |
|-----------|--------|
| Needs injected dependencies (HTTP client, DB) | Enrichment **step** or **service** |
| Multi-step orchestration | Service, pipeline, or base class |
| Behaviour varies by scenario | Strategy / factory + handler |
| One-off 2-line transform inside a single caller | Inline or private method — see YAGNI |

---

## Checklist

- [ ] No new `*Helper` classes in `src/`
- [ ] Small model logic is `{Type}Extensions` with `this` receiver
- [ ] Extension methods are pure (or logging/DI wiring only)
- [ ] `[ExcludeFromCodeCoverage]` on logic-free extension files when appropriate
