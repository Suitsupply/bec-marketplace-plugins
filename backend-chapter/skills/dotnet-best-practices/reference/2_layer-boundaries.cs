// Layer boundaries — DTO vs domain model separation.
// App works ONLY with App.Models domain types. Api and Infra convert at their edges.
// See: 2_layer-boundaries.md

// ========== API LAYER — receives DTOs, passes domain to App ==========

// Api.Models — public HTTP request contract (wire DTO)
using System.Text.Json.Serialization;

namespace {ServiceName}.Api.Models.v1.Order.Requests;

public record FooCreatedRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

// App.Models — domain model
namespace {ServiceName}.App.Models.Order.Models;

public record FooCreatedWebhook(
    string Id,
    string Name,
    DateTimeOffset ReceivedAt);

// Api/Mappers — boundary conversion (no business logic); static class, no interface;
// v1 in folder/namespace, not type name
namespace {ServiceName}.Api.Mappers.v1;

public static class FooMapper
{
    public static FooCreatedWebhook ToDomain(FooCreatedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new FooCreatedWebhook(request.Id, request.Name, DateTimeOffset.UtcNow);
    }
}

// Api Function — deserialize DTO, map to domain, call App
// await service.ProcessAsync(FooMapper.ToDomain(requestDto), cancellationToken);

// App service — domain only; never sees Api.Models
namespace {ServiceName}.App.Services.Receivers;

public interface IFooReceiverService
{
    Task ProcessAsync(FooCreatedWebhook domain, CancellationToken cancellationToken);
}

// ========== INFRA LAYER — receives wire DTOs, returns domain to App ==========

// Infra wire DTO — internal; never on IClient interface
namespace {ServiceName}.Infra.Clients.FooClient.Models;

internal sealed record FooOrderWireDto(
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("status")] string Status);

// App.Models domain
namespace {ServiceName}.App.Models.Order.Models;

public record FooOrder(
    string Id,
    string Status);

// Infra mapping — wire → domain at client boundary
internal static class FooOrderWireDtoExtensions
{
    internal static FooOrder ToDomain(this FooOrderWireDto dto) =>
        new(dto.OrderId, dto.Status);
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
// public Task ProcessAsync(FooCreatedRequest request, …)
// public Task ProcessAsync(FooOrderWireDto dto, …)

// ✓ CORRECT — App always receives/uses App.Models domain types
