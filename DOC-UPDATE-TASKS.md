# DOC-UPDATE-TASKS.md — one-time doc reconciliation

Small, mechanical edits to bring the repo's existing docs in line with the ship-scope pivot. Do these before any feature work. **Verify every claim against disk; do not carry forward anything from these instructions that the code contradicts.** Prior doc drift on this repo came from documenting designs as if they were builds.

## 1. Add the new files

- `SCOPE.md` at repo root. This is the contract. Reopening scope means editing it in a tracked commit.
- `SHIP-PLAN.md` at repo root.

## 2. CLAUDE.md

Do not regenerate wholesale. Patch these sections against what is actually on disk:

- **Add a pointer at the top:** ship-scope is active until August 4, 2026; `SCOPE.md` governs what may be built. Out-of-scope work must be named as scope before being helped with.
- **Current state section:** rewrite from disk. It should reflect the SignalR phases, the orchestration service, the LiveKit media provider with real token minting, and the MAUI client's existence and paused status. Verify each item exists before listing it.
- **Add a client note:** the ship target is a Razor Pages web UI with a React island, not MAUI. The MAUI project remains in the repo but is frozen for this cycle.
- **Add to conventions:** docs describe what is on disk; planned work lives in plan files only.

## 3. PHASES-MAUI.md

- Add a header line: frozen as of July 28, 2026 for the August 4 ship. Not deleted; deferred.
- No other edits. Do not check or uncheck boxes.

## 4. PHASES-STREAMING.md

- Phase 3 and Phase 4 reference the MAUI WebView client. Add a note that the ship-scope client is the Razor Pages plus React island target, and the MAUI WebView work is frozen alongside `PHASES-MAUI.md`.
- The uncommitted commit checkboxes in Phases 1, 2, and 3 reflect real uncommitted work. **Land those commits before anything else.** Three phases of work sitting uncommitted is the single most fragile thing in the repo right now.
- Keep the connection gotchas section as is. The startup-only provider selection note and the `livekit-client` UMD path note both still apply.

## 5. LIVEKIT-DOTNET-DESKTOP.md

- Add a header line: frozen until after August 4, 2026. The standing rules section currently says RoomLoom Phase 4 is the prerequisite; update that to say shipped RoomLoom is the prerequisite.

## 6. README.md

Leave it alone until Monday August 3. It gets rewritten from disk as a ship artifact, with the screen capture at the top. Editing it now guarantees editing it twice.

## Ordering

1. Land the uncommitted work from PHASES-STREAMING Phases 1 through 3.
2. Add `SCOPE.md` and `SHIP-PLAN.md`.
3. Patch `CLAUDE.md`, the two frozen phase docs, and the desktop binding doc.
4. Commit as one doc-reconciliation commit, separate from the code commits in step 1.