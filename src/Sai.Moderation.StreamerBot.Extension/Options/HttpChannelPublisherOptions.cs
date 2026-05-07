namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class HttpChannelPublisherOptions
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:8787";
    public string EventsBasePath { get; init; } = "/v1/events";
    public string? ApiToken { get; init; }
    public int TimeoutMs { get; init; } = 4000;
}
