namespace iRadar.Overlay.Window;

// State machine for Edit Mode. Polls the hotkey each frame; when the chord
// fires, flips IsActive and notifies the subscriber. The subscriber is the
// composition root, which persists the current widget layout to disk on
// transition off and (optionally) re-asserts initial state on transition on.
public sealed class EditModeController
{
    private readonly GlobalHotkey _hotkey;
    private readonly Action<bool> _onToggled;

    public bool IsActive { get; private set; }

    public EditModeController(GlobalHotkey hotkey, Action<bool> onToggled)
    {
        ArgumentNullException.ThrowIfNull(hotkey);
        ArgumentNullException.ThrowIfNull(onToggled);
        _hotkey = hotkey;
        _onToggled = onToggled;
    }

    // Call once per render frame.
    public void Tick()
    {
        if (!_hotkey.ConsumeIfTriggered_CtrlAltE()) return;

        IsActive = !IsActive;
        _onToggled(IsActive);
    }
}
