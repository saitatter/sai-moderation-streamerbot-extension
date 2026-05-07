using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IModerationDecisionStore
{
    Task SaveAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken = default);

    Task<StoredModerationDecision?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);
}
