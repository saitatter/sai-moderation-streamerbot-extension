using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class HttpStreamerBotChannelPublisher(
    HttpClient httpClient,
    HttpChannelPublisherOptions options) : IStreamerBotChannelPublisher
{
    public async Task PublishAsync(
        string channel,
        string payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            throw new ArgumentException("Channel is required.", nameof(channel));
        }

        var endpoint = BuildEventsUri(options.BaseUrl, options.EventsBasePath, channel);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(Math.Max(100, options.TimeoutMs));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(options.ApiToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }

        using var response = await httpClient.SendAsync(request, linkedCts.Token);
        response.EnsureSuccessStatusCode();
    }

    private static Uri BuildEventsUri(string baseUrl, string eventsBasePath, string channel)
    {
        var baseUri = baseUrl.EndsWith('/') ? new Uri(baseUrl) : new Uri(baseUrl + "/");
        var eventsPath = eventsBasePath.Trim('/');
        var normalized = $"{eventsPath}/{channel}";
        return new Uri(baseUri, normalized);
    }
}
