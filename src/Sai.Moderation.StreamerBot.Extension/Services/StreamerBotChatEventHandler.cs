using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotChatEventHandler(
    IStreamerBotChatEventMapper mapper,
    ModerationBridgeService moderationBridgeService)
{
    public async Task<ModerationResult?> HandleRawEventAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (!mapper.TryMap(rawJson, out var chatEvent) || chatEvent is null)
        {
            return null;
        }

        return await moderationBridgeService.HandleChatEventAsync(chatEvent, cancellationToken);
    }
}

