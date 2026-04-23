# Streamer.bot SDK Wiring Example

This document shows a practical way to connect Streamer.bot runtime callbacks to the moderation pipeline from this repository.

## Goal

Wire incoming callback data from Streamer.bot into:
1. `StreamerBotCallbackReceiver`
2. `StreamerBotEventCallbackAdapter`
3. `StreamerBotRuntimeBridge`
4. `StreamerBotChatEventHandler`
5. `ModerationBridgeService`

## Reference Registration (DI)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

services.AddSingleton(new ModerationBridgeOptions
{
    ForwardFlagsToOverlay = true
});

services.AddSingleton(new StreamerBotRuntimeOptions
{
    ChatEventNames = new[] { "TwitchChatMessage", "YouTubeMessage", "KickChatMessage" },
    UseContainsFallback = true
});

services.AddSingleton<IStreamerBotChatEventMapper, StreamerBotChatEventMapper>();
services.AddSingleton<IModerationBackendClient, HttpModerationBackendClient>();      // existing implementation
services.AddSingleton<IModerationEventPublisher, StreamerBotModerationEventPublisher>(); // existing implementation

services.AddSingleton<ModerationBridgeService>();
services.AddSingleton<StreamerBotChatEventHandler>();
services.AddSingleton<IStreamerBotRuntimeBridge, StreamerBotRuntimeBridge>();
services.AddSingleton<IStreamerBotEventCallbackAdapter, StreamerBotEventCallbackAdapter>();
services.AddSingleton<IStreamerBotCallbackReceiver, StreamerBotCallbackReceiver>();

var provider = services.BuildServiceProvider();
var callbackReceiver = provider.GetRequiredService<IStreamerBotCallbackReceiver>();
```

## Callback Hook Example

Use your real Streamer.bot callback source and route it to the receiver.

```csharp
using Sai.Moderation.StreamerBot.Extension.Models;

// Pseudo-signature. Replace with the real SDK event handler signature.
async Task OnStreamerBotEventAsync(string eventName, string rawJson, CancellationToken cancellationToken)
{
    var callbackEvent = new StreamerBotCallbackEvent(
        eventName,
        rawJson,
        DateTimeOffset.UtcNow);

    await callbackReceiver.ReceiveAsync(callbackEvent, cancellationToken);
}
```

## Adapter Rules

- Non-chat callbacks are ignored.
- Chat callbacks are selected by:
  - exact match from `StreamerBotRuntimeOptions.ChatEventNames`, or
  - contains `"chat"` when `UseContainsFallback` is enabled.

## What To Replace For Real SDK

1. Replace pseudo callback signature with the real Streamer.bot event args.
2. Set `eventName` from SDK field (for example callback type or event ID name).
3. Serialize SDK payload to raw JSON string if not already JSON.
4. Pass cancellation token from host runtime if available.

## Minimal Non-DI Example

If your extension host does not use `IServiceCollection`, instantiate directly:

```csharp
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var mapper = new StreamerBotChatEventMapper();
var backend = new HttpModerationBackendClient(/* ... */);
var publisher = new StreamerBotModerationEventPublisher(/* ... */);
var bridgeOptions = new ModerationBridgeOptions { ForwardFlagsToOverlay = true };
var runtimeOptions = new StreamerBotRuntimeOptions();

var moderationBridge = new ModerationBridgeService(
    backend,
    publisher,
    bridgeOptions,
    loggerFactory.CreateLogger<ModerationBridgeService>());

var eventHandler = new StreamerBotChatEventHandler(
    mapper,
    moderationBridge,
    loggerFactory.CreateLogger<StreamerBotChatEventHandler>());

var runtimeBridge = new StreamerBotRuntimeBridge(
    eventHandler,
    loggerFactory.CreateLogger<StreamerBotRuntimeBridge>());

var adapter = new StreamerBotEventCallbackAdapter(
    runtimeBridge,
    runtimeOptions,
    loggerFactory.CreateLogger<StreamerBotEventCallbackAdapter>());

var receiver = new StreamerBotCallbackReceiver(
    adapter,
    loggerFactory.CreateLogger<StreamerBotCallbackReceiver>());
```
