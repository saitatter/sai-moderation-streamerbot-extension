using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotChatEventHandler(
    IStreamerBotChatEventMapper mapper,
    ModerationBridgeService moderationBridgeService,
    ILogger<StreamerBotChatEventHandler> logger)
{
    public async Task<ModerationResult?> HandleRawEventAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            logger.LogDebug("Ignored empty raw chat payload.");
            return null;
        }

        if (!mapper.TryMap(rawJson, out var chatEvent) || chatEvent is null)
        {
            logger.LogDebug("Ignored raw event because mapper could not normalize the payload.");
            return null;
        }

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["messageId"] = chatEvent.MessageId,
            ["platform"] = chatEvent.Platform,
            ["channelId"] = chatEvent.ChannelId
        });

        logger.LogInformation("Mapped raw chat event for user {Username}.", chatEvent.Username);
        return await moderationBridgeService.HandleChatEventAsync(chatEvent, cancellationToken);
    }
}
