// App.Models/{Feature}/Models/Webhooks/ — domain shape after ingress (no JsonPropertyName).
// Integration processors may deserialize queue payloads to this type; HTTP ingress should map at Api per layer boundaries.

namespace {ServiceName}.App.Models.Foo.Models.Webhooks;

public record FooCreatedWebhook(
    long Id,
    string? Name);
