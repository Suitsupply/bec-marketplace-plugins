using System.Diagnostics.CodeAnalysis;

namespace Template.Api.Messaging.Settings;

[ExcludeFromCodeCoverage]
public record MessageRetryOptions
{
    public int MaxDeliveryCount { get; init; } = 3;

    /// <summary>Base delay between retry attempts. When <see cref="BackoffMultiplier"/> is greater than 1 this is the delay for the first retry.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff multiplier applied to <see cref="RetryDelay"/> on each attempt: RetryDelay × BackoffMultiplier^(attempt − 1).
    /// Use 1 for a fixed delay, 2 to double each time, 1.5 for slower growth, etc.
    /// </summary>
    public double BackoffMultiplier { get; init; } = 1;
}