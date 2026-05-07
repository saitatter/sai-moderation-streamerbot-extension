# Streamer.bot Handler Usage

`StreamerBotCallbackReceiver` is the entrypoint you plug into Streamer.bot SDK callbacks.

`StreamerBotEventCallbackAdapter` is the filtering/dispatch layer for callback event names.

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

1. Streamer.bot callback invokes `StreamerBotCallbackReceiver.ReceiveAsync(event)`
2. Receiver forwards to `StreamerBotEventCallbackAdapter.HandleIncomingEventAsync(eventName, rawJson)`
3. Adapter filters non-chat callbacks using `StreamerBotRuntimeOptions`
4. `StreamerBotRuntimeBridge.ProcessRawChatEventAsync(rawJson)`
5. `StreamerBotChatEventHandler.HandleRawEventAsync(rawJson)`
6. Mapper parses and normalizes to `ChatEvent`
7. `ModerationBridgeService` calls backend moderation
8. Dashboard event is always published
9. Overlay event is published based on verdict and bridge options

## Callback Wiring Example

```csharp
var callbackEvent = new StreamerBotCallbackEvent(
    callback.EventName,
    callback.RawData,
    DateTimeOffset.UtcNow);

await callbackReceiver.ReceiveAsync(callbackEvent, cancellationToken);
```

## Logging

- The handler and bridge services emit structured logs and include `messageId` scope for correlation.
- Ignored payloads (invalid shape or unsupported event type) are logged at `Debug` level.
