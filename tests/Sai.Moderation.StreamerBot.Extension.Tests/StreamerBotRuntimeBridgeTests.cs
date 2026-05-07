using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotRuntimeBridgeTests
{
    [Fact]
    public async Task ReturnsFalseWhenRawPayloadIsIgnored()
    {
        var handler = BuildHandler(new FakeMapper(false, null));
        var bridge = new StreamerBotRuntimeBridge(handler, NullLogger<StreamerBotRuntimeBridge>.Instance);

        var processed = await bridge.ProcessRawChatEventAsync("{bad json}");

        Assert.False(processed);
    }

    [Fact]
    public async Task ReturnsTrueWhenRawPayloadProducesModerationResult()
    {
        var mappedEvent = new ChatEvent(
            "m-1",
            "Twitch",
            "c-1",
            "u-1",
            "alice",
            "hello",
            DateTimeOffset.UtcNow);
        var handler = BuildHandler(new FakeMapper(true, mappedEvent));
        var bridge = new StreamerBotRuntimeBridge(handler, NullLogger<StreamerBotRuntimeBridge>.Instance);

        var processed = await bridge.ProcessRawChatEventAsync("{}");

        Assert.True(processed);
    }

    private static StreamerBotChatEventHandler BuildHandler(IStreamerBotChatEventMapper mapper)
    {
        var moderationService = new ModerationBridgeService(
            new FakeBackendClient(),
            new FakePublisher(),
            new InMemoryModerationDecisionStore(),
            new ModerationBridgeOptions(),
            NullLogger<ModerationBridgeService>.Instance);

        return new StreamerBotChatEventHandler(
            mapper,
            moderationService,
            NullLogger<StreamerBotChatEventHandler>.Instance);
    }

    private sealed class FakeMapper(bool shouldMap, ChatEvent? mappedEvent) : IStreamerBotChatEventMapper
    {
        public bool TryMap(string rawJson, out ChatEvent? chatEvent)
        {
            chatEvent = mappedEvent;
            return shouldMap;
        }
    }

    private sealed class FakeBackendClient : IModerationBackendClient
    {
        public Task<ModerationResult> ModerateAsync(
            ModerationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ModerationResult(
                request.MessageId,
                ModerationVerdict.Allow,
                0.97,
                "safe",
                "ok",
                7));
        }
    }

    private sealed class FakePublisher : IModerationEventPublisher
    {
        public Task PublishDashboardEventAsync(
            ChatEvent chatEvent,
            ModerationResult result,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishOverlayEventAsync(
            ChatEvent chatEvent,
            ModerationResult result,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishManualOverrideEventAsync(
            ChatEvent chatEvent,
            ModerationResult result,
            ManualOverrideRequest request,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
