namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record ChatEvent(
    string MessageId,
    string Platform,
    string ChannelId,
    string UserId,
    string Username,
    string Text,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<string>? Badges = null
);

