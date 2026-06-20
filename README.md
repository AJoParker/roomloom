# RoomLoom

RoomLoom is a modular live-conference platform built to weave any scheduling system into one place. The core never knows which calendar or media backend it is talking to. Scheduling providers and media servers plug into contracts the core owns, so adding support for a new system means writing one adapter, not editing the core.

Built with ASP.NET Core. SignalR handles real-time presence and signaling; media is delegated to a pluggable provider (a fake one for local dev, with a LiveKit adapter as the real path). A .NET MAUI client is planned.

## Why this exists

Most conference integrations are written the wrong way around. The core app grows a direct dependency on Google Calendar, then Outlook, then the next thing, and every new integration touches the center. RoomLoom inverts that. The core defines what it needs in its own vocabulary, and each external system gets an adapter that translates into that vocabulary. The center stays closed; integration happens at the edges.

That design is the point of the project. "Works with any scheduling system" holds even for systems that do not exist yet, because the contract was defined before any adapter.

## Architecture

Three projects, dependencies pointing inward:

```
RoomLoom.Core            // domain models + interfaces. References nothing.
RoomLoom.Infrastructure  // provider implementations + persistence. References Core.
RoomLoom.Api             // controllers, hosted services, composition root. References both.
```

- **Core** owns the vocabulary. Models (`ScheduledSession`, `LiveSession`, `Participant`) and contracts (`ISchedulingProvider`, `IMediaProvider`). It depends on nothing, by design, and the project references enforce that at compile time. It is also persistence-ignorant: no EF Core reference, no data annotations, nothing about how it gets stored.
- **Infrastructure** keeps the promises. The EF Core `DbContext`, the SQL Server persistence, and the provider implementations live here. Today: `EfSchedulingProvider` (real, SQL-backed), `InMemorySchedulingProvider` (kept for tests and fast local runs), and `FakeMediaProvider`. Later: real calendar adapters and a LiveKit media provider.
- **Api** is the composition root. It reads configuration, wires implementations to contracts via dependency injection, hosts the background service, and exposes the HTTP and real-time surface.

The dependency direction is the whole architecture. Core cannot name an Infrastructure type, so it cannot couple to a provider or to EF Core. This is dependency inversion (the arrow points inward) and ports and adapters (Core defines the sockets, Infrastructure supplies the plugs).

### Persistence

Scheduled sessions are persisted to SQL Server via EF Core. The mapping is configured entirely with the Fluent API inside the `DbContext`, which keeps EF concerns out of the Core models. The `LiveSession` model is deliberately not persisted: it is runtime state that should not survive a restart, so it lives in memory and is owned by the real-time layer.

Each `ScheduledSession` carries a planned-status lifecycle: `Scheduled`, `Live`, `Cancelled`, `Expired`. This is distinct from a live session's runtime status. The plan's status describes the booking; the runtime status describes a meeting actually in progress. They touch only at "Live."

### Background expiry

A hosted `BackgroundService` sweeps on a timer and marks lapsed sessions (still `Scheduled`, end time past) as `Expired`. It uses the scope-factory pattern: the service is a singleton, so it creates a fresh DI scope and a fresh `DbContext` on each tick rather than capturing a scoped context for the app lifetime. The sweep is an atomic `ExecuteUpdateAsync`, which avoids loading rows just to update them and closes the race between a session going live and the sweep marking it expired.

## Current state

Early build, active development. Working today:

- Three-project structure with enforced dependency direction.
- Domain models and provider interfaces in Core.
- EF Core persistence on SQL Server, with Fluent API mapping and migrations.
- In-memory and SQL-backed scheduling providers, both satisfying the same contract.
- Fake media provider behind `IMediaProvider`.
- Background service that expires lapsed sessions.

## Roadmap

- SignalR hub for presence, join/leave, and session lifecycle events.
- Session orchestration service: take a `ScheduledSession` live, create the `LiveSession`, request a media room, track runtime state.
- Real adapters: a LiveKit media provider and at least one real calendar provider.
- MAUI client that talks only to the RoomLoom API, never to providers directly.
- Long-term: a native .NET media layer to replace the LiveKit dependency. This is where the hard part lives (SFU, WebRTC negotiation, congestion control), so it is a deliberate long-term direction, not a near-term claim.

## Setup

Requires .NET 10 and a running SQL Server instance. On Apple Silicon, SQL Server 2022 under Docker Desktop with Rosetta is the tested path.

1. Start SQL Server (Docker). Set a strong `SA` password.
2. Put the connection string in user-secrets on the Api project (it is not committed):
   ```
   dotnet user-secrets set "ConnectionStrings:RoomLoomDb" "Server=localhost,1433;Database=RoomLoom;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
   ```
3. Apply migrations:
   ```
   dotnet ef database update --project RoomLoom.Infrastructure --startup-project RoomLoom.Api
   ```
4. Run:
   ```
   dotnet run --project RoomLoom.Api
   ```

For a database-free run (no SQL Server, no Docker), the in-memory scheduling provider can be registered instead of the EF one. The fake and real providers satisfy the same `ISchedulingProvider` contract, so the swap is a single registration line. That is the architecture point, not a workaround.

## Stack

- ASP.NET Core (.NET 10)
- EF Core with the SQL Server provider
- SQL Server (2022 under Docker on Apple Silicon)
- SignalR for real-time presence and signaling (in progress)
- LiveKit for media via a pluggable provider; fake provider for local dev
- .NET MAUI for the client (planned)

## License

TBD.