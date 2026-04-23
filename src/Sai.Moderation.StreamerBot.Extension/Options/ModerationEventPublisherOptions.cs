namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class ModerationEventPublisherOptions
{
    public required string BaseUrl { get; init; }
    public string DashboardPath { get; init; } = "/v1/events/dashboard";
    public string OverlayPath { get; init; } = "/v1/events/overlay";
    public int RequestTimeoutMs { get; init; } = 3_000;
}

