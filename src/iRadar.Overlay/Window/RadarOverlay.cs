using ClickableTransparentOverlay;
using iRadar.Core.Radar;
using iRadar.Overlay.Widgets;

namespace iRadar.Overlay.Window;

// Top-level transparent click-through window. Hosted by
// ClickableTransparentOverlay (external Win32 layered window + D3D11 +
// ImGui.NET). The overlay is sized to the virtual desktop bounding box at
// construction time and repositioned to that origin on PostInitialized —
// so it covers every monitor at once. Widgets drawn inside ImGui can
// therefore be dragged anywhere on any screen during Edit Mode.
//
// Render gates:
//   - Outside Edit Mode: only paint widgets when the host process
//     (iRacing) holds the foreground. That keeps the overlay invisible
//     when the user alt-tabs to other apps.
//   - In Edit Mode: render unconditionally so the user can position
//     things even with iRacing minimised.
//
// Click-through: CTO automatically toggles WS_EX_TRANSPARENT based on
// ImGui.GetIO().WantCaptureMouse. With every widget using NoInputs
// (locked mode), WantCaptureMouse stays false and the flag stays set —
// clicks always pass through. In Edit Mode the widgets DO accept input
// so the flag flaps as the cursor enters/leaves each widget; this is the
// expected behavior and ObserveExStyle silently skips logging in that
// mode.
public sealed class RadarOverlay : ClickableTransparentOverlay.Overlay
{
    private readonly RadarFrameBuffer _frames;
    private readonly HostProcessDetector _hostDetector;
    private readonly MonitorLocator _monitorLocator;
    private readonly OverlayWindowMover _windowMover;
    private readonly WindowStyleManager _styleManager;
    private readonly EditModeController _editMode;
    private readonly WidgetLayoutManager _layouts;
    private readonly Action<string> _log;

    private bool _positionedToVirtualDesktop;
    private int _lastReportedExStyle;
    private DateTime _lastStyleLogUtc = DateTime.MinValue;

    public RadarOverlay(
        int width,
        int height,
        RadarFrameBuffer frames,
        HostProcessDetector hostDetector,
        MonitorLocator monitorLocator,
        OverlayWindowMover windowMover,
        WindowStyleManager styleManager,
        EditModeController editMode,
        WidgetLayoutManager layouts,
        Action<string>? log = null)
        : base("iRadar Overlay", width, height)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(hostDetector);
        ArgumentNullException.ThrowIfNull(monitorLocator);
        ArgumentNullException.ThrowIfNull(windowMover);
        ArgumentNullException.ThrowIfNull(styleManager);
        ArgumentNullException.ThrowIfNull(editMode);
        ArgumentNullException.ThrowIfNull(layouts);

        _frames = frames;
        _hostDetector = hostDetector;
        _monitorLocator = monitorLocator;
        _windowMover = windowMover;
        _styleManager = styleManager;
        _editMode = editMode;
        _layouts = layouts;
        _log = log ?? (_ => { });

        VSync = true;
    }

    protected override Task PostInitialized()
    {
        TryPositionToVirtualDesktop();
        return Task.CompletedTask;
    }

    protected override void Render()
    {
        _editMode.Tick();

        // Defensive retry — if PostInitialized fired before our HWND was
        // ready, keep trying once a frame until it sticks.
        if (!_positionedToVirtualDesktop)
        {
            TryPositionToVirtualDesktop();
        }

        ObserveExStyle();

        // In Edit Mode the user is positioning widgets — render even if
        // iRacing isn't focused. In locked mode hide everything when not in
        // game so widgets don't leak into other apps.
        if (!_editMode.IsActive && !_hostDetector.IsHostInForeground())
        {
            return;
        }

        var snapshot = _frames.Snapshot;
        var frame = _frames.Frame;

        StatusWidget.Draw(snapshot, frame, _layouts, _editMode.IsActive);
        RadarWidget.Draw(frame, _layouts, _editMode.IsActive);
        RelativeWidget.Draw(frame, _layouts, _editMode.IsActive);

        if (_editMode.IsActive)
        {
            EditModeBanner.Draw();
        }
    }

    private void TryPositionToVirtualDesktop()
    {
        try
        {
            var bounds = _monitorLocator.GetVirtualDesktopBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var ourHwnd = _windowMover.GetCurrentProcessMainWindow();
            if (ourHwnd == IntPtr.Zero) return;

            if (_windowMover.TryMove(ourHwnd, bounds.X, bounds.Y))
            {
                _positionedToVirtualDesktop = true;
                _log($"[overlay] positioned to virtual desktop {bounds.Width}x{bounds.Height} at ({bounds.X}, {bounds.Y})");
            }
        }
        catch (Exception ex)
        {
            _log($"[overlay] reposition error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ObserveExStyle()
    {
        // Expected behavior: in Edit Mode CTO toggles WS_EX_TRANSPARENT as
        // the cursor enters and leaves widgets (so the user can grab them).
        // Logging every flap would flood the file — skip in edit mode.
        if (_editMode.IsActive) return;

        try
        {
            var hwnd = _windowMover.GetCurrentProcessMainWindow();
            if (hwnd == IntPtr.Zero) return;

            var current = _styleManager.ReadExStyle(hwnd);
            if (current == _lastReportedExStyle) return;

            var now = DateTime.UtcNow;
            if (now - _lastStyleLogUtc < TimeSpan.FromMilliseconds(500)) return;

            _lastStyleLogUtc = now;
            _log($"[overlay] hwnd=0x{hwnd.ToInt64():X} exstyle: 0x{_lastReportedExStyle:X8} -> 0x{current:X8}");
            _lastReportedExStyle = current;
        }
        catch (Exception ex)
        {
            _log($"[overlay] observe-exstyle exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
