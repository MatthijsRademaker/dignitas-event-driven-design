# AGENTS.md

## Project intent
- Call center employee use case: calls, transcripts, and call-related events.
- Content sequence: 1 presentation, then 2 exercises in order: CQRS inconsistency -> outbox pattern -> saga state machine.
- Naming convention: avoid `*Service`; prefer `*er` (e.g., `OrderProcessor`, `InvoiceCreator`).

## Exercises (call center)
- CQRS inconsistency demo: persist a `TranscriptSegment` but fail to publish `TranscriptReceived`, leaving read models (agent dashboard/suggestions) stale.
- Exercise 1 (Outbox): add outbox storage + `OutboxDispatcher` so `TranscriptReceived` publishes reliably and dashboards update after dispatch.
- Exercise 2 (Saga): `CallResolutionSaga` coordinates `CallStarted` -> `TranscriptStreaming` -> `ResolutionDrafted` -> `CustomerConfirmationRequested` -> `CallResolved`, with compensations like `FollowUpScheduled` on `CallerDisconnected` or timeouts.

## Branches
- `main`: full solution (latest). Use for combined presentation + exercises.
- `demo-cqrs`: CQRS-only demo where publish failures leave projections stale.
- `exercise-1-outbox`: outbox pattern implementation for exercise 1.
- `exercise-2-saga`: reserved for saga state machine (exercise 2).

## Solution layout
- Aspire solution root: `src/src/EDA`.
- AppHost: `src/src/EDA/EDA.AppHost/AppHost.cs` (wires Postgres + RabbitMQ, server, Vite frontend, publishes `wwwroot`).
- Server: `src/src/EDA/EDA.Server/`.
- Frontend (Vite/React): `src/src/EDA/frontend/`.

## Running the app
- Use `aspire start` from `src/src/EDA` (apphost path is `src/src/EDA/aspire.config.json`).
- Frontend API proxy reads `SERVER_HTTPS`/`SERVER_HTTP` (set by Aspire); avoid hardcoding API endpoints in `src/src/EDA/frontend/vite.config.ts`.

## Tooling constraints
- .NET targets `net10.0` in `EDA.AppHost` and `EDA.Server`.
- Frontend Node engine: `^20.19.0 || >=22.12.0` (see `src/src/EDA/frontend/package.json`).

## Presentation
- Slide deck lives in `presentation/` (Slidev). Use `pnpm install` + `pnpm dev`.
