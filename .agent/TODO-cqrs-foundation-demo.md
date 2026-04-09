# CQRS Foundation Demo Plan

## Goal
Lay the foundation for the CQRS inconsistency demo using Aspire-managed RabbitMQ, Postgres, and MassTransit in the existing scaffold.

## Key decision
- No first-party Aspire MassTransit integration was found.
- Use Aspire hosting integrations for `Postgres` and `RabbitMQ`.
- Use Aspire Postgres app integration for EF Core.
- Configure MassTransit directly against the RabbitMQ connection info injected by Aspire.

## Implementation steps

1. AppHost foundation
- Update `src/src/EDA/EDA.AppHost/EDA.AppHost.csproj`
- Add `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.RabbitMQ`
- Update `src/src/EDA/EDA.AppHost/AppHost.cs`
- Add:
  - `builder.AddPostgres("postgres")`
  - `.AddDatabase("callcenter")`
  - `builder.AddRabbitMQ("messaging")`
- Wire `EDA.Server` with references to both resources and wait for them
- Keep the Vite frontend and `PublishWithContainerFiles`

2. Server dependencies and bootstrapping
- Update `src/src/EDA/EDA.Server/EDA.Server.csproj`
- Add:
  - `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`
  - `MassTransit`
  - `MassTransit.RabbitMQ`
- Remove Redis output-cache scaffolding from `Program.cs`
- Keep Aspire defaults, health checks, OpenAPI, and telemetry

3. Persistence model
- Use one Postgres database with clear write/read separation
- Write-side entities:
  - `CallSession`
  - `TranscriptSegment`
- Read-side entities:
  - `AgentDashboardProjection`
  - `SuggestionProjection`
- Use one EF Core context for speed, with separate tables per concern
- Use `EnsureCreated` plus startup seed data

4. Seeded demo scenario
- Add a startup initializer like `DemoSeeder`
- Seed:
  - one active call
  - one agent
  - one caller
  - initial dashboard state
- Keep the seeded call id stable for the frontend demo

5. CQRS inconsistency workflow
- Add a command endpoint that records transcript text
- Flow:
  1. Save `TranscriptSegment` to Postgres
  2. If `simulatePublishFailure` is true, stop before publish and return a demo-specific failure result
  3. Otherwise publish `TranscriptReceived` through MassTransit
- This intentionally creates inconsistency:
  - write model is correct
  - read model stays stale when the event is not published

6. Event contracts and consumers
- Contracts:
  - `TranscriptReceived`
- Consumers/projectors:
  - `DashboardProjector`
  - `SuggestionProjector`
- Consumers update read-side tables asynchronously from RabbitMQ
- Keep suggestion generation deterministic and simple for now

7. API surface
- Replace weather endpoints with:
  - `GET /api/demo/state`
  - `POST /api/demo/transcripts`
  - `POST /api/demo/reset`
- `GET /api/demo/state` should return:
  - current call
  - saved transcript segments
  - dashboard projection
  - suggestions

8. Frontend demo UI
- Replace the current starter UI in `src/src/EDA/frontend/src/App.tsx`
- Build one screen with:
  - transcript entry form
  - `Save and publish` action
  - `Save but fail before publish` action
  - write-side transcript log
  - read-side dashboard panel
  - suggestions panel
- Refetch after writes so the eventual consistency gap is visible

9. Naming convention
- Avoid `*Service`
- Prefer names like:
  - `TranscriptRecorder`
  - `DashboardProjector`
  - `SuggestionProjector`
  - `DemoSeeder`

10. Verification
- `dotnet build` from `src/src/EDA`
- `npm run build` from `src/src/EDA/frontend`
- Run with:
  - `"/Users/matthijsrademaker/.aspire/bin/aspire" start`
- Manual checks:
  - happy path updates projections
  - failure path leaves write model ahead of read model
  - reset restores the demo state

## Notes
- This keeps the first exercise intentionally incomplete so the later outbox exercise has a real failure to fix.
