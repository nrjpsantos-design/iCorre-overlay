using ClickableTransparentOverlay;
using iRadar.Core.Radar;
using iRadar.Overlay.Widgets;

namespace iRadar.Overlay.Window;

// Top-level transparent click-through window. Hosted by
// ClickableTransparentOverlay (external Win32 layered window + D3D11 +
// ImGui.NET). The overlay is always-on-top; the render loop only draws
// ImGui content when the host process (iRacing) holds the foreground —
// otherwise the window stays present but renders nothing, so the user
// doesn't see iRadar widgets bleeding onto other apps.
//
// Multi-monitor: ClickableTransparentOverlay creates the window on the
// primary monitor by default. We watch where iRacing's main window lives
// and reposition / resize our window to cover the same monitor — checked
// on PostInitialized and re-checked every few seconds during Render.
public sealed class RadarOverlay : ClickableTransparentOverlay.Overlay
{
    private static readonly TimeSpan MonitorPollInterval = TimeSpan.FromSeconds(2);

    private readonly RadarFrameBuffer _frames;
    private readonly HostProcessDetector _hostDetector;
    private readonly IRacingWindowFinder _iRacingFinder;
    private readonly MonitorLocator _monitorLocator;
    private readonly OverlayWindowMover _windowMover;
    private readonly WindowStyleManager _styleManager;
    private readonly Action<string> _log;

    private MonitorBounds? _currentBounds;
    private DateTime _lastMonitorCheckUtc = DateTime.MinValue;
    private bool _clickThroughEverApplied;
    private int _lastReportedExStyle;
    private int _clickThroughLogFrames;

    public RadarOverlay(
        RadarFrameBuffer frames,
        HostProcessDetector hostDetector,
        IRacingWindowFinder iRacingFinder,
        MonitorLocator monitorLocator,
        OverlayWindowMover windowMover,
        WindowStyleManager styleManager,
        Action<string>? log = null)
        : base("iRadar Overlay")
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(hostDetector);
        ArgumentNullException.ThrowIfNull(iRacingFinder);
        ArgumentNullException.ThrowIfNull(monitorLocator);
        ArgumentNullException.ThrowIfNull(windowMover);
        ArgumentNullException.ThrowIfNull(styleManager);

        _frames = frames;
        _hostDetector = hostDetector;
        _iRacingFinder = iRacingFinder;
        _monitorLocator = monitorLocator;
        _windowMover = windowMover;
        _styleManager = styleManager;
        _log = log ?? (_ => { });

        VSync = true;
    }

    protected override Task PostInitialized()
    {
        // PostInitialized runs after the SDL window is created, so our HWND
        // exists by now. Reposition once; click-through is asserted on every
        // Render frame (because SDL/CTO may overwrite our style bits).
        TryRepositionToHostMonitor(force: true);
        TryEnsureClickThrough();
        return Task.CompletedTask;
    }

    protected override void Render()
    {
        // SDL / CTO may re-apply window styles on its own message-loop ticks,
        // so we re-assert click-through on every frame. Logging is throttled
        // to changes only — see TryEnsureClickThrough.
        TryEnsureClickThrough();

        // Re-check monitor placement periodically so a user moving iRacing
        // across monitors mid-session sees the overlay follow.
        var now = DateTime.UtcNow;
        if (now - _lastMonitorCheckUtc >= MonitorPollInterval)
        {
            _lastMonitorCheckUtc = now;
            TryRepositionToHostMonitor(force: false);
        }

        if (!_hostDetector.IsHostInForeground())
        {
            return;
        }

        var snapshot = _frames.Snapshot;
        var frame = _frames.Frame;

        HelloWidget.Draw(snapshot, frame);
    }

    private void TryEnsureClickThrough()
    {
        try
        {
            var hwnd = _windowMover.GetCurrentProcessMainWindow();
            if (hwnd == IntPtr.Zero)
            {
                if (!_clickThroughEverApplied && _clickThroughLogFrames == 0)
                {
                    _log("[overlay] click-through: HWND not ready yet, will retry");
                    _clickThroughLogFrames = 120;  // suppress repeats for ~2s @ 60Hz
                }
                if (_clickThroughLogFrames > 0) _clickThroughLogFrames--;
                return;
            }

            var current = _styleManager.ReadExStyle(hwnd);
            if (current != _lastReportedExStyle)
            {
                _log($"[overlay] hwnd=0x{hwnd.ToInt64():X} exstyle changed: 0x{_lastReportedExStyle:X8} -> 0x{current:X8}");
                _lastReportedExStyle = current;
            }

            var result = _styleManager.MakeClickThrough(hwnd);

            if (result.Success && !_clickThroughEverApplied)
            {
                _clickThroughEverApplied = true;
                _log($"[overlay] click-through enabled on hwnd=0x{hwnd.ToInt64():X}: {result.Reason}");
                _log("[overlay] clicks now pass to iRacing — Edit Mode lands in Fase 6");
            }
            else if (!result.Success)
            {
                // Failures should be rare; throttle to once every ~2 seconds.
                if (_clickThroughLogFrames == 0)
                {
                    _log($"[overlay] click-through MakeClickThrough failed: {result.Reason}");
                    _clickThroughLogFrames = 120;
                }
                _clickThroughLogFrames--;
            }
        }
        catch (Exception ex)
        {
            _log($"[overlay] click-through exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void TryRepositionToHostMonitor(bool force)
    {
        try
        {
            var iRacingHwnd = _iRacingFinder.TryFindMainWindow();
            if (iRacingHwnd is null || iRacingHwnd == IntPtr.Zero)
            {
                if (force) _log("[overlay] iRacing window not found yet — will retry");
                return;
            }

            var bounds = _monitorLocator.GetMonitorBoundsFor(iRacingHwnd.Value);
            if (bounds is null)
            {
                if (force) _log("[overlay] could not determine iRacing's monitor — will retry");
                return;
            }
            if (!force && bounds.Value.Equals(_currentBounds)) return;

            var ourHwnd = _windowMover.GetCurrentProcessMainWindow();
            if (ourHwnd == IntPtr.Zero)
            {
                if (force) _log("[overlay] our own window HWND not yet ready — will retry");
                return;
            }

            // Only translate (X, Y) — do NOT resize. CTO sized the swap chain
            // at construction time; changing the size via SetWindowPos can
            // leave the chain in an inconsistent state and the window
            // renders blank.
            if (_windowMover.TryMove(ourHwnd, bounds.Value.X, bounds.Value.Y))
            {
                _currentBounds = bounds;
                _log($"[overlay] following iRacing onto monitor at ({bounds.Value.X}, {bounds.Value.Y})");
            }
            else
            {
                _log("[overlay] SetWindowPos failed");
            }
        }
        catch (Exception ex)
        {
            // Reposition must never crash the render loop.
            _log($"[overlay] reposition error: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
