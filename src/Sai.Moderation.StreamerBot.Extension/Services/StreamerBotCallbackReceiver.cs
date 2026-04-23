using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotCallbackReceiver(
    IStreamerBotEventCallbackAdapter callbackAdapter,
    ILogger<StreamerBotCallbackReceiver> logger) : IStreamerBotCallbackReceiver
{
    public async Task<bool> ReceiveAsync(
        StreamerBotCallbackEvent callbackEvent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callbackEvent.EventName))
        {
            logger.LogDebug("Ignored callback with empty event name.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(callbackEvent.RawJson))
        {
            logger.LogDebug(
                "Ignored callback {EventName} because payload is empty.",
                callbackEvent.EventName);
            return false;
        }

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["eventName"] = callbackEvent.EventName,
            ["receivedAt"] = callbackEvent.ReceivedAt
        });

        logger.LogDebug("Received Streamer.bot callback event.");
        return await callbackAdapter.HandleIncomingEventAsync(
            callbackEvent.EventName,
            callbackEvent.RawJson,
            cancellationToken);
    }
}
