// Encapsulation — hide implementation detail; expose only what callers need.
// Chapter: internal Infra clients, public App interfaces, immutable DTOs, no leaking HTTP/types across layers.

// ✓ Interface in App/Clients/Interfaces (public contract); implementation internal in Infra
namespace {ServiceName}.App.Clients.Interfaces;

public interface IFooClient
{
    Task<FooResult?> GetAsync(string id, CancellationToken cancellationToken);
}

namespace {ServiceName}.Infra.Clients.FooClient;

internal sealed class FooClient(HttpClient httpClient) : IFooClient
{
    public async Task<FooResult?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"items/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FooResult>(cancellationToken: cancellationToken);
    }
}

// ✓ Settings bound via IOptions — consumers read values, not IConfiguration keys
public sealed class FooProcessorService(IOptions<FooSettings> options, IFooClient client)
{
    public Task ProcessAsync(CancellationToken cancellationToken) =>
        client.GetAsync("1", cancellationToken);
}

// ✓ Immutable DTO — callers cannot mutate shared state after construction
public record FooWebhookRequest
{
    public required string Id { get; init; }
    public required string Status { get; init; }
}

// ✗ WRONG — public setters on model passed through pipeline
public class LeakyModel
{
    public string Id { get; set; } = "";
    public HttpResponseMessage RawResponse { get; set; } = default!;  // Infra type in App model
}

// ✗ WRONG — exposing ServiceCollectionExtensions internals to App
// App must never reference Infra project.