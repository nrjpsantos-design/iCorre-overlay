namespace iRadar.Overlay.Window;

// Pixel-space rectangle of a physical monitor. X/Y can be negative when the
// host monitor is positioned left/above the primary in the virtual-desktop
// coordinate system — common with multi-monitor setups.
public readonly record struct MonitorBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
