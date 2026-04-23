using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class ManualOverrideServiceTests
{
    [Fact]
    public async Task ReturnsNullWhenMessageIsUnknown()
    {
        var publisher = new FakePublisher();
        var service = new ManualOverrideService(
            new InMemoryModerationDecisionStore(),
            publisher,
            new ModerationBridgeOptions(),
            NullLogger<ManualOverrideService>.Instance);

        var result = await service.ApplyOverrideAsync(
            new ManualOverrideRequest("missing", ManualOverrideAction.Block, "mod-1", "bad", DateTimeOffset.UtcNow));

        Assert.Null(result);
        Assert.Equal(0, publisher.ManualOverridePublishedCount);
    }

    [Fact]
    public async Task ApproveOverridePublishesManualEventAndOverlay()
    {
        var store = new InMemoryModerationDecisionStore();
        var chatEvent = BuildChatEvent();
        await store.SaveAsync(
            chatEvent,
            new ModerationResult(chatEvent.MessageId, ModerationVerdict.Block, 0.7, "toxicity", "model", 10));
        var publisher = new FakePublisher();
        var service = new ManualOverrideService(
            store,
            publisher,
            new ModerationBridgeOptions(),
            NullLogger<ManualOverrideService>.Instance);

        var result = await service.ApplyOverrideAsync(
            new ManualOverrideRequest(chatEvent.MessageId, ManualOverrideAction.Approve, "mod-1", "context", DateTimeOffset.UtcNow));

        Assert.NotNull(result);
        Assert.Equal(ModerationVerdict.Allow, result!.Verdict);
        Assert.Equal(1, publisher.ManualOverridePublishedCount);
        Assert.Equal(1, publisher.OverlayPublishedCount);
    }

    [Fact]
    public async Task BlockOverrideDoesNotPublishOverlay()
    {
        var store = new InMemoryModerationDecisionStore();
        var chatEvent = BuildChatEvent();
        await store.SaveAsync(
            chatEvent,
            new ModerationResult(chatEvent.MessageId, ModerationVerdict.Allow, 0.98, "safe", "model", 10));
        var publisher = new FakePublisher();
        var service = new ManualOverrideService(
            store,
            publisher,
            new ModerationBridgeOptions(),
            NullLogger<ManualOverrideService>.Instance);

        var result = await service.ApplyOverrideAsync(
            new ManualOverrideRequest(chatEvent.MessageId, ManualOverrideAction.Block, "mod-2", "manual block", DateTimeOffset.UtcNow));

        Assert.NotNull(result);
        Assert.Equal(ModerationVerdict.Block, result!.Verdict);
        Assert.Equal(1, publisher.ManualOverridePublishedCount);
        Assert.Equal(0, publisher.OverlayPublishedCount);
    }

    private static ChatEvent BuildChatEvent()
    {
        return new ChatEvent("m-1", "Twitch", "c-1", "u-1", "alice", "hello", DateTimeOffset.UtcNow);
    }

    private sealed class FakePublisher : IModerationEventPublisher
    {
        public int OverlayPublishedCount { get; private set; }
        public int ManualOverridePublishedCount { get; private set; }

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
            OverlayPublishedCount += 1;
            return Task.CompletedTask;
        }

        public Task PublishManualOverrideEventAsync(
            ChatEvent chatEvent,
            ModerationResult result,
            ManualOverrideRequest request,
            CancellationToken cancellationToken)
        {
            ManualOverridePublishedCount += 1;
            return Task.CompletedTask;
        }
    }
}
