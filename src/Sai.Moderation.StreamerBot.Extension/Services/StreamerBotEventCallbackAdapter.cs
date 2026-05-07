using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotEventCallbackAdapter(
    IStreamerBotRuntimeBridge runtimeBridge,
    StreamerBotRuntimeOptions options,
    ILogger<StreamerBotEventCallbackAdapter> logger) : IStreamerBotEventCallbackAdapter
{
    public async Task<bool> HandleIncomingEventAsync(
        string eventName,
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            logger.LogDebug("Ignored event callback because event name is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            logger.LogDebug("Ignored event {EventName} because payload is empty.", eventName);
            return false;
        }

        if (!IsChatEvent(eventName))
        {
            logger.LogDebug("Ignored non-chat event {EventName}.", eventName);
            return false;
        }

        return await runtimeBridge.ProcessRawChatEventAsync(rawJson, cancellationToken);
    }

    private bool IsChatEvent(string eventName)
    {
        if (options.ChatEventNames.Any(
                candidate => string.Equals(candidate, eventName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return options.UseContainsFallback
            && eventName.Contains("chat", StringComparison.OrdinalIgnoreCase);
    }
}
