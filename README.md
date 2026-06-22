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
- **Infrastructure** keeps the promises. The EF Core `DbContext`, the SQL Server persistence wiring, and the provider implementations live here. Today: `InMemorySchedulingProvider` (the stub that proves the socket) and `FakeMediaProvider`. `RoomLoomDbContext` is already wired so an `EfSchedulingProvider` can drop in as the second plug without touching the core. Later: real calendar adapters and a LiveKit media provider.
- **Api** is the composition root. It reads configuration, wires implementations to contracts via dependency injection, hosts the background service, and exposes the HTTP and real-time surface.

The dependency direction is the whole architecture. Core cannot name an Infrastructure type, so it cannot couple to a provider or to EF Core. This is dependency inversion (the arrow points inward) and ports and adapters (Core defines the sockets, Infrastructure supplies the plugs).

### Persistence

Scheduled sessions will persist to SQL Server via EF Core. The mapping is configured entirely with the Fluent API inside `RoomLoomDbContext`, which keeps EF concerns out of the Core models. The context is ready; the EF-backed scheduling provider that uses it is the next plug to write. The `LiveSession` model is deliberately not persisted: it is runtime state that should not survive a restart, so it lives in memory and is owned by the real-time layer.

Each `ScheduledSession` carries a planned-status lifecycle: `Scheduled`, `Live`, `Cancelled`, `Expired`. This is distinct from a live session's runtime status. The plan's status describes the booking; the runtime status describes a meeting actually in progress. They touch only at "Live."

### Background expiry

A hosted `BackgroundService` sweeps on a timer and marks lapsed sessions (still `Scheduled`, end time past) as `Expired`. It uses the scope-factory pattern: the service is a singleton, so it creates a fresh DI scope and a fresh `DbContext` on each tick rather than capturing a scoped context for the app lifetime. The sweep is an atomic `ExecuteUpdateAsync`, which avoids loading rows just to update them and closes the race between a session going live and the sweep marking it expired.

### Real-time layer

SignalR provides the real-time surface. `SessionHub` is intentionally thin: it is created per invocation and holds no fields. The durable runtime state lives in `LiveSessionService`, a singleton owned by the Api layer, with `ConcurrentDictionary`-backed presence keyed by SignalR connection id. SignalR groups (one per session) are the routing primitive; the hub never tracks connection ids itself, and presence is auto-cleaned when a connection drops via an `OnDisconnectedAsync` override.

Lifecycle (go-live and end) is orchestrated by `SessionService` in Infrastructure, which depends only on Core ports: `ISchedulingProvider` for the plan, `IMediaProvider` for the room, `ILiveSessionService` for the runtime object, and `ISessionNotifier` for broadcasts. The Api supplies `SignalRSessionNotifier`, the only adapter that knows about `IHubContext<SessionHub>`. Orchestration stays free of SignalR; the hub stays free of scheduling and media.

#### Scaling

This is a single-instance design today. Group membership and the presence dictionaries live in process memory, so running multiple Api instances behind a load balancer would shard presence and miss broadcasts across instances. The known scale-out path is a SignalR backplane (Redis) or Azure SignalR Service, both of which are drop-in. Not built yet; documented as the path when horizontal scale is needed.

## Current state

Early build, active development. Working today:

- Three-project structure with enforced dependency direction.
- Domain models and provider interfaces in Core.
- EF Core persistence wiring for SQL Server, with Fluent API mapping in `RoomLoomDbContext`. Migrations land alongside the first EF-backed provider.
- `InMemorySchedulingProvider` satisfying `ISchedulingProvider`. An EF-backed provider is the next plug.
- Fake media provider behind `IMediaProvider`.
- Background service that expires lapsed sessions.
- SignalR hub with presence (join, leave on disconnect, who-is-here query).
- Session orchestration (`ISessionService`) for go-live and end, with broadcasts through an `ISessionNotifier` port.
- Integration tests using `WebApplicationFactory` and a real SignalR client over the in-memory test server.

## Roadmap

- `EfSchedulingProvider`: the SQL-backed implementation of `ISchedulingProvider`, registered alongside or in place of the in-memory provider. Ships with the first migration.
- Real adapters: a LiveKit media provider and at least one real calendar provider.
- Auth on the hub and HTTP endpoints.
- MAUI client that talks only to the RoomLoom API, never to providers directly. Will use `WithAutomaticReconnect()` on the SignalR connection.
- Long-term: a native .NET media layer to replace the LiveKit dependency. This is where the hard part lives (SFU, WebRTC negotiation, congestion control), so it is a deliberate long-term direction, not a near-term claim.

## Setup

Requires .NET 10. The live scheduling registration today is `InMemorySchedulingProvider`, so the app runs with no database. SQL Server is required only to exercise the EF Core persistence wiring (and will be required once `EfSchedulingProvider` lands). On Apple Silicon, SQL Server 2022 under Docker Desktop with Rosetta is the tested path.

1. Run:
   ```
   dotnet run --project RoomLoom.Api
   ```

To exercise the SQL Server persistence wiring:

1. Start SQL Server (Docker). Set a strong `SA` password.
2. Put the connection string in user-secrets on the Api project (it is not committed):
   ```
   dotnet user-secrets set "ConnectionStrings:RoomLoomDb" "Server=localhost,1433;Database=RoomLoom;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
   ```
3. Create and apply the initial migration (none are committed yet):
   ```
   dotnet ef migrations add Initial --project RoomLoom.Infrastructure --startup-project RoomLoom.Api
   dotnet ef database update --project RoomLoom.Infrastructure --startup-project RoomLoom.Api
   ```
4. Run as above.

Swapping the in-memory provider for an EF-backed one (once it exists) is a single registration line in `Program.cs`. The fake and real providers satisfy the same `ISchedulingProvider` contract, so the swap is one line. That is the architecture point, not a workaround.

## Stack

- ASP.NET Core (.NET 10)
- EF Core with the SQL Server provider
- SQL Server (2022 under Docker on Apple Silicon)
- SignalR for real-time presence and signaling (in progress)
- LiveKit for media via a pluggable provider; fake provider for local dev
- .NET MAUI for the client (planned)

## License

TBD.