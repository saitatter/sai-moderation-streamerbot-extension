using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class ModerationBridgeService(
    IModerationBackendClient moderationBackendClient,
    IModerationEventPublisher moderationEventPublisher,
    ModerationBridgeOptions options,
    ILogger<ModerationBridgeService> logger)
{
    public async Task<ModerationResult> HandleChatEventAsync(
        ChatEvent chatEvent,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["messageId"] = chatEvent.MessageId,
            ["platform"] = chatEvent.Platform,
            ["channelId"] = chatEvent.ChannelId,
            ["username"] = chatEvent.Username
        });

        logger.LogInformation("Submitting message for moderation.");

        var request = new ModerationRequest(
            chatEvent.MessageId,
            chatEvent.Platform,
            chatEvent.ChannelId,
            chatEvent.UserId,
            chatEvent.Username,
            chatEvent.Text,
            chatEvent.ReceivedAt);

        var result = await moderationBackendClient.ModerateAsync(request, cancellationToken);
        logger.LogInformation(
            "Moderation verdict received: {Verdict} ({Category}) with confidence {Confidence}.",
            result.Verdict,
            result.Category,
            result.Confidence);

        await moderationEventPublisher.PublishDashboardEventAsync(chatEvent, result, cancellationToken);
        logger.LogDebug("Published moderation result to dashboard channel.");

        if (ShouldForwardToOverlay(result.Verdict))
        {
            await moderationEventPublisher.PublishOverlayEventAsync(chatEvent, result, cancellationToken);
            logger.LogDebug("Forwarded message to overlay channel.");
        }
        else
        {
            logger.LogDebug("Skipped overlay publish for verdict {Verdict}.", result.Verdict);
        }

        return result;
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
}
