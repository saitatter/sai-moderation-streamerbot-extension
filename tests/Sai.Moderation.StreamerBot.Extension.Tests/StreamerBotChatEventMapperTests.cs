using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class StreamerBotChatEventMapperTests
{
    [Fact]
    public void MapsTwitchPayloadToChatEvent()
    {
        var mapper = new StreamerBotChatEventMapper();
        var payload = """
            {
              "event": { "source": "Twitch" },
              "data": {
                "messageId": "msg-123",
                "channel": { "id": "chan-1" },
                "user": {
                  "id": "user-8",
                  "name": "alice",
                  "badges": [{ "imageUrl": "https://example.com/mod.png" }]
                },
                "message": { "message": "hello world" }
              }
            }
            """;

        var mapped = mapper.TryMap(payload, out var chatEvent);

        Assert.True(mapped);
        Assert.NotNull(chatEvent);
        Assert.Equal("msg-123", chatEvent!.MessageId);
        Assert.Equal("Twitch", chatEvent.Platform);
        Assert.Equal("chan-1", chatEvent.ChannelId);
        Assert.Equal("user-8", chatEvent.UserId);
        Assert.Equal("alice", chatEvent.Username);
        Assert.Equal("hello world", chatEvent.Text);
        Assert.Single(chatEvent.Badges!);
    }

    [Fact]
    public void ReturnsFalseForInvalidPayload()
    {
        var mapper = new StreamerBotChatEventMapper();

        var mapped = mapper.TryMap("{\"event\":{},\"data\":{}}", out var chatEvent);

        Assert.False(mapped);
        Assert.Null(chatEvent);
    }

    [Fact]
    public void GeneratesStableMessageIdWhenPayloadOmitsMessageId()
    {
        var mapper = new StreamerBotChatEventMapper();
        var payload = """
            {
              "event": { "source": "Twitch" },
              "data": {
                "user": { "id": "user-8", "name": "alice" },
                "message": "hello world"
              }
            }
            """;

        var firstMapped = mapper.TryMap(payload, out var firstEvent);
        var secondMapped = mapper.TryMap(payload, out var secondEvent);

        Assert.True(firstMapped);
        Assert.True(secondMapped);
        Assert.NotNull(firstEvent);
        Assert.NotNull(secondEvent);
        Assert.Equal(firstEvent!.MessageId, secondEvent!.MessageId);
        Assert.StartsWith("generated-", firstEvent.MessageId);
    }

    [Fact]
    public void UsesPayloadTimestampWhenPresent()
    {
        var mapper = new StreamerBotChatEventMapper();
        var payload = """
            {
              "event": { "source": "Twitch" },
              "data": {
                "timestamp": "2026-04-23T09:13:11Z",
                "user": { "id": "user-8", "name": "alice" },
                "message": "hello world"
              }
            }
            """;

        var mapped = mapper.TryMap(payload, out var chatEvent);

        Assert.True(mapped);
        Assert.NotNull(chatEvent);
        Assert.Equal(DateTimeOffset.Parse("2026-04-23T09:13:11Z"), chatEvent!.ReceivedAt);
    }
}
