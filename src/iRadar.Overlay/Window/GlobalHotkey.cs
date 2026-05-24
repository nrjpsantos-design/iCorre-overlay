using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Polls keyboard state for the Ctrl+Alt+E chord and detects the rising edge
// (key down once → fire once, even if held). Uses user32!GetAsyncKeyState,
// which reads the *global* keyboard state regardless of which window is in
// foreground — so the user can toggle Edit Mode while iRacing has focus
// without needing a Win32 message hook.
//
// Anti-cheat note: GetAsyncKeyState is a read-only keyboard query. It does
// NOT install a low-level keyboard hook, does NOT touch iRacing's input
// queue, and does NOT inject any key events. It's the same primitive used
// by Task Manager, streaming software, and every overlay framework.
public sealed class GlobalHotkey
{
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU    = 0x12;   // Alt
    private const int VK_E       = 0x45;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private bool _chordWasDown;

    // Returns true once when the user transitions from "chord not held" to
    // "chord held". Held state returns false on subsequent calls until the
    // user releases at least one of the modifier keys.
    public bool ConsumeIfTriggered_CtrlAltE()
    {
        var chordDown = IsDown(VK_CONTROL) && IsDown(VK_MENU) && IsDown(VK_E);

        var fired = chordDown && !_chordWasDown;
        _chordWasDown = chordDown;
        return fired;
    }

    private static bool IsDown(int vKey)
        => (GetAsyncKeyState(vKey) & 0x8000) != 0;
}
