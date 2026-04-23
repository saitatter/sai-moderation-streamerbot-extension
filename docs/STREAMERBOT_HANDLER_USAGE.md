# Streamer.bot Handler Usage

`StreamerBotEventCallbackAdapter` is the callback adapter you plug into Streamer.bot SDK events.

`StreamerBotRuntimeBridge` is the runtime entrypoint for raw chat payloads that passed callback filtering.

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

1. Streamer.bot callback invokes `StreamerBotEventCallbackAdapter.HandleIncomingEventAsync(eventName, rawJson)`
2. Adapter filters non-chat callbacks using `StreamerBotRuntimeOptions`
3. `StreamerBotRuntimeBridge.ProcessRawChatEventAsync(rawJson)`
4. `StreamerBotChatEventHandler.HandleRawEventAsync(rawJson)`
5. Mapper parses and normalizes to `ChatEvent`
6. `ModerationBridgeService` calls backend moderation
7. Dashboard event is always published
8. Overlay event is published based on verdict and bridge options

## Callback Wiring Example

```csharp
await eventCallbackAdapter.HandleIncomingEventAsync(callback.EventName, callback.RawData, cancellationToken);
```

## Logging

- The handler and bridge services emit structured logs and include `messageId` scope for correlation.
- Ignored payloads (invalid shape or unsupported event type) are logged at `Debug` level.
