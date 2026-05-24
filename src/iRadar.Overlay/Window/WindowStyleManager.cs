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
// Only user32 style-bit APIs are used — no hooks, no injection, no foreign-
// process access.
public sealed class WindowStyleManager
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Returns true if the new flags differ from what's already set; false if
    // the call failed OR the bits were already in the requested state.
    public bool MakeClickThrough(IntPtr hwnd)
        => ApplyStyleChange(hwnd, addFlags: WS_EX_TRANSPARENT | WS_EX_NOACTIVATE, removeFlags: 0);

    public bool MakeInteractive(IntPtr hwnd)
        => ApplyStyleChange(hwnd, addFlags: 0, removeFlags: WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);

    public bool IsClickThrough(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        var current = GetWindowLong(hwnd, GWL_EXSTYLE);
        return (current & WS_EX_TRANSPARENT) != 0;
    }

    private static bool ApplyStyleChange(IntPtr hwnd, int addFlags, int removeFlags)
    {
        if (hwnd == IntPtr.Zero) return false;

        var current = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (current == 0) return false;

        var updated = (current | addFlags) & ~removeFlags;
        if (updated == current) return false;

        _ = SetWindowLong(hwnd, GWL_EXSTYLE, updated);
        // Verify the change took effect.
        var verify = GetWindowLong(hwnd, GWL_EXSTYLE);
        return verify == updated;
    }
}
