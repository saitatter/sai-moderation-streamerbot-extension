using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotSdkEntrypoint(
    IStreamerBotCallbackReceiver callbackReceiver,
    ILogger<StreamerBotSdkEntrypoint> logger) : IStreamerBotSdkEntrypoint
{
    public async Task<bool> HandleSdkEventAsync(
        string eventName,
        object? payload,
        CancellationToken cancellationToken = default)
    {
        var rawJson = NormalizePayload(payload);
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            logger.LogDebug(
                "Ignored SDK event {EventName} because payload could not be normalized to JSON.",
                eventName);
            return false;
        }

        var callbackEvent = new StreamerBotCallbackEvent(
            eventName,
            rawJson,
            DateTimeOffset.UtcNow);

        return await callbackReceiver.ReceiveAsync(callbackEvent, cancellationToken);
    }

    private static string NormalizePayload(object? payload)
    {
        return payload switch
        {
            null => string.Empty,
            string json => json,
            JsonElement jsonElement => jsonElement.GetRawText(),
            _ => JsonSerializer.Serialize(payload)
        };
    }
}
