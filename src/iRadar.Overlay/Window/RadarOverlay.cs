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
public sealed class RadarOverlay : ClickableTransparentOverlay.Overlay
{
    private readonly RadarFrameBuffer _frames;
    private readonly HostProcessDetector _hostDetector;

    public RadarOverlay(RadarFrameBuffer frames, HostProcessDetector hostDetector)
        : base("iRadar Overlay")
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(hostDetector);

        _frames = frames;
        _hostDetector = hostDetector;

        // Use VSync to match the display refresh and keep GPU load low.
        VSync = true;
    }

    protected override void Render()
    {
        if (!_hostDetector.IsHostInForeground())
        {
            return;
        }

        var snapshot = _frames.Snapshot;
        var frame = _frames.Frame;

        HelloWidget.Draw(snapshot, frame);
    }
}
