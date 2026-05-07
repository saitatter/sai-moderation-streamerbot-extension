using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotModerationEventPublisher(
    IStreamerBotChannelPublisher channelPublisher,
    StreamerBotPublishOptions options,
    ILogger<StreamerBotModerationEventPublisher> logger) : IModerationEventPublisher
{
    public Task PublishDashboardEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "moderation.result",
            messageId = result.MessageId,
            platform = chatEvent.Platform,
            username = chatEvent.Username,
            text = chatEvent.Text,
            verdict = ToWireVerdict(result.Verdict),
            confidence = result.Confidence,
            category = result.Category,
            reason = result.Reason,
            receivedAt = chatEvent.ReceivedAt
        });

        logger.LogDebug("Publishing dashboard moderation event for {MessageId}.", result.MessageId);
        return channelPublisher.PublishAsync(options.DashboardChannel, payload, cancellationToken);
    }

    public Task PublishOverlayEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "overlay.message",
            messageId = result.MessageId,
            platform = chatEvent.Platform,
            username = chatEvent.Username,
            text = chatEvent.Text,
            verdict = ToWireVerdict(result.Verdict)
        });

        logger.LogDebug("Publishing overlay event for {MessageId}.", result.MessageId);
        return channelPublisher.PublishAsync(options.OverlayChannel, payload, cancellationToken);
    }

    public Task PublishManualOverrideEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        ManualOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "moderation.override",
            messageId = result.MessageId,
            operatorId = request.OperatorId,
            action = ToWireAction(request.Action),
            reason = request.Reason,
            verdict = ToWireVerdict(result.Verdict),
            category = result.Category
        });

        logger.LogDebug("Publishing manual override event for {MessageId}.", result.MessageId);
        return channelPublisher.PublishAsync(options.DashboardChannel, payload, cancellationToken);
    }

    private static string ToWireVerdict(ModerationVerdict verdict)
    {
        return verdict.ToString().ToLowerInvariant();
    }

    private static string ToWireAction(ManualOverrideAction action)
    {
        return action switch
        {
            ManualOverrideAction.Approve => "approve",
            ManualOverrideAction.Block => "block",
            ManualOverrideAction.FalsePositive => "falsePositive",
            _ => action.ToString()
        };
    }
}
