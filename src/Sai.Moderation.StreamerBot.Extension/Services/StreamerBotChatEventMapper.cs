using System.Text.Json;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class StreamerBotChatEventMapper : IStreamerBotChatEventMapper
{
    public bool TryMap(string rawJson, out ChatEvent? chatEvent)
    {
        chatEvent = null;
        if (string.IsNullOrWhiteSpace(rawJson)) return false;

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            var source = GetString(root, "event", "source");
            if (string.IsNullOrWhiteSpace(source)) return false;

            var userName = GetString(root, "data", "user", "name");
            var text = GetMessageText(root);
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(text)) return false;

            var messageId =
                GetString(root, "data", "messageId")
                ?? GetString(root, "data", "message", "id")
                ?? Guid.NewGuid().ToString("N");

            var channelId =
                GetString(root, "data", "channelId")
                ?? GetString(root, "data", "channel", "id")
                ?? "unknown-channel";

            var userId = GetString(root, "data", "user", "id") ?? userName;
            var badges = GetBadgeUrls(root);

            chatEvent = new ChatEvent(
                messageId,
                source,
                channelId,
                userId,
                userName,
                text,
                DateTimeOffset.UtcNow,
                badges);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetMessageText(JsonElement root)
    {
        var direct = GetString(root, "data", "message");
        if (!string.IsNullOrWhiteSpace(direct)) return direct;

        var nested = GetString(root, "data", "message", "message");
        if (!string.IsNullOrWhiteSpace(nested)) return nested;

        return null;
    }

    private static IReadOnlyList<string> GetBadgeUrls(JsonElement root)
    {
        if (!TryGet(root, out var badgesElement, "data", "user", "badges")) return [];
        if (badgesElement.ValueKind != JsonValueKind.Array) return [];

        var urls = new List<string>();
        foreach (var badge in badgesElement.EnumerateArray())
        {
            if (badge.ValueKind != JsonValueKind.Object) continue;
            if (badge.TryGetProperty("imageUrl", out var imageUrl)
                && imageUrl.ValueKind == JsonValueKind.String)
            {
                var value = imageUrl.GetString();
                if (!string.IsNullOrWhiteSpace(value)) urls.Add(value);
            }
        }

        return urls;
    }

    private static string? GetString(JsonElement root, params string[] path)
    {
        if (!TryGet(root, out var element, path)) return null;
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static bool TryGet(JsonElement root, out JsonElement current, params string[] path)
    {
        current = root;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object) return false;
            if (!current.TryGetProperty(key, out current)) return false;
        }
        return true;
    }
}

