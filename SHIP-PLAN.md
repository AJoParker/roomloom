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

The genuinely new surface this week is: `EfSchedulingProvider` + initial migration (Wednesday), Razor Pages UI, the React island, a `POST /sessions` create endpoint, CI (build + tests), and Google auth. Azure infra was cut July 29; hosting is local.

Known disk mismatches to fix on their day: `GoLiveAsync` currently passes the session Title as the room name; the closed decision requires `session-{sessionId}` (one line, Saturday). The port has `CreateSessionAsync` but no endpoint maps it (Friday, with the create form).

## Day plan

### Tue 7/28 — Pipeline only. Zero features. (Done; Azure half superseded.)

**Superseded July 29: no Azure.** The portal tasks (resource group, App Service, Azure SQL) are void. What landed and stands (commit 6c46910): Razor Pages wired into `RoomLoom.Api`, Vite + React island building to `/js/room/room.js`, GitHub Actions workflow with Node steps before publish. Remaining from this day, carried to Wednesday: push the repo to GitHub and convert the workflow from deploy to CI-only (build + tests).

Moved off this day originally and still deferred: Google Cloud project, Calendar API, consent screen (not needed until Sunday).

### Wed 7/29 — Build the EF provider, cut the first migration, run it against local SQL Server.

This is a real build day, not a config day. The EF provider and migrations were previously listed as existing; they do not (verified against disk July 28).

- **OwnerId decision: CLOSED July 29 — yes, nullable `OwnerId` column.** Goes into today's initial migration; Google sign-in populates it Sunday.
- Add a `sqlserver` service (SQL Server 2022) to the existing `docker-compose.yml` next to `livekit`; connection string in user-secrets (`ConnectionStrings:RoomLoomDb`), never committed.
- Write `EfSchedulingProvider : ISchedulingProvider` over the existing `RoomLoomDbContext` (CRUD adapter; the DbContext and Fluent mapping already exist). Mark-transition methods MUST be optimistic conditional updates returning whether the row transitioned, per the `ISchedulingProvider` doc comments.
- Add `OwnerId` to the Core model and Fluent mapping; create the initial migration. Read the generated migration before applying.
- Apply the migration to the local Docker SQL Server.
- Register `EfSchedulingProvider` in `Program.cs` conditional on connection string presence (same pattern as the media provider swap); `InMemorySchedulingProvider` stays the no-DB fallback so existing tests are untouched.
- Push the repo to GitHub; convert the Actions workflow to CI-only (build + tests, no deploy step); confirm tests green in CI.
- Reconcile the drop-in slice (section 6 of the pivot doc) against the real codebase: it assumes `Core/Ports`, `MediaJoinTicket`, and `/api/sessions/{id}/token`, while the repo has `Core/Interfaces`, `Task<string>`, and `/live-sessions/{id}/token`. **Pick one shape now.** Recommended: keep the repo's shape, take only the React island from the slice.

**Done when:** `EfSchedulingProvider` registered and reading/writing local SQL Server through the applied migration, tests green in CI, ownership decision recorded in `SCOPE.md`.

**If time is left, bank it. Do not fill it.** It goes to Saturday.

### Thu 7/30 — Roster loop, locally.

- Sessions list page and room page (Razor Pages).
- SignalR JS client on the room page, outside the React island.
- Hub wired to join, disconnect, roster broadcast.

**Done when:** two localhost browsers show each other; closing one removes it from the other.

### Fri 7/31 — Same loop across two devices. Session create form.

- Bind Kestrel to the LAN (`--urls http://0.0.0.0:5150` or launchSettings) and verify the roster loop from a second device on the same network (phone or laptop hitting the machine's LAN IP). Mind the macOS firewall prompt.
- `POST /sessions` endpoint mapping the port's existing `CreateSessionAsync` (no endpoint maps it today).
- Create form: title, start time. Nothing else.

**Done when:** roster syncs across two real devices on the LAN.

### Sat 8/1 — LiveKit. Risk shrank with the Azure cut.

The local dev server in docker-compose is already proven end to end (the frozen MAUI WebView client streamed against it). No LiveKit Cloud project, no new infrastructure. The genuinely new piece is only the React island consuming the token endpoint.

- One-line fix in `SessionService.GoLiveAsync`: pass `session-{scheduledSessionId}` as the room name instead of the session Title (closed decision; current code uses the Title, which collides across same-titled sessions).
- React island (`<VideoConference />` as shipped) connected to the existing token endpoint against the local dev server.
- Known trap: the `video` claim must decode as a nested JSON object, not a quoted string. **Verify the token at jwt.io before opening a browser.** A string claim fails with an error that points nowhere. (The existing `LiveKitMediaProviderTests` unit test already asserts the nested shape.)

**Done when:** two browsers see each other's video locally.

**Tripwire fires end of day.** If video is not working: clear `LiveKit:Url` from user-secrets (empty value falls back to `FakeMediaProvider`), add one README line about the port and the stubbed adapter, move on. Do not spend Sunday on WebRTC.

### Sun 8/2 — Identity. Last build day.

- Google Cloud project, Calendar API enabled, consent screen in Testing mode with self as test user (deferred from Tuesday).
- Google sign-in.
- `GoogleCalendarAdapter`, read-only, provider selectable by config.

**Done when:** signing in shows real calendar events, and flipping one config value swaps the source.

**Tripwire fires mid-afternoon.** If sign-in is not working, cut `GoogleCalendarAdapter` entirely. InMemory and EF already prove the port is not decorative. Do not let the last build day become an OAuth debugging session.

### Mon 8/3 — Artifacts. No new features.

- 30-second screen capture: two windows side by side, create then join. This is THE portfolio artifact now that there is no live URL; budget retakes.
- README with the capture at the top, the architecture paragraph, a "run it locally" section (docker compose up, user-secrets, dotnet run), and the captions roadmap entry (two sentences, no code).
- Repo public, description, topics.
- ParkerPortfolio card with the repo link and the capture.
- Re-verify the README describes what is on disk. No aspirational claims.

**Done when:** a fresh clone following the README's run-it-locally steps reaches the working app, and the README renders with the capture.

### Tue 8/4 — No code.

- Cold test: fresh clone on a clean checkout, follow the README start to finish.
- LinkedIn post.
- Resume bullet.
- Send the repo link to Ed and to Ron Willoughby.

**Done when:** sent.

## After the ship date

- `PHASES-MAUI.md` and `LIVEKIT-DOTNET-DESKTOP.md` unfreeze.
- Captions move from README roadmap to a real plan.
- Cloud deployment, if ever, returns as its own scoped decision; nothing this week assumes it.