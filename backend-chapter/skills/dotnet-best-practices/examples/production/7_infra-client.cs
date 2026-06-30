using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using {ServiceName}.App.Clients.Interfaces;
using {ServiceName}.App.Models.Foo.Models;

namespace {ServiceName}.Infra.Clients.FooClient;

// Infra/Clients/FooClient/Models/FooResultWireDto.cs
internal sealed record FooResultWireDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal static class FooResultWireDtoExtensions
{
    internal static FooResult ToDomain(this FooResultWireDto dto) =>
        new(dto.Id, dto.Name);
}

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

        var wire = await response.Content.ReadFromJsonAsync<FooResultWireDto>(cancellationToken: cancellationToken);
        return wire?.ToDomain();
    }
}
