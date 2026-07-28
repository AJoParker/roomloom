# SCOPE.md

Ship date: **Tuesday, August 4, 2026.** The date does not move. Scope moves.

Reopening scope requires visibly editing this file in a tracked commit.

## The acceptance test

> RoomLoom lets a signed-in user create a session, share a link, and have a second
> person join that session live, with the roster updating in real time on both screens.

Anything that does not serve that sentence is out.

## In

1. `ISchedulingProvider` port with at least two adapters. `InMemorySchedulingProvider` (hardcoded stub data) exists on disk; the EF-backed provider does NOT yet exist and is Wednesday build work. It is load-bearing: the stub does not persist creates, so the acceptance test cannot run on it. Together they satisfy the "port is not decorative" requirement. `GoogleCalendarAdapter` (read-only) is a Sunday stretch, not a load-bearing requirement.
2. `IMediaProvider` port with `LiveKitMediaProvider` and `FakeMediaProvider`.
3. Session lifecycle: create, list, join, end. No edit, no delete, no recurrence.
4. SignalR hub doing one thing: participant joined, participant left, roster broadcast.
5. EF Core against Azure SQL. Sessions and Participants. Migrations committed.
6. Auth: Google sign-in only. No roles, no profile page.
7. Deployed to Azure App Service from GitHub Actions.
8. README with a 30-second screen capture at the top.

## Out

Do not propose these. Flag it if I start drifting toward them.

- **The MAUI client.** All of it. Paused, not deleted. `PHASES-MAUI.md` is frozen.
- **The native LiveKit desktop FFI binding project.** `LIVEKIT-DOTNET-DESKTOP.md` is frozen until after August 4. This is the most seductive item on the list and the likeliest way the ship date dies.
- Any scheduling adapter beyond InMemory, EF, and (optionally) Google Calendar. No Outlook, Zoom, Calendly, Teams.
- Recording, breakout rooms, waiting room, host controls, mute-all. Whatever LiveKit's prebuilt component includes is free and stays; nothing beyond it.
- **Captions.** Top post-ship feature and the intended differentiator. Ships as a README roadmap entry with two sentences on the approach, not as code.
- Test breadth. Domain-layer tests against the fakes only. Zero new integration tests, zero UI tests. Existing tests stay.
- Timezone handling beyond UTC storage and browser-local rendering.
- Notifications, email, invites, calendar write-back, ICS export.
- Observability. App Service default logging only. No App Insights, no OpenTelemetry.
- Polish outside the join flow. No dark mode, no responsive tuning past "does not break on a laptop," no skeletons, no empty-state illustrations.
- Infrastructure as code. Click it out in the portal.

## Tripwires

Mechanical, not judgment calls.

- **Media, end of Saturday 8/1.** If video is not working, clear `LiveKit:Url` in App Service settings (an empty value falls back to `FakeMediaProvider`; that is the actual switch on disk, there is no `Media:Provider` key), add one README line explaining the port and why the adapter is stubbed, and move on. The roster syncing already proves the real-time story. Do not spend Sunday on WebRTC.
- **Google auth and calendar, mid-afternoon Sunday 8/2.** If sign-in is not working, cut `GoogleCalendarAdapter` entirely. The InMemory and EF adapters already prove the port. Do not let the last build day become an OAuth debugging session.
- **Any day, any task.** If a task is not done by the end of its assigned day, it moves to Out. It does not eat the next day.

## Closed decisions

Do not reopen without a specific new fact. If one is challenged, say so directly rather than quietly redesigning around it.

| Decision | Rationale |
|---|---|
| Razor Pages + a React island, not Blazor | LiveKit's prebuilt UI is React-only. Blazor Server would mean JS interop on top of a framework already using SignalR. Also differentiates RoomLoom from Fulcrum in the portfolio. |
| React scoped to a single `#livekit-root` div | Rest of the page stays server-rendered. SignalR roster is plain JS outside the island. No shared state across the boundary. |
| `<VideoConference />` used as shipped | Replacing it with individual components to customize layout is where the schedule dies. |
| **Token minting keeps `Livekit.Server.Sdk.Dotnet` 1.2.2** | Reversal of the earlier "hand-roll the JWT" decision. The SDK path is already implemented, already covered by a unit test that decodes the JWT and asserts identity and grants, plus two endpoint tests. Rewriting working tested code to drop a dependency is scope addition. Hand-roll only if the SDK actually fails against LiveKit Cloud. |
| Token minting lives behind `IMediaProvider` in Infrastructure | Core knows nothing about JWTs or WebRTC. This is the architectural seam the project demonstrates. |
| Room name derived as `session-{sessionId}` | No rooms table. Two people on the same session id land in the same room. |
| App Service **B1 Linux**, not F1 | Free tier does not support WebSockets, which SignalR needs. Web sockets must also be explicitly set to On in General settings. |
| Azure SQL serverless, auto-pause disabled during the demo window | A cold start makes the live link look broken to a hiring manager. Re-enable after the demo window; set a reminder. |
| Google OAuth consent screen stays in Testing mode | Publishing triggers a verification review that takes weeks. |
| Vite build wiring done up front, not mid-week | Bolting a build pipeline onto a working app later is the classic schedule killer. |

## Open decision, must close Wednesday

**Do sessions have an owner?** If Google sign-in means sessions become user-scoped, that is an `OwnerId` column on Sessions plus query changes. Decide Wednesday and put the column in the migration being applied that day. Discovering it Sunday means a migration on the last build day.

## Working rules

- Hold the scope. If I propose something on the Out list, say so plainly before helping. Name it as scope, then let me decide.
- Prefer the smaller version. When there is a correct way and a fast way and both ship, recommend the fast one and note what it costs.
- No polish I did not ask for. No extra config, no defensive abstractions, no "while we're here."
- Flag schedule slips at the start of a day, not the end.
- Ask before generating large scaffolds. Small, reviewable pieces.
- Docs describe what is on disk. Planned work lives in plan files, never in CLAUDE.md or README.
- No em dashes in generated prose or docs.