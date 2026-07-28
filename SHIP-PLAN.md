# SHIP-PLAN.md — RoomLoom ship week

Ship date: **Tuesday, August 4, 2026.** Read `SCOPE.md` first; it is the contract.

## Standing rule for every day

90 minutes of job applications, five minimum, **before the editor opens.** Not after. A week of heads-down building feels like progress the entire time, which is exactly what makes it a good place to hide.

## What already exists (do not rebuild)

Verify against disk before starting any day's work. As of July 28 the repo already has:

- Three-project structure with enforced inward dependencies.
- Core domain models and both ports.
- `InMemorySchedulingProvider` (stub only), `FakeMediaProvider`, `LiveKitMediaProvider` (real token minting via `Livekit.Server.Sdk.Dotnet` 1.2.2, config-selected on `LiveKit:Url`).
- `RoomLoomDbContext` with Fluent mapping. **No migrations exist yet** and **no EF-backed scheduling provider exists yet**; both are Wednesday build work, verified against disk July 28.
- SignalR hub, `LiveSessionService` singleton, session orchestration, `SignalRSessionNotifier`.
- Background expiry service.
- Domain tests against the fakes (21 passing).
- Token endpoint returning `{ url, token }` with tests.

The genuinely new surface this week is: `EfSchedulingProvider` + initial migration (Wednesday), Razor Pages UI, the React island, a `POST /sessions` create endpoint, Azure infra, CI/CD, and Google auth.

Known disk mismatches to fix on their day: `GoLiveAsync` currently passes the session Title as the room name; the closed decision requires `session-{sessionId}` (one line, Saturday). The port has `CreateSessionAsync` but no endpoint maps it (Friday, with the create form).

## Day plan

### Tue 7/28 (tonight) — Infra and pipeline only. Zero features.

Trimmed from the original. Google Cloud and LiveKit Cloud moved out of tonight.

- Azure resource group.
- B1 Linux App Service. **Web sockets set to On** in General settings (F1 does not support them).
- Azure SQL serverless. Auto-pause disabled. **Add a firewall rule for your current IP** in the networking blade, or Wednesday's migration will fail in a way that looks like a connection string bug.
- GitHub Actions deploy workflow.
- Vite + React build wiring, with the Node steps sequenced before `dotnet publish`.

**Done when:** the untouched scaffold deploys on push and `/js/room/room.js` is served from the live URL.

Moved off tonight: Google Cloud project, Calendar API, consent screen (not needed until Sunday). LiveKit Cloud project (not needed until Saturday; the local dev server covers Thursday and Friday).

### Wed 7/29 — Build the EF provider, cut the first migration, point it at Azure SQL.

This is a real build day, not a config day. The EF provider and migrations were previously listed as existing; they do not (verified against disk July 28).

- **Decide first: do sessions have an `OwnerId`?** The answer goes into the initial migration cut today. Deciding this Sunday means a schema change on the last build day.
- Write `EfSchedulingProvider : ISchedulingProvider` over the existing `RoomLoomDbContext` (CRUD adapter; the DbContext and Fluent mapping already exist).
- Create the initial migration (OwnerId included if decided yes). Read the generated migration before applying.
- Connection string to Azure SQL in App Service configuration; apply the migration to Azure SQL.
- Register `EfSchedulingProvider` in `Program.cs` (conditional on connection string, same pattern as the media provider swap).
- Confirm existing tests still green in CI.
- Reconcile the drop-in slice (section 6 of the pivot doc) against the real codebase: it assumes `Core/Ports`, `MediaJoinTicket`, and `/api/sessions/{id}/token`, while the repo has `Core/Interfaces`, `Task<string>`, and `/live-sessions/{id}/token`. **Pick one shape now.** Recommended: keep the repo's shape, take only the React island from the slice.

**Done when:** `EfSchedulingProvider` registered and reading/writing Azure SQL through the applied migration, tests green in CI, ownership decision recorded in `SCOPE.md`.

**If time is left, bank it. Do not fill it.** It goes to Saturday.

### Thu 7/30 — Roster loop, locally.

- Sessions list page and room page (Razor Pages).
- SignalR JS client on the room page, outside the React island.
- Hub wired to join, disconnect, roster broadcast.

**Done when:** two localhost browsers show each other; closing one removes it from the other.

### Fri 7/31 — Same loop in the cloud. Session create form.

- Deploy and verify the roster loop on the public URL.
- `POST /sessions` endpoint mapping the port's existing `CreateSessionAsync` (no endpoint maps it today).
- Create form: title, start time. Nothing else.

**Done when:** roster syncs across two real devices on the public URL.

### Sat 8/1 — LiveKit. Highest risk day.

- LiveKit Cloud project (deferred from Tuesday).
- One-line fix in `SessionService.GoLiveAsync`: pass `session-{scheduledSessionId}` as the room name instead of the session Title (closed decision; current code uses the Title, which collides across same-titled sessions).
- Point `LiveKitMediaProvider` config at Cloud instead of the local dev server.
- React island connected to the existing token endpoint.
- Known trap: the `video` claim must decode as a nested JSON object, not a quoted string. **Verify the token at jwt.io before opening a browser.** A string claim fails with an error that points nowhere. (The existing `LiveKitMediaProviderTests` unit test already asserts the nested shape; jwt.io is the belt-and-suspenders check against the Cloud config.)

**Done when:** two browsers see each other's video on the live URL.

**Tripwire fires end of day.** If video is not working: clear `LiveKit:Url` in App Service settings (empty value falls back to `FakeMediaProvider`), add one README line about the port and the stubbed adapter, move on. Do not spend Sunday on WebRTC.

### Sun 8/2 — Identity. Last build day.

- Google Cloud project, Calendar API enabled, consent screen in Testing mode with self as test user (deferred from Tuesday).
- Google sign-in.
- `GoogleCalendarAdapter`, read-only, provider selectable by config.

**Done when:** signing in shows real calendar events, and flipping one config value swaps the source.

**Tripwire fires mid-afternoon.** If sign-in is not working, cut `GoogleCalendarAdapter` entirely. InMemory and EF already prove the port is not decorative. Do not let the last build day become an OAuth debugging session.

### Mon 8/3 — Artifacts. No new features.

- 30-second screen capture: two windows side by side, create then join.
- README with the capture at the top, the architecture paragraph, and the captions roadmap entry (two sentences, no code).
- Repo public, description, topics.
- ParkerPortfolio card with the live link.
- Re-verify the README describes what is on disk. No aspirational claims.

**Done when:** a fresh incognito window loads the app and the README renders with the capture.

### Tue 8/4 — No code.

- Cold test from phone and from an incognito window.
- LinkedIn post.
- Resume bullet.
- Send the link to Ed and to Ron Willoughby.

**Done when:** sent.

## After the ship date

- Re-enable Azure SQL auto-pause once the demo window closes, or budget for it staying on.
- `PHASES-MAUI.md` and `LIVEKIT-DOTNET-DESKTOP.md` unfreeze.
- Captions move from README roadmap to a real plan.