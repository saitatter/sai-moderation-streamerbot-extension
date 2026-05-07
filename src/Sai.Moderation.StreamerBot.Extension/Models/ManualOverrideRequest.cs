namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record ManualOverrideRequest(
    string MessageId,
    ManualOverrideAction Action,
    string OperatorId,
    string Reason,
    DateTimeOffset RequestedAt
);
