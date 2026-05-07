using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotEventCallbackAdapterTests
{
    [Fact]
    public async Task IgnoresNonChatEvents()
    {
        var runtimeBridge = new FakeRuntimeBridge(true);
        var adapter = new StreamerBotEventCallbackAdapter(
            runtimeBridge,
            new StreamerBotRuntimeOptions(),
            NullLogger<StreamerBotEventCallbackAdapter>.Instance);

        var handled = await adapter.HandleIncomingEventAsync("RewardRedeemed", "{}");

        Assert.False(handled);
        Assert.Equal(0, runtimeBridge.CallCount);
    }

    [Fact]
    public async Task ForwardsConfiguredChatEventsToRuntimeBridge()
    {
        var runtimeBridge = new FakeRuntimeBridge(true);
        var adapter = new StreamerBotEventCallbackAdapter(
            runtimeBridge,
            new StreamerBotRuntimeOptions(),
            NullLogger<StreamerBotEventCallbackAdapter>.Instance);

        var handled = await adapter.HandleIncomingEventAsync("TwitchChatMessage", "{\"x\":1}");

        Assert.True(handled);
        Assert.Equal(1, runtimeBridge.CallCount);
        Assert.Equal("{\"x\":1}", runtimeBridge.LastPayload);
    }

    [Fact]
    public async Task UsesContainsFallbackWhenEnabled()
    {
        var runtimeBridge = new FakeRuntimeBridge(true);
        var adapter = new StreamerBotEventCallbackAdapter(
            runtimeBridge,
            new StreamerBotRuntimeOptions { ChatEventNames = ["SomethingElse"], UseContainsFallback = true },
            NullLogger<StreamerBotEventCallbackAdapter>.Instance);

        var handled = await adapter.HandleIncomingEventAsync("KickChatInbound", "{\"m\":\"hello\"}");

        Assert.True(handled);
        Assert.Equal(1, runtimeBridge.CallCount);
    }

    [Fact]
    public async Task SkipsContainsFallbackWhenDisabled()
    {
        var runtimeBridge = new FakeRuntimeBridge(true);
        var adapter = new StreamerBotEventCallbackAdapter(
            runtimeBridge,
            new StreamerBotRuntimeOptions { ChatEventNames = ["SomethingElse"], UseContainsFallback = false },
            NullLogger<StreamerBotEventCallbackAdapter>.Instance);

        var handled = await adapter.HandleIncomingEventAsync("KickChatInbound", "{\"m\":\"hello\"}");

        Assert.False(handled);
        Assert.Equal(0, runtimeBridge.CallCount);
    }

    private sealed class FakeRuntimeBridge(bool returnValue) : IStreamerBotRuntimeBridge
    {
        public int CallCount { get; private set; }
        public string? LastPayload { get; private set; }

        public Task<bool> ProcessRawChatEventAsync(
            string rawJson,
            CancellationToken cancellationToken = default)
        {
            CallCount += 1;
            LastPayload = rawJson;
            return Task.FromResult(returnValue);
        }
    }
}
