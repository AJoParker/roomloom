# LIVEKIT-DOTNET-DESKTOP.md — Native LiveKit Client for .NET Desktop (Catalyst + Windows)

**FROZEN until after August 4, 2026.** `SCOPE.md` governs; this is explicitly named there as the likeliest way the ship date dies. Unfreezes after ship.

Plan for building a native (non-WebView) LiveKit client for .NET MAUI on desktop, by binding LiveKit's Rust FFI layer. This is a SEPARATE project from RoomLoom. RoomLoom ships with the WebView + LiveKit JS approach and stays that way; this project is the deeper specialization.

## Phase 0 outcome — OUTCOME A (2026-07-02)

The shipped macOS FFI dylib loads and executes inside a Mac Catalyst process. Verified in `~/dotnet/LiveKitMauiSpike` with `Livekit.Rtc.Dotnet` 0.1.3 (`runtimes/osx-arm64/native/liblivekit_ffi.dylib`, LC_BUILD_VERSION platform 1 / macOS, minos 11.0, sdk 15.5, not zippered). Full round trip proven: `Room.ConnectAsync` against a closed port returned `RoomException: Failed to connect: engine: signal failure: ws failure: IO error: Connection refused (os error 61)` — that error text originates in the Rust core, so managed -> FFI -> Rust engine -> syscall -> FFI callback -> managed all executed. dyld did not reject the PLATFORM_MACOS dylib in the macabi process; no macabi cross-compile needed for milestone 1.

Two pieces of packaging plumbing are required (both small, both in the spike csproj/code):
1. `maccatalyst-arm64` does not RID-fall-back to `osx-arm64`, so NuGet never selects the native asset. Fix: explicit `<Content>` copy of the dylib from the NuGet cache into the app bundle (lands in `Contents/Resources/`).
2. The binding's `DllImport("livekit_ffi")` cannot find the dylib there. Fix: `NativeLibrary.SetDllImportResolver` on the binding assembly, mapping `livekit_ffi` to the bundled path. In the real library (Phase 5) this belongs in a platform init call.

Machine note: Xcode 26.6 vs the .NET Catalyst SDK pin of 26.5 requires `<ValidateXcodeVersion>false</ValidateXcodeVersion>` until the SDK pack catches up.

Gap 1 on Mac is closed. Next: Phase 1 (connect and receive events against the real dev server).

## Context (read first)

- There is no production .NET client WebRTC/video SDK. LiveKit's native core is its Rust SDK, which exposes an official C-compatible FFI layer (`livekit-ffi`) built for other languages to bind. LiveKit's own Python/Node/Unity SDKs wrap it.
- A .NET binding already exists: `Livekit.Rtc.Dotnet` (pabloFuente/livekit-server-sdk-dotnet). It targets server-side participants and ships native binaries for Windows/Linux/macOS (x64 + ARM64). It does NOT do device capture or on-screen rendering — the caller feeds AudioSource/VideoSource with raw frames and receives decoded frames back.
- The work is therefore three gaps: (1) binary/platform fit, (2) device capture in, (3) frame rendering out. Desktop scope shrinks gap 1 dramatically.
- The transport/protocol layer (WebRTC, ICE, SRTP, congestion control, codecs) lives in the Rust core. Do NOT write any codec, transport, or WebRTC-internal code in this project. If a task appears to require it, the task is wrong.

## Scope

- **In:** MAUI on Mac Catalyst (milestone 1) and Windows (milestone 2). Camera + mic capture, publish to a LiveKit room, subscribe and render remote video, play remote audio.
- **Out:** iOS/Android (milestone 3, future), screen share, device pickers, simulcast tuning, recording, production deployment. No SIPSorcery, no FFmpeg, no libwebrtc.

## Hard constraints

- Dev machine is an M4 Mac. The Windows target CANNOT be built or tested on it (WinUI 3 requires Windows). Milestone 2 is blocked on Windows hardware/VM access — do not attempt it from macOS.
- "Mac" in MAUI = **Mac Catalyst**, built against the iOS-flavored SDK (target triple `aarch64-apple-ios-macabi`), NOT plain macOS (`aarch64-apple-darwin`). The shipped macOS binaries in Livekit.Rtc.Dotnet were built for plain .NET-on-macOS. Whether they load inside a Catalyst process is UNVERIFIED and is the single biggest early risk. Phase 0 exists to answer it before anything else is built.

## Phase 0 — The macabi spike (do this first, nothing else until answered)

Goal: answer one question — does the shipped macOS FFI dylib load and P/Invoke inside a Mac Catalyst app?

- [x] Bare MAUI app, Catalyst target only. (`~/dotnet/LiveKitMauiSpike`)
- [x] Reference `Livekit.Rtc.Dotnet`, attempt to load the native library and call one trivial FFI function (or just construct a `Room` object) inside the Catalyst app. Spike does three explicit layers: bundle presence, `NativeLibrary.TryLoad`, and a bogus `Room.ConnectAsync` forced through a `DllImportResolver`.
- [x] Outcome A — it loads and calls: gap 1 on Mac is closed; skip to Phase 1. THIS IS THE OUTCOME.
- [ ] ~~Outcome B~~ — did not occur; no macabi cross-compile needed.
- [x] Record the outcome and the exact error (if any) at the top of this file.

Done when: a Catalyst app can call into livekit-ffi, whichever binary that took.

## Phase 1 — Connect and receive data (no media yet)

Goal: a Catalyst app joins a LiveKit room as a participant via the FFI binding. Proves the binding, event loop, and token flow with zero capture/render complexity.

