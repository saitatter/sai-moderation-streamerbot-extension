namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class StreamerBotPublishOptions
{
    public string DashboardChannel { get; init; } = "moderation.dashboard";
    public string OverlayChannel { get; init; } = "chat.overlay";
}
