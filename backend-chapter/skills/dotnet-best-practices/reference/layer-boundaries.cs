// Layer boundaries — DTO vs domain model separation.
// App works ONLY with App.Models domain types. Api and Infra convert at their edges.
// See: layer-boundaries.md

// ========== API LAYER — receives DTOs, passes domain to App ==========

// Api.Models — public HTTP request contract (wire DTO)
namespace {ServiceName}.Api.Models.Order.Requests;

public record FooCreatedRequestDto(string Id, string Name);

// App.Models — domain model
namespace {ServiceName}.App.Models.Webhooks;

public record FooCreatedWebhook(string Id, string Name, DateTimeOffset ReceivedAt);

// Api/Mappers — boundary conversion (no business logic)
namespace {ServiceName}.Api.Mappers;

public interface IFooWebhookMapper
{
    FooCreatedWebhook ToDomain(FooCreatedRequestDto dto);
}

public sealed class FooWebhookMapper : IFooWebhookMapper
{
    public FooCreatedWebhook ToDomain(FooCreatedRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new FooCreatedWebhook(dto.Id, dto.Name, DateTimeOffset.UtcNow);
    }
}

// Api Function — deserialize DTO, map to domain, call App
// await service.ProcessAsync(webhookMapper.ToDomain(requestDto), cancellationToken);

// App service — domain only; never sees Api.Models
namespace {ServiceName}.App.Services.Receivers;

public interface IFooCreatedReceiverService
{
    Task ProcessAsync(FooCreatedWebhook domain, CancellationToken cancellationToken);
}

// ========== INFRA LAYER — receives wire DTOs, returns domain to App ==========

// Infra wire DTO — internal; never on IClient interface
namespace {ServiceName}.Infra.Clients.FooClient.Models;

internal sealed record FooOrderWireDto(string order_id, string status);

// App.Models domain
namespace {ServiceName}.App.Models.Foo;

public record FooOrder(string Id, string Status);

// Infra mapping — wire → domain at client boundary
internal static class FooOrderWireDtoExtensions
{
    internal static FooOrder ToDomain(this FooOrderWireDto dto) =>
        new(dto.order_id, dto.status);
}

// App/Clients/Interfaces — domain types in contract
namespace {ServiceName}.App.Clients.Interfaces;

public interface IFooClient
{
    Task<FooOrder?> GetOrderAsync(string id, CancellationToken cancellationToken);
}

// Infra/Clients/FooClient/FooClient.cs
// var wire = await response.Content.ReadFromJsonAsync<FooOrderWireDto>(ct);
// return wire?.ToDomain();

// ✗ WRONG — App service takes Api DTO or Infra wire DTO
// public Task ProcessAsync(FooCreatedRequestDto dto, …)
// public Task ProcessAsync(FooOrderWireDto dto, …)

// ✓ CORRECT — App always receives/uses App.Models domain types
