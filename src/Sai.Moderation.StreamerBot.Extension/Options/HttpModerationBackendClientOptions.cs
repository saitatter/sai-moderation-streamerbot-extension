namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class HttpModerationBackendClientOptions
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:8787";
    public string ModeratePath { get; init; } = "/v1/moderate";
    public string? ApiToken { get; init; }
    public int TimeoutMs { get; init; } = 6000;
    public int MaxAttempts { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 200;
}
