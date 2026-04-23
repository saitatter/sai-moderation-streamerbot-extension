# Streamer.bot Handler Usage

`StreamerBotRuntimeBridge` is the runtime entrypoint for raw event payloads received from Streamer.bot.

`StreamerBotChatEventHandler` remains the inner pipeline component that maps payloads and forwards them to moderation.

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

1. `StreamerBotRuntimeBridge.ProcessRawChatEventAsync(rawJson)`
2. `StreamerBotChatEventHandler.HandleRawEventAsync(rawJson)`
3. Mapper parses and normalizes to `ChatEvent`
4. `ModerationBridgeService` calls backend moderation
5. Dashboard event is always published
6. Overlay event is published based on verdict and bridge options

## Logging

- The handler and bridge services emit structured logs and include `messageId` scope for correlation.
- Ignored payloads (invalid shape or unsupported event type) are logged at `Debug` level.
