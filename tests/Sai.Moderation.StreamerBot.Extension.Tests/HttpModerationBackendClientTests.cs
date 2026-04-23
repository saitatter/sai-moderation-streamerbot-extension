using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class HttpModerationBackendClientTests
{
    [Fact]
    public async Task SendsAuthorizationHeaderWhenTokenConfigured()
    {
        var handler = new FakeHandler((request) =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("token-123", request.Headers.Authorization?.Parameter);
            var result = new ModerationResult("m-1", ModerationVerdict.Allow, 0.9, "safe", "ok", 12);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(result)
            };
        });
        var client = new HttpClient(handler);
        var service = new HttpModerationBackendClient(
            client,
            new HttpModerationBackendClientOptions
            {
                BaseUrl = "http://localhost:8787",
                ApiToken = "token-123",
                MaxAttempts = 1
            },
            NullLogger<HttpModerationBackendClient>.Instance);

        var result = await service.ModerateAsync(
            new ModerationRequest("m-1", "Twitch", "c-1", "u-1", "alice", "hello", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(ModerationVerdict.Allow, result.Verdict);
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
