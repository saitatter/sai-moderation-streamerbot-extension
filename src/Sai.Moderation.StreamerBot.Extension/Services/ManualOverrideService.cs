using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class ManualOverrideService(
    IModerationDecisionStore decisionStore,
    IModerationEventPublisher moderationEventPublisher,
    ModerationBridgeOptions options,
    ILogger<ManualOverrideService> logger) : IManualOverrideService
{
    public async Task<ModerationResult?> ApplyOverrideAsync(
        ManualOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            logger.LogDebug("Ignored manual override because message id is empty.");
            return null;
        }

        var decision = await decisionStore.GetByMessageIdAsync(request.MessageId, cancellationToken);
        if (decision is null)
        {
            logger.LogWarning("Manual override skipped. Message {MessageId} is unknown.", request.MessageId);
            return null;
        }

        var overriddenVerdict = MapVerdict(request.Action);
        var overriddenResult = decision.Result with
        {
            Verdict = overriddenVerdict,
            Category = "manual-override",
            Reason = $"manual:{request.Action.ToString().ToLowerInvariant()} by {request.OperatorId} - {request.Reason}"
        };

        await decisionStore.SaveAsync(decision.ChatEvent, overriddenResult, cancellationToken);
        await moderationEventPublisher.PublishManualOverrideEventAsync(
            decision.ChatEvent,
            overriddenResult,
            request,
            cancellationToken);

        if (ShouldForwardToOverlay(overriddenResult.Verdict))
        {
            await moderationEventPublisher.PublishOverlayEventAsync(
                decision.ChatEvent,
                overriddenResult,
                cancellationToken);
        }

        return overriddenResult;
    }

    private bool ShouldForwardToOverlay(ModerationVerdict verdict)
    {
        return verdict switch
        {
            ModerationVerdict.Allow => true,
            ModerationVerdict.Flag => options.ForwardFlagsToOverlay,
            _ => false
        };
    }

    private static ModerationVerdict MapVerdict(ManualOverrideAction action)
    {
        return action switch
        {
            ManualOverrideAction.Approve => ModerationVerdict.Allow,
            ManualOverrideAction.FalsePositive => ModerationVerdict.Allow,
            ManualOverrideAction.Block => ModerationVerdict.Block,
            _ => ModerationVerdict.Flag
        };
    }
}
