using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IModerationBackendClient
{
    Task<ModerationResult> ModerateAsync(ModerationRequest request, CancellationToken cancellationToken);
}

