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
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;

    // Resolves the HWND of the current process's main window. Returns
    // IntPtr.Zero if the window hasn't been created yet or can't be found.
    public IntPtr GetCurrentProcessMainWindow()
    {
        using var p = Process.GetCurrentProcess();
        p.Refresh();
        return p.MainWindowHandle;
    }

    // Move the window to (x, y) WITHOUT changing its size. Avoids triggering
    // the CTO/D3D11 swap-chain resize path, which is fragile and was the
    // cause of an invisible-window regression on first try.
    public bool TryMove(IntPtr hwnd, int x, int y)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (!IsWindow(hwnd)) return false;

        return SetWindowPos(
            hwnd,
            HwndTopmost,
            x, y,
            0, 0,
            SwpNoActivate | SwpShowWindow | SwpNoSize);
    }

    // Kept for callers that genuinely want to resize too; current production
    // path uses TryMove.
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
