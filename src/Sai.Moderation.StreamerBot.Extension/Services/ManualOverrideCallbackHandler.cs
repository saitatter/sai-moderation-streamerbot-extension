using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class ManualOverrideCallbackHandler(
    IManualOverrideService manualOverrideService,
    ILogger<ManualOverrideCallbackHandler> logger)
{
    public async Task<ModerationResult?> HandleRawOverrideAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (!TryParse(rawJson, out var request) || request is null)
        {
            logger.LogDebug("Ignored manual override callback due to invalid payload.");
            return null;
        }

        return await manualOverrideService.ApplyOverrideAsync(request, cancellationToken);
    }

    private static bool TryParse(string rawJson, out ManualOverrideRequest? request)
    {
        request = null;
        if (string.IsNullOrWhiteSpace(rawJson)) return false;

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var messageId = GetString(root, "messageId");
            var actionValue = GetString(root, "action");
            var operatorId = GetString(root, "operatorId") ?? GetString(root, "operator");
            var reason = GetString(root, "reason");

            if (string.IsNullOrWhiteSpace(messageId)
                || string.IsNullOrWhiteSpace(actionValue)
                || string.IsNullOrWhiteSpace(operatorId)
                || string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            if (!Enum.TryParse<ManualOverrideAction>(actionValue, true, out var action))
            {
                return false;
            }

            request = new ManualOverrideRequest(
                messageId,
                action,
                operatorId,
                reason,
                DateTimeOffset.UtcNow);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)) return null;
        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
