namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record ModerationRequest(
    string MessageId,
    string Platform,
    string ChannelId,
    string UserId,
    string Username,
    string Text,
    DateTimeOffset ReceivedAt
);

