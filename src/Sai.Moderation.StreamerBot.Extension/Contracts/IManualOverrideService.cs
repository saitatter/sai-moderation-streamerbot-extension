using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IManualOverrideService
{
    Task<ModerationResult?> ApplyOverrideAsync(
        ManualOverrideRequest request,
        CancellationToken cancellationToken = default);
}
