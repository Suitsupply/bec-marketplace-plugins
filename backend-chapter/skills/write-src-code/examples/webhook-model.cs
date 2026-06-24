using System.Text.Json.Serialization;

namespace {ServiceName}.App.Models.Webhooks;

public record FooCreatedWebhookRequest(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name);
