using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class ModerationBridgeService(
    IModerationBackendClient moderationBackendClient,
    IModerationEventPublisher moderationEventPublisher,
    ModerationBridgeOptions options)
{
    public async Task<ModerationResult> HandleChatEventAsync(
        ChatEvent chatEvent,
        CancellationToken cancellationToken = default)
    {
        var request = new ModerationRequest(
            chatEvent.MessageId,
            chatEvent.Platform,
            chatEvent.ChannelId,
            chatEvent.UserId,
            chatEvent.Username,
            chatEvent.Text,
            chatEvent.ReceivedAt);

        var result = await moderationBackendClient.ModerateAsync(request, cancellationToken);

        await moderationEventPublisher.PublishDashboardEventAsync(chatEvent, result, cancellationToken);

        if (ShouldForwardToOverlay(result.Verdict))
        {
            await moderationEventPublisher.PublishOverlayEventAsync(chatEvent, result, cancellationToken);
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

