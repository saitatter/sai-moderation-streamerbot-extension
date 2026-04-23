using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IModerationEventPublisher
{
    Task PublishDashboardEventAsync(ChatEvent chatEvent, ModerationResult result, CancellationToken cancellationToken);
    Task PublishOverlayEventAsync(ChatEvent chatEvent, ModerationResult result, CancellationToken cancellationToken);
    Task PublishManualOverrideEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        ManualOverrideRequest request,
        CancellationToken cancellationToken);
}
