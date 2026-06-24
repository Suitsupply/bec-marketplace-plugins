using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using {ServiceName}.App.Clients.Interfaces;

namespace {ServiceName}.Infra.Clients.FooClient;

[ExcludeFromCodeCoverage]
internal sealed class FooClient(HttpClient httpClient) : IFooClient
{
    public async Task<FooResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var response = await httpClient.GetAsync($"api/v1/foo/{Uri.EscapeDataString(id)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FooResult>(cancellationToken: cancellationToken);
    }
}
