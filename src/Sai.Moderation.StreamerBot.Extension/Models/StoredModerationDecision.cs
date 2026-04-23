namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record StoredModerationDecision(
    ChatEvent ChatEvent,
    ModerationResult Result
);
