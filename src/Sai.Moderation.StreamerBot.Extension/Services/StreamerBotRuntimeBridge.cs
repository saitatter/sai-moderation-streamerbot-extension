using Microsoft.Extensions.Logging;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotRuntimeBridge(
    StreamerBotChatEventHandler chatEventHandler,
    ILogger<StreamerBotRuntimeBridge> logger)
{
    public async Task<bool> ProcessRawChatEventAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        var moderationResult = await chatEventHandler.HandleRawEventAsync(rawJson, cancellationToken);
        if (moderationResult is null)
        {
            logger.LogDebug("Raw event was ignored because it did not map to a supported chat payload.");
            return false;
        }

        logger.LogInformation(
            "Completed chat moderation pipeline for message {MessageId} with verdict {Verdict}.",
            moderationResult.MessageId,
            moderationResult.Verdict);
        return true;
    }
}
