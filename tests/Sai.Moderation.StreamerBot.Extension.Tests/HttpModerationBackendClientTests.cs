using System.Net;
using System.Text;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class HttpModerationBackendClientTests
{
    [Fact]
    public async Task ReturnsParsedModerationResultOnSuccess()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = BuildJsonContent("""
                    {
                      "verdict": "allow",
                      "confidence": 0.92,
                      "category": "safe",
                      "reason": "ok",
                      "latencyMs": 34
                    }
                    """)
            }));
        var client = BuildClient(handler);

        var result = await client.ModerateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal(ModerationVerdict.Allow, result.Verdict);
        Assert.Equal(0.92, result.Confidence);
        Assert.Equal("safe", result.Category);
        Assert.Equal("ok", result.Reason);
        Assert.Equal(34, result.LatencyMs);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RetriesOnTransientStatusCode()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = BuildJsonContent("""{"error":"temporary"}""")
            }),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = BuildJsonContent("""
                    {
                      "verdict": "flag",
                      "confidence": 0.71,
                      "category": "toxicity",
                      "reason": "review",
                      "latencyMs": 55
                    }
                    """)
            }));
        var client = BuildClient(handler);

        var result = await client.ModerateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal(ModerationVerdict.Flag, result.Verdict);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task RetriesOnTimeoutThenSucceeds()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => throw new OperationCanceledException("timeout"),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = BuildJsonContent("""
                    {
                      "verdict": "block",
                      "confidence": 0.99,
                      "category": "abuse",
                      "reason": "blocked",
                      "latencyMs": 19
                    }
                    """)
            }));
        var client = BuildClient(handler);

        var result = await client.ModerateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal(ModerationVerdict.Block, result.Verdict);
        Assert.Equal(2, handler.Calls);
    }

    private static HttpModerationBackendClient BuildClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = new ModerationBackendClientOptions
        {
            BaseUrl = "http://localhost:8787",
            ModeratePath = "/v1/moderate",
            RequestTimeoutMs = 200,
            MaxAttempts = 3,
            RetryDelayMs = 1
        };
        return new HttpModerationBackendClient(httpClient, options);
    }

    private static ModerationRequest BuildRequest()
    {
        return new ModerationRequest(
            "msg-1",
            "Twitch",
            "chan-1",
            "user-1",
            "alice",
            "hello",
            DateTimeOffset.UtcNow);
    }

    private static StringContent BuildJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class SequenceHttpMessageHandler(
        params Func<HttpRequestMessage, Task<HttpResponseMessage>>[] steps) : HttpMessageHandler
    {
        private int _index;
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls += 1;
            var current = Math.Min(_index, steps.Length - 1);
            _index += 1;
            return steps[current](request);
        }
    }
}

