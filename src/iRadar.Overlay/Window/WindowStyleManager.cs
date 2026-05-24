using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Toggles the overlay's interactivity by flipping the WS_EX_TRANSPARENT and
// WS_EX_NOACTIVATE extended-window-style bits.
//
//   click-through (default for showing the radar):
//     WS_EX_TRANSPARENT  — mouse events pass through to whatever is behind
//                          us (iRacing), so clicks on widget text don't
//                          steal focus from the game.
//     WS_EX_NOACTIVATE   — defense in depth: even if some code path tries
//                          to activate the window, it stays in background.
//
//   interactive (Fase 6 Edit Mode, not in this commit):
//     both bits cleared, so the user can drag and resize widgets.
//
// After every style change we call SetWindowPos with SWP_FRAMECHANGED so the
// non-client cache is invalidated and the new style takes effect immediately
// — without this, MSDN warns the change can be ignored until the next paint.
public sealed class WindowStyleManager
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE_SWP = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy,
        uint uFlags);

    public StyleResult MakeClickThrough(IntPtr hwnd)
        => Apply(hwnd, addFlags: WS_EX_TRANSPARENT | WS_EX_NOACTIVATE, removeFlags: 0);

    public StyleResult MakeInteractive(IntPtr hwnd)
        => Apply(hwnd, addFlags: 0, removeFlags: WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);

    public bool IsClickThrough(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        var current = GetWindowLong(hwnd, GWL_EXSTYLE);
        return (current & WS_EX_TRANSPARENT) != 0;
    }

    public int ReadExStyle(IntPtr hwnd)
        => hwnd == IntPtr.Zero ? 0 : GetWindowLong(hwnd, GWL_EXSTYLE);

    private static StyleResult Apply(IntPtr hwnd, int addFlags, int removeFlags)
    {
        if (hwnd == IntPtr.Zero)
        {
            return new StyleResult(false, "hwnd is zero", 0, 0);
        }

        var before = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (before == 0)
        {
            return new StyleResult(false, $"GetWindowLong returned 0 (err={Marshal.GetLastWin32Error()})", 0, 0);
        }

        var target = (before | addFlags) & ~removeFlags;
        if (target == before)
        {
            return new StyleResult(true, "style already matches target", before, before);
        }

        _ = SetWindowLong(hwnd, GWL_EXSTYLE, target);
        // SWP_FRAMECHANGED flushes the non-client cache so the new exstyle is
        // honored immediately. Without this MSDN explicitly warns the change
        // may not take effect.
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE_SWP | SWP_FRAMECHANGED);

        var after = GetWindowLong(hwnd, GWL_EXSTYLE);
        var success = after == target;
        var reason = success
            ? $"style updated 0x{before:X8} -> 0x{after:X8}"
            : $"verify mismatch: wanted 0x{target:X8}, got 0x{after:X8} (err={Marshal.GetLastWin32Error()})";

        return new StyleResult(success, reason, before, after);
    }
}

public readonly record struct StyleResult(bool Success, string Reason, int Before, int After);
