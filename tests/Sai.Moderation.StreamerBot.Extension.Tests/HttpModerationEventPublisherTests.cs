using System.Net;
using System.Text;
using System.Text.Json;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class HttpModerationEventPublisherTests
{
    [Fact]
    public async Task PublishesDashboardAndOverlayEventsToConfiguredPaths()
    {
        HttpRequestMessage? first = null;
        HttpRequestMessage? second = null;

        var handler = new SequenceHttpMessageHandler(
            request =>
            {
                first = CloneRequest(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            },
            request =>
            {
                second = CloneRequest(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            });

        var publisher = BuildPublisher(handler);
        var chatEvent = BuildChatEvent();
        var result = BuildResult(ModerationVerdict.Flag);

        await publisher.PublishDashboardEventAsync(chatEvent, result, CancellationToken.None);
        await publisher.PublishOverlayEventAsync(chatEvent, result, CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("http://localhost:8787/v1/events/dashboard", first!.RequestUri!.ToString());
        Assert.Equal("http://localhost:8787/v1/events/overlay", second!.RequestUri!.ToString());

        var firstBody = await first.Content!.ReadAsStringAsync();
        var secondBody = await second.Content!.ReadAsStringAsync();
        var dashboard = JsonDocument.Parse(firstBody).RootElement;
        var overlay = JsonDocument.Parse(secondBody).RootElement;

        Assert.Equal("moderation.result", dashboard.GetProperty("eventType").GetString());
        Assert.Equal("flag", dashboard.GetProperty("verdict").GetString());
        Assert.Equal("overlay.message", overlay.GetProperty("eventType").GetString());
        Assert.Equal("flag", overlay.GetProperty("verdict").GetString());
    }

    [Fact]
    public async Task ThrowsOnPublishFailure()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("""{"error":"temporary"}""", Encoding.UTF8, "application/json")
            }));
        var publisher = BuildPublisher(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            publisher.PublishDashboardEventAsync(BuildChatEvent(), BuildResult(ModerationVerdict.Allow), CancellationToken.None));
    }

    private static HttpModerationEventPublisher BuildPublisher(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = new ModerationEventPublisherOptions
        {
            BaseUrl = "http://localhost:8787",
            DashboardPath = "/v1/events/dashboard",
            OverlayPath = "/v1/events/overlay",
            RequestTimeoutMs = 500,
        };
        return new HttpModerationEventPublisher(httpClient, options);
    }

    private static ChatEvent BuildChatEvent()
    {
        return new ChatEvent(
            "m-1",
            "Twitch",
            "chan-1",
            "u-1",
            "alice",
            "hello world",
            DateTimeOffset.UtcNow,
            ["mod"]);
    }

    private static ModerationResult BuildResult(ModerationVerdict verdict)
    {
        return new ModerationResult("m-1", verdict, 0.75, "toxicity", "review", 32);
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            clone.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        return clone;
    }

    private sealed class SequenceHttpMessageHandler(
        params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] steps) : HttpMessageHandler
    {
        private int _index;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var current = Math.Min(_index, steps.Length - 1);
            _index += 1;
            return steps[current](request);
        }
    }
}

