using System.Net;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class HttpStreamerBotChannelPublisherTests
{
    [Fact]
    public async Task PublishesPayloadToChannelEndpointWithToken()
    {
        var handler = new FakeHandler((request) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("http://localhost:8787/v1/events/dashboard", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("token-123", request.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var client = new HttpClient(handler);
        var publisher = new HttpStreamerBotChannelPublisher(
            client,
            new HttpChannelPublisherOptions
            {
                BaseUrl = "http://localhost:8787",
                EventsBasePath = "/v1/events",
                ApiToken = "token-123"
            });

        await publisher.PublishAsync("dashboard", "{\"eventType\":\"moderation.result\"}");
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
