namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class StreamerBotRuntimeOptions
{
    public IReadOnlyList<string> ChatEventNames { get; init; } =
    [
        "ChatMessage",
        "TwitchChatMessage",
        "YouTubeMessage"
    ];

    public bool UseContainsFallback { get; init; } = true;
}
