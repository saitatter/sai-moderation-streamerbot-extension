using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotChatEventHandlerTests
{
    [Fact]
    public async Task ReturnsNullWhenMapperCannotParseEvent()
    {
        var mapper = new FakeMapper(false, null);
        var service = BuildBridgeService();
        var handler = new StreamerBotChatEventHandler(mapper, service);

        var result = await handler.HandleRawEventAsync("{bad json}");

        Assert.Null(result);
    }

    [Fact]
    public async Task ForwardsMappedEventToBridgeService()
    {
        var eventToMap = new ChatEvent(
            "m-1",
            "YouTube",
            "chan-1",
            "u-1",
            "alice",
            "hello",
            DateTimeOffset.UtcNow);
        var mapper = new FakeMapper(true, eventToMap);
        var service = BuildBridgeService();
        var handler = new StreamerBotChatEventHandler(mapper, service);

        var result = await handler.HandleRawEventAsync("{}");

        Assert.NotNull(result);
        Assert.Equal(ModerationVerdict.Allow, result!.Verdict);
    }

    private static ModerationBridgeService BuildBridgeService()
    {
        return new ModerationBridgeService(
            new FakeBackendClient(),
            new FakePublisher(),
            new ModerationBridgeOptions { ForwardFlagsToOverlay = false });
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
                0.99,
                "safe",
                "ok",
                5));
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
    }
}

