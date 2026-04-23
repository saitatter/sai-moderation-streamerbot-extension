namespace Sai.Moderation.StreamerBot.Extension.Models;

public sealed record ModerationResult(
    string MessageId,
    ModerationVerdict Verdict,
    double Confidence,
    string Category,
    string Reason,
    int LatencyMs
);

