# iRadar Overlay

External radar/spotter overlay for iRacing. Reads telemetry via the official
public IRSDK shared-memory interface and renders a transparent always-on-top
window that shows nearby cars and collision threats.

**Status: Fase 1 (live telemetry connection).** The `iRadar.Replay` tool can
connect to a running iRacing instance via the public IRSDK shared memory and
print live telemetry every 100ms. No overlay window yet — that's Fase 4.

## Anti-cheat posture

The non-negotiable design constraint is that iRadar **never touches** the
`iRacingSim64DX11.exe` (or DX12) process:

- Reads only the publicly documented `Local\IRSDKMemMapFileName` shared memory
- Runs in its own process — no DLL injection, no DirectX hook, no kernel driver
- Never calls `ReadProcessMemory`, `WriteProcessMemory`, `CreateRemoteThread`,
  `SetWindowsHookEx`, or any related Win32 API
- A CI gate (`tools/verify-no-forbidden-apis.sh`) fails the build if any of
  those names appear in the source — enforced on every push and PR

This is the same approach used by widely accepted overlays (Garage61,
iOverlay/RaceLabApps, SimHub, CrewChief, iRon, Kapps, VRS Coach, JRT) — all
tolerated by iRacing for years.

## Project structure

```
src/
├── iRadar.Core/              # pure radar engine — math, threat detection, gaps
│                             # no Win32, no rendering, cross-platform, fully tested
├── iRadar.Overlay/           # Win32 layered window + Vortice D3D11 + ImGui.NET
├── iRadar.Infrastructure/    # IRSDK adapter, config persistence, logging
└── iRadar.App/               # composition root (WinExe entry point)

tests/
├── iRadar.Core.Tests/        # xUnit — runs against recorded .ibt fixtures
└── iRadar.Replay/            # console app that feeds an .ibt back through Core

tools/
└── verify-no-forbidden-apis.sh   # CI anti-cheat gate
```

## Requirements

- **Build / dev**: .NET 8 SDK on Windows (the `Overlay`/`Infrastructure`/`App`
  projects target `net8.0-windows`). The `Core` and `Core.Tests` projects target
  plain `net8.0` and build/run anywhere.
- **Runtime**: Windows 10/11 with iRacing installed
- **iRacing display mode**: Windowed (Borderless) — fullscreen exclusive will
  cover the overlay. The app detects and warns.

## Building (when .NET 8 SDK is available)

```bash
dotnet restore iRadar.sln
dotnet build iRadar.sln -c Release
dotnet test tests/iRadar.Core.Tests
```

## Running the Fase 1 telemetry dumper (zero-risk)

This validates that the IRSDK connection works end-to-end without any chance
of affecting your iRacing account. **Full step-by-step walkthrough** —
including prerequisites, troubleshooting, and a validation checklist — is in
[docs/TESTING-FASE1.md](docs/TESTING-FASE1.md).

Short version:
1. Open **iRacing** on your Windows machine.
2. Go to the **Replays** tab and load any saved session (yours or a friend's).
   Hit play — iRacing is now broadcasting telemetry from a recording, not
   from a live session.
3. From a PowerShell on the same machine:
   ```powershell
   dotnet run --project tests/iRadar.Replay -c Release
   ```
4. You should see lines like:
   ```
   [state] Connecting
   [state] InCar
   tick=  123456 playerIdx=  5 speed= 247.3km/h lap=  3 lapPct=0.481 cars=27 proximity=Clear        track="Spa-Francorchamps" [on-track]
   ```
5. `Ctrl+C` to stop.

Because iRacing is just playing back a recording, nothing you do here can
affect your iRating, safety rating, or stewards' review.

## Zero-risk testing strategy

The radar engine is validated against recorded telemetry — no live iRacing
session needed:

1. iRacing automatically records every session to `Documents\iRacing\telemetry\*.ibt`
2. `tests/iRadar.Replay` reads those files and feeds them through `iRadar.Core`
   exactly as a live session would
3. All unit tests (Fases 1–7) run against these fixtures

First live use only after the maturity gate in the project plan, and even then
only in **replays** and **offline AI sessions** before any online play.

## Roadmap

See the project plan for the full 10-phase roadmap. Current phase: **0
(repository scaffolding)**.
