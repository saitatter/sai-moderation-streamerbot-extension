# sai-moderation-streamerbot-extension

Streamer.bot extension bridge for moderation workflows.

## Purpose
- Consume chat events inside Streamer.bot runtime.
- Forward moderation requests to external backend (`sai-moderation-docker`).
- Publish verdict events to:
  - internal moderator dashboard queue
  - chat overlay feed (only allowed, optionally flagged)

## Stack
- .NET 8 class library
- xUnit tests
- Conventional Commits + semantic release metadata conventions

## Build & Test
```bash
dotnet build
dotnet test
```

## Runtime Notes

- `HttpModerationBackendClient` supports bearer token auth through `HttpModerationBackendClientOptions.ApiToken`.
- `HttpStreamerBotChannelPublisher` supports bearer token auth through `HttpChannelPublisherOptions.ApiToken`.
- For persistence across restarts, use `SqliteModerationDecisionStore` instead of `InMemoryModerationDecisionStore`.

## Integration Contract
See [docs/INTEGRATION_CONTRACT.md](docs/INTEGRATION_CONTRACT.md).
