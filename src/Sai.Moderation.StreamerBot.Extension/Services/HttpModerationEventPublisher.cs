using System.Net.Http.Json;
using System.Text.Json;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class HttpModerationEventPublisher(
    HttpClient httpClient,
    ModerationEventPublisherOptions options) : IModerationEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishDashboardEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var payload = new DashboardEventPayload(
            "moderation.result",
            chatEvent.MessageId,
            chatEvent.Platform,
            chatEvent.ChannelId,
            chatEvent.UserId,
            chatEvent.Username,
            chatEvent.Text,
            result.Verdict.ToString().ToLowerInvariant(),
            result.Confidence,
            result.Category,
            result.Reason,
            result.LatencyMs,
            chatEvent.ReceivedAt);

        return PostAsync(options.DashboardPath, payload, cancellationToken);
    }

    public Task PublishOverlayEventAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken)
    {
        var payload = new OverlayEventPayload(
            "overlay.message",
            chatEvent.MessageId,
            chatEvent.Platform,
            chatEvent.ChannelId,
            chatEvent.Username,
            chatEvent.Text,
            result.Verdict.ToString().ToLowerInvariant(),
            chatEvent.Badges ?? []);

        return PostAsync(options.OverlayPath, payload, cancellationToken);
    }

    private async Task PostAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(100, options.RequestTimeoutMs));

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(options.BaseUrl, path))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        using var response = await httpClient.SendAsync(request, timeoutCts.Token);
        if (response.IsSuccessStatusCode) return;

        var statusCode = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        throw new HttpRequestException(
            $"Event publish failed with HTTP {statusCode}: {body}",
            null,
            response.StatusCode);
    }

    private static Uri BuildUri(string baseUrl, string path)
    {
        var baseUri = baseUrl.EndsWith('/')
            ? new Uri(baseUrl)
            : new Uri($"{baseUrl}/");
        var relativePath = path.TrimStart('/');
        return new Uri(baseUri, relativePath);
    }

    private sealed record DashboardEventPayload(
        string EventType,
        string MessageId,
        string Platform,
        string ChannelId,
        string UserId,
        string Username,
        string Text,
        string Verdict,
        double Confidence,
        string Category,
        string Reason,
        int LatencyMs,
        DateTimeOffset ReceivedAt);

    private sealed record OverlayEventPayload(
        string EventType,
        string MessageId,
        string Platform,
        string ChannelId,
        string Username,
        string Text,
        string Verdict,
        IReadOnlyList<string> Badges);
}