- [ ] LiveKit dev server in Docker (same `livekit-server --dev` setup as RoomLoom; reuse its compose file or replicate).
- [ ] Mint a token (reuse RoomLoom's LiveKitMediaProvider approach or the `lk` CLI — do not build new token infra).
- [ ] Connect: `Room.ConnectAsync(wsUrl, token)`, wire ParticipantConnected / TrackSubscribed / Disconnected events to logs.
- [ ] Verify against a browser tab joined to the same room: the Catalyst app sees the browser participant appear/leave.
- [ ] Commit: "connect to LiveKit room via FFI from Catalyst".

Done when: participant join/leave events from a real room print in the Catalyst app.

## Phase 2 — Audio out then audio in

Audio first: no rendering surface needed, smaller frames, faster feedback loop than video.

- [ ] **Playback (remote audio):** subscribe to a remote audio track, pull PCM frames from the FFI, play via the platform audio output (AVAudioEngine on Catalyst). A browser tab publishing mic audio is the test source.
- [ ] **Capture (local mic):** capture PCM from the mic (AVAudioEngine input node on Catalyst), push into an FFI `AudioSource` (mind sample rate/channel expectations, e.g. 48000/1), publish the track. Verify audible in the browser tab.
- [ ] Commit each direction separately.

Done when: two-way audio between the Catalyst app and a browser tab.

Watch out for: sample-format mismatches (float vs int16 PCM, sample rate) — the classic silent-audio or chipmunk bug. Log frame counts/formats on both sides before debugging anything else.

## Phase 3 — Video render (remote in)

Goal: remote participant video on screen in the Catalyst app. Rendering before capture, because a browser tab provides a free test source.

- [ ] Subscribe to a remote video track; receive decoded I420 frames from the FFI.
- [ ] Render path: convert I420 -> displayable surface. Options, in order of preference: (a) GPU — YUV planes to textures, shader-composite (the Between Bells OpenGL skill; on Catalyst, Metal via a MAUI handler or SkiaSharp with GPU backing), (b) SkiaSharp CPU convert + SKCanvasView (acceptable first cut, watch CPU at 30fps), (c) per-platform native view. Start with (b) to get pixels fast, then move hot path to (a) if CPU cost shows.
- [ ] Frame pump: FFI frames arrive on a background thread; marshal to the UI thread correctly, drop frames rather than queue when behind.
- [ ] Commit: "render remote video track".

Done when: a browser participant's camera renders live in the Catalyst app at watchable framerate.

## Phase 4 — Video capture (local out)

- [ ] Capture camera frames on Catalyst via AVFoundation (AVCaptureSession), convert the native pixel format (likely NV12/BGRA) to I420, push into an FFI `VideoSource`, publish.
- [ ] Verify the Catalyst camera renders in a browser tab.
- [ ] Local self-view: render the captured frames locally too (reuses Phase 3 path).
- [ ] Commit: "publish camera from Catalyst".

Done when: two-way video + audio between Catalyst app and browser. That is the milestone-1 demo.

Watch out for: pixel-format conversion (NV12 -> I420 plane shuffling) and camera permission entitlements in the Catalyst app manifest.

## Phase 5 — Shape the API (wrap the plumbing)

Goal: turn the working spike into the start of an idiomatic library. Binding is done; this is the wrapper.

- [ ] Extract a clean surface: something like `LiveKitClient` (connect/disconnect), `LocalMedia` (start/stop camera+mic), and a `VideoView` MAUI control that takes a track and renders it.
- [ ] Consumer test: a fresh sample app should do join-and-video in under ~30 lines against the wrapper.
- [ ] Split repo layout: binding glue / platform capture / rendering / public API as separate projects, same inward-dependency discipline as RoomLoom.
- [ ] Commit: "public API surface v0".

## Milestone 2 — Windows (blocked on hardware)

- [ ] Requires a Windows machine or VM. Do not start from macOS.
- [ ] The shipped win-x64 FFI binary should load in a WinUI process directly (plain Windows .NET — no macabi-style mismatch expected). Verify with a Phase-0-style spike anyway.
- [ ] Capture: Media Foundation / Windows.Media.Capture. Rendering: same I420 pipeline, Skia or D3D-backed.
- [ ] Re-run Phases 1-4 on Windows; most non-capture code should be shared.

## Milestone 3 — Mobile (future, out of scope here)

Cross-compile livekit-ffi for aarch64-apple-ios / aarch64-linux-android, mobile capture APIs, mobile GPU render. The original full-gap-1 work. Not part of this plan.

## References

- Existing .NET binding to extend: https://github.com/pabloFuente/livekit-server-sdk-dotnet (`Livekit.Rtc.Dotnet` — FFI-based, desktop binaries)
- LiveKit Rust SDKs / livekit-ffi source: https://github.com/livekit/rust-sdks
- Notion planning page: "LiveKit Client for .NET MAUI: The FFI Binding Path" (full alternatives analysis and terminology)
- RoomLoom PHASES-STREAMING.md — the WebView approach this project eventually replaces for desktop

## Standing rules

- One phase at a time; each phase green and committed before the next.
- If the shipped binary fails on Catalyst (Phase 0 outcome B), scope the macabi build fully before writing app code against it.
- Prefer contributing capture/render work upstream to Livekit.Rtc.Dotnet over forking, where the maintainer is receptive.
- No em dashes in generated prose/docs.
- This is an active portfolio project running alongside the job search. Every phase boundary is a shippable, demoable state. If the project must pause (interviews, RoomLoom work, life), stop AT a phase boundary with a green commit, never mid-phase. A finished Phase N beats a half-done Phase N+1.
- Prerequisite: shipped RoomLoom (the August 4, 2026 ship per `SCOPE.md`) before this repo opens, so the portfolio always contains at least one complete project.