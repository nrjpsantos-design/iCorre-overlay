using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Wraps the user32 monitor-info APIs. Given any HWND, returns the pixel
// bounds of the monitor the window is on. Uses only read-only user32 calls
// — the same APIs every Windows app uses to query display layout.
public sealed class MonitorLocator
{
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

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
