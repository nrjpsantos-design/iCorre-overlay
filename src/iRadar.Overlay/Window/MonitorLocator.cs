using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Wraps the user32 monitor-info APIs. Given any HWND, returns the pixel
// bounds of the monitor the window is on. Also exposes the virtual-desktop
// bounding rectangle (union of all attached monitors) which the overlay
// uses to size itself so Edit Mode can drag widgets across the full screen.
//
// All calls are read-only user32 queries — the same APIs every Windows app
// uses to query display layout.
public sealed class MonitorLocator
{
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // Bounding rectangle of all monitors, in virtual-desktop coordinates.
    // X/Y can be negative if a monitor sits left/above the primary.
    public MonitorBounds GetVirtualDesktopBounds()
    {
        return new MonitorBounds(
            GetSystemMetrics(SM_XVIRTUALSCREEN),
            GetSystemMetrics(SM_YVIRTUALSCREEN),
            GetSystemMetrics(SM_CXVIRTUALSCREEN),
            GetSystemMetrics(SM_CYVIRTUALSCREEN));
    }

    public MonitorBounds? GetMonitorBoundsFor(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;

        var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (hMon == IntPtr.Zero) return null;

        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(hMon, ref info)) return null;

        return new MonitorBounds(
            info.Monitor.Left,
            info.Monitor.Top,
            info.Monitor.Right - info.Monitor.Left,
            info.Monitor.Bottom - info.Monitor.Top);
    }
}
