namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class ModerationBackendClientOptions
{
    public required string BaseUrl { get; init; }
    public string ModeratePath { get; init; } = "/v1/moderate";
    public int RequestTimeoutMs { get; init; } = 3_000;
    public int MaxAttempts { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 250;
}

