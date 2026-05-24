using System.Diagnostics;
using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Repositions our own overlay window via SetWindowPos. Used to move the
// transparent window onto whichever monitor iRacing is currently displayed
// on, so the ImGui widgets actually appear over the game.
public sealed class OverlayWindowMover
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x, int y,
        int cx, int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    // Resolves the HWND of the current process's main window. Returns
    // IntPtr.Zero if the window hasn't been created yet or can't be found.
    public IntPtr GetCurrentProcessMainWindow()
    {
        using var p = Process.GetCurrentProcess();
        p.Refresh();
        return p.MainWindowHandle;
    }

    public bool TryMoveAndResize(IntPtr hwnd, MonitorBounds bounds)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (!IsWindow(hwnd)) return false;

        return SetWindowPos(
            hwnd,
            HwndTopmost,
            bounds.X, bounds.Y,
            bounds.Width, bounds.Height,
            SwpNoActivate | SwpShowWindow);
    }
}
