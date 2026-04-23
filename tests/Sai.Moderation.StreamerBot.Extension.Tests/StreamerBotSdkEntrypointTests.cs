using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotSdkEntrypointTests
{
    [Fact]
    public async Task ForwardsStringPayloadWithoutReserializing()
    {
        var receiver = new FakeReceiver(true);
        var entrypoint = new StreamerBotSdkEntrypoint(
            receiver,
            NullLogger<StreamerBotSdkEntrypoint>.Instance);

        var handled = await entrypoint.HandleSdkEventAsync("TwitchChatMessage", "{\"msg\":\"hello\"}");

        Assert.True(handled);
        Assert.Equal(1, receiver.CallCount);
        Assert.Equal("{\"msg\":\"hello\"}", receiver.LastEvent!.RawJson);
    }

    [Fact]
    public async Task SerializesObjectPayloadToJson()
    {
        var receiver = new FakeReceiver(true);
        var entrypoint = new StreamerBotSdkEntrypoint(
            receiver,
            NullLogger<StreamerBotSdkEntrypoint>.Instance);

        var payload = new { message = "hello", user = "alice" };
        await entrypoint.HandleSdkEventAsync("YouTubeMessage", payload);

        Assert.NotNull(receiver.LastEvent);
        Assert.Contains("\"message\":\"hello\"", receiver.LastEvent!.RawJson);
        Assert.Contains("\"user\":\"alice\"", receiver.LastEvent.RawJson);
    }

    [Fact]
    public async Task UsesRawTextForJsonElementPayload()
    {
        var receiver = new FakeReceiver(true);
        var entrypoint = new StreamerBotSdkEntrypoint(
            receiver,
            NullLogger<StreamerBotSdkEntrypoint>.Instance);
        using var document = JsonDocument.Parse("{\"a\":1}");

        await entrypoint.HandleSdkEventAsync("KickChatMessage", document.RootElement);

        Assert.NotNull(receiver.LastEvent);
        Assert.Equal("{\"a\":1}", receiver.LastEvent!.RawJson);
    }

    [Fact]
    public async Task ReturnsFalseWhenPayloadIsNull()
    {
        var receiver = new FakeReceiver(true);
        var entrypoint = new StreamerBotSdkEntrypoint(
            receiver,
            NullLogger<StreamerBotSdkEntrypoint>.Instance);

        var handled = await entrypoint.HandleSdkEventAsync("TwitchChatMessage", null);

        Assert.False(handled);
        Assert.Equal(0, receiver.CallCount);
    }

    private sealed class FakeReceiver(bool result) : IStreamerBotCallbackReceiver
    {
        public int CallCount { get; private set; }
        public StreamerBotCallbackEvent? LastEvent { get; private set; }

        public Task<bool> ReceiveAsync(
            StreamerBotCallbackEvent callbackEvent,
            CancellationToken cancellationToken = default)
        {
            CallCount += 1;
            LastEvent = callbackEvent;
            return Task.FromResult(result);
        }
    }
}
