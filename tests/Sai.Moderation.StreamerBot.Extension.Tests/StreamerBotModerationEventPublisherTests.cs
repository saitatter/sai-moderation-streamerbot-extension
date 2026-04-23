using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotModerationEventPublisherTests
{
    [Fact]
    public async Task PublishesDashboardEventToConfiguredChannel()
    {
        var channelPublisher = new FakeChannelPublisher();
        var publisher = BuildPublisher(channelPublisher);
        var chatEvent = BuildChatEvent();
        var result = BuildResult(ModerationVerdict.Flag);

        await publisher.PublishDashboardEventAsync(chatEvent, result, CancellationToken.None);

        Assert.Single(channelPublisher.Messages);
        Assert.Equal("mod.dashboard", channelPublisher.Messages[0].Channel);
        using var json = JsonDocument.Parse(channelPublisher.Messages[0].Payload);
        Assert.Equal("moderation.result", json.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("flag", json.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public async Task PublishesOverlayEventToConfiguredChannel()
    {
        var channelPublisher = new FakeChannelPublisher();
        var publisher = BuildPublisher(channelPublisher);

        await publisher.PublishOverlayEventAsync(BuildChatEvent(), BuildResult(ModerationVerdict.Allow), CancellationToken.None);

        Assert.Single(channelPublisher.Messages);
        Assert.Equal("chat.overlay", channelPublisher.Messages[0].Channel);
        using var json = JsonDocument.Parse(channelPublisher.Messages[0].Payload);
        Assert.Equal("overlay.message", json.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("allow", json.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public async Task PublishesManualOverrideEventOnDashboardChannel()
    {
        var channelPublisher = new FakeChannelPublisher();
        var publisher = BuildPublisher(channelPublisher);
        var request = new ManualOverrideRequest(
            "m-1",
            ManualOverrideAction.FalsePositive,
            "mod-1",
            "context",
            DateTimeOffset.UtcNow);

        await publisher.PublishManualOverrideEventAsync(
            BuildChatEvent(),
            BuildResult(ModerationVerdict.Allow),
            request,
            CancellationToken.None);

        Assert.Single(channelPublisher.Messages);
        Assert.Equal("mod.dashboard", channelPublisher.Messages[0].Channel);
        using var json = JsonDocument.Parse(channelPublisher.Messages[0].Payload);
        Assert.Equal("moderation.override", json.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("falsePositive", json.RootElement.GetProperty("action").GetString());
    }

    private static StreamerBotModerationEventPublisher BuildPublisher(FakeChannelPublisher channelPublisher)
    {
        return new StreamerBotModerationEventPublisher(
            channelPublisher,
            new StreamerBotPublishOptions
            {
                DashboardChannel = "mod.dashboard",
                OverlayChannel = "chat.overlay"
            },
            NullLogger<StreamerBotModerationEventPublisher>.Instance);
    }

    private static ChatEvent BuildChatEvent()
    {
        return new ChatEvent("m-1", "Twitch", "c-1", "u-1", "alice", "hello", DateTimeOffset.UtcNow);
    }

    private static ModerationResult BuildResult(ModerationVerdict verdict)
    {
        return new ModerationResult("m-1", verdict, 0.9, "safe", "ok", 12);
    }

    private sealed class FakeChannelPublisher : IStreamerBotChannelPublisher
    {
        public List<(string Channel, string Payload)> Messages { get; } = [];

        public Task PublishAsync(
            string channel,
            string payload,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((channel, payload));
            return Task.CompletedTask;
        }
    }
}
