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

## Integration Contract
See [docs/INTEGRATION_CONTRACT.md](docs/INTEGRATION_CONTRACT.md).

