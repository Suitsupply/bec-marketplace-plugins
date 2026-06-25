// Separation of Concerns — each layer owns one aspect; DTO↔domain conversion at Api and Infra edges.
// Chapter layout: Api → Infra → App → App.Models (inward dependencies only).
// See: 2_layer-boundaries.md

/*
 * Layer responsibilities:
 *
 * {ServiceName}.Api        HTTP entry; deserializes HTTP DTOs; maps to domain before App
 * {ServiceName}.Api.Models Public HTTP wire contracts (NuGet)
 * {ServiceName}.App        Use cases — domain models ONLY; no Api.Models or Infra wire DTOs
 * {ServiceName}.App.Models Domain models — App's language
 * {ServiceName}.Infra      Client impls; wire DTOs in Infra/.../Models/; map to domain before return
 */

// ✗ WRONG — Function calls App with raw JSON; App deserializes wire shape
public sealed class FooReceiver(IFooService service)
{
    public Task Run(HttpRequest req, CancellationToken ct) =>
        service.ProcessAsync(req.Body.ReadAsString(), ct);  // App sees transport, not domain
}

// ✓ CORRECT — Api converts DTO → domain at boundary
public sealed class FooReceiver(IFooService service, IFooWebhookMapper mapper)
{
    public async Task Run(HttpRequest req, CancellationToken ct)
    {
        var dto = await req.ReadFromJsonAsync<FooCreatedRequest>(ct);
        await service.ProcessAsync(mapper.ToDomain(dto!), ct);
    }
}

// ✗ WRONG — IClient returns Infra wire DTO
public interface IFooClient { Task<FooOrderWireDto?> GetAsync(string id, CancellationToken ct); }

// ✓ CORRECT — IClient returns App.Models domain; Infra maps internally
public interface IFooClient { Task<FooOrder?> GetAsync(string id, CancellationToken ct); }

// App orchestrates domain; enrichment applies business rules; Infra maps domain → wire at publish/call boundaries.