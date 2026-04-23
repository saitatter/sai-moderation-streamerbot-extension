# Action Plan

## Phase 1: Extension Runtime Hook
- Define event hooks for Streamer.bot chat events.
- Normalize incoming platform payloads into `ChatEvent`.
- Add resilient message ID generation strategy.

## Phase 2: Backend Moderation Client
- Implement HTTP client to `sai-moderation-docker`.
- Add timeout, retry, and fallback handling.
- Add structured logging with message correlation IDs.

## Phase 3: Decision Forwarding
- Publish all moderation results to dashboard channel.
- Publish only allowed (and optionally flagged) messages to overlay channel.
- Add option flags for runtime behavior.

## Phase 4: Manual Override Flow
- Add callback route/events for moderator manual actions.
- Record override reason and operator identity.
- Re-emit corrected decision downstream.

## Phase 5: Hardening
- Add integration tests with mocked backend responses.
- Add serialization compatibility tests for contract DTOs.
- Add performance checks for high-throughput bursts.

