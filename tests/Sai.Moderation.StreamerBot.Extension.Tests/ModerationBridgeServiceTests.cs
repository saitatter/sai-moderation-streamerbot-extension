using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class ModerationBridgeServiceTests
{
    [Fact]
    public async Task ForwardsAllowedMessagesToOverlayAndDashboard()
    {
        var backend = new FakeBackendClient(
            new ModerationResult("m-1", ModerationVerdict.Allow, 0.95, "safe", "ok", 22));
        var publisher = new FakePublisher();
        var service = new ModerationBridgeService(
            backend,
            publisher,
            new ModerationBridgeOptions { ForwardFlagsToOverlay = false },
            NullLogger<ModerationBridgeService>.Instance);

        await service.HandleChatEventAsync(BuildChatEvent());

        Assert.Equal(1, publisher.DashboardPublishedCount);
        Assert.Equal(1, publisher.OverlayPublishedCount);
    }

    [Fact]
    public async Task DoesNotForwardFlagsToOverlayWhenOptionDisabled()
    {
        var backend = new FakeBackendClient(
            new ModerationResult("m-1", ModerationVerdict.Flag, 0.81, "toxicity", "review", 48));
        var publisher = new FakePublisher();
        var service = new ModerationBridgeService(
            backend,
            publisher,
            new ModerationBridgeOptions { ForwardFlagsToOverlay = false },
            NullLogger<ModerationBridgeService>.Instance);

        await service.HandleChatEventAsync(BuildChatEvent());

        Assert.Equal(1, publisher.DashboardPublishedCount);
        Assert.Equal(0, publisher.OverlayPublishedCount);
    }

    private static ChatEvent BuildChatEvent()
    {
        return new ChatEvent(
            "m-1",
            "Twitch",
            "c-1",
            "u-1",
            "test_user",
            "hello world",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeBackendClient(ModerationResult result) : IModerationBackendClient
    {
        public Task<ModerationResult> ModerateAsync(
            ModerationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result with { MessageId = request.MessageId });
        }
    }

    private sealed class FakePublisher : IModerationEventPublisher
    {
        public int DashboardPublishedCount { get; private set; }
        public int OverlayPublishedCount { get; private set; }

        public Task PublishDashboardEventAsync(
            ChatEvent chatEvent,
            ModerationResult result,
            CancellationToken cancellationToken)
        {
            DashboardPublishedCount += 1;
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
    }
}
