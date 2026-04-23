using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotCallbackReceiverTests
{
    [Fact]
    public async Task ReturnsFalseWhenEventNameIsMissing()
    {
        var adapter = new FakeAdapter(true);
        var receiver = new StreamerBotCallbackReceiver(
            adapter,
            NullLogger<StreamerBotCallbackReceiver>.Instance);

        var handled = await receiver.ReceiveAsync(new StreamerBotCallbackEvent("", "{}", DateTimeOffset.UtcNow));

        Assert.False(handled);
        Assert.Equal(0, adapter.CallCount);
    }

    [Fact]
    public async Task ForwardsValidCallbackToAdapter()
    {
        var adapter = new FakeAdapter(true);
        var receiver = new StreamerBotCallbackReceiver(
            adapter,
            NullLogger<StreamerBotCallbackReceiver>.Instance);
        var callback = new StreamerBotCallbackEvent(
            "TwitchChatMessage",
            "{\"message\":\"hi\"}",
            DateTimeOffset.Parse("2026-04-23T12:00:00Z"));

        var handled = await receiver.ReceiveAsync(callback);

        Assert.True(handled);
        Assert.Equal(1, adapter.CallCount);
        Assert.Equal("TwitchChatMessage", adapter.LastEventName);
        Assert.Equal("{\"message\":\"hi\"}", adapter.LastPayload);
    }

    private sealed class FakeAdapter(bool result) : IStreamerBotEventCallbackAdapter
    {
        public int CallCount { get; private set; }
        public string? LastEventName { get; private set; }
        public string? LastPayload { get; private set; }

        public Task<bool> HandleIncomingEventAsync(
            string eventName,
            string rawJson,
            CancellationToken cancellationToken = default)
        {
            CallCount += 1;
            LastEventName = eventName;
            LastPayload = rawJson;
            return Task.FromResult(result);
        }
    }
}
