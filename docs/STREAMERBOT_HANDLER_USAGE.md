# Streamer.bot Handler Usage

`StreamerBotChatEventHandler` is the entrypoint for raw event payloads received from Streamer.bot.

## Expected Input

The handler accepts raw JSON strings and relies on `StreamerBotChatEventMapper` to map them into `ChatEvent`.

Minimal payload shape:

```json
{
  "event": { "source": "Twitch" },
  "data": {
    "messageId": "msg-1",
    "channel": { "id": "chan-1" },
    "user": { "id": "u-1", "name": "alice" },
    "message": { "message": "hello" }
  }
}
```

## Flow

1. `StreamerBotChatEventHandler.HandleRawEventAsync(rawJson)`
2. Mapper parses and normalizes to `ChatEvent`
3. `ModerationBridgeService` calls backend moderation
4. Dashboard event is always published
5. Overlay event is published based on verdict and bridge options

