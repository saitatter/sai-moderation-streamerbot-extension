namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record StreamerBotCallbackEvent(
    string EventName,
    string RawJson,
    DateTimeOffset ReceivedAt
);
