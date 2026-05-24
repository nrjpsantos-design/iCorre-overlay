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
    private readonly Action<string> _log;

    private MonitorBounds? _currentBounds;
    private DateTime _lastMonitorCheckUtc = DateTime.MinValue;

    public RadarOverlay(
        RadarFrameBuffer frames,
        HostProcessDetector hostDetector,
        IRacingWindowFinder iRacingFinder,
        MonitorLocator monitorLocator,
        OverlayWindowMover windowMover,
        Action<string>? log = null)
        : base("iRadar Overlay")
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(hostDetector);
        ArgumentNullException.ThrowIfNull(iRacingFinder);
        ArgumentNullException.ThrowIfNull(monitorLocator);
        ArgumentNullException.ThrowIfNull(windowMover);

        _frames = frames;
        _hostDetector = hostDetector;
        _iRacingFinder = iRacingFinder;
        _monitorLocator = monitorLocator;
        _windowMover = windowMover;
        _log = log ?? (_ => { });

        VSync = true;
    }

    protected override Task PostInitialized()
    {
        // PostInitialized runs after the SDL window is created, so our HWND
        // exists by now. Do the first reposition immediately so iRacing's
        // monitor gets covered on first frame.
        TryRepositionToHostMonitor(force: true);
        return Task.CompletedTask;
    }

    protected override void Render()
    {
        // Re-check periodically so a user moving iRacing across monitors
        // mid-session sees the overlay follow.
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

    private void TryRepositionToHostMonitor(bool force)
    {
        var iRacingHwnd = _iRacingFinder.TryFindMainWindow();
        if (iRacingHwnd is null || iRacingHwnd == IntPtr.Zero)
        {
            return;
        }

        var bounds = _monitorLocator.GetMonitorBoundsFor(iRacingHwnd.Value);
        if (bounds is null) return;
        if (!force && bounds.Value.Equals(_currentBounds)) return;

        var ourHwnd = _windowMover.GetCurrentProcessMainWindow();
        if (ourHwnd == IntPtr.Zero) return;

        if (_windowMover.TryMoveAndResize(ourHwnd, bounds.Value))
        {
            _currentBounds = bounds;
            _log($"[overlay] following iRacing onto monitor {bounds.Value.Width}x{bounds.Value.Height} at ({bounds.Value.X}, {bounds.Value.Y})");
        }
    }
}
