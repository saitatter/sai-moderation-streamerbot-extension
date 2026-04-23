using System.Collections.Concurrent;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class InMemoryModerationDecisionStore : IModerationDecisionStore
{
    private readonly ConcurrentDictionary<string, StoredModerationDecision> decisions = new();

    public Task SaveAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken = default)
    {
        decisions[result.MessageId] = new StoredModerationDecision(chatEvent, result);
        return Task.CompletedTask;
    }

    public Task<StoredModerationDecision?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        decisions.TryGetValue(messageId, out var decision);
        return Task.FromResult(decision);
    }
}
