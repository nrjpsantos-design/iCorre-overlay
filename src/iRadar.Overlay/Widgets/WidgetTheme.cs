using System.Numerics;
using iRadar.Core.Radar;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Shared colors and helpers for the widget set. Keeping these centralized
// makes it easy to swap in a different palette (e.g., a "Quieter" theme
// later) without touching every widget.
internal static class WidgetTheme
{
    // Threat / status colors. All RGBA, 0..1.
    public static readonly Vector4 Safe        = new(0.55f, 0.75f, 0.55f, 0.85f);
    public static readonly Vector4 Close       = new(0.97f, 0.79f, 0.20f, 1.00f);
    public static readonly Vector4 Danger      = new(0.95f, 0.30f, 0.30f, 1.00f);
    public static readonly Vector4 Player      = new(0.30f, 0.85f, 0.45f, 1.00f);
    public static readonly Vector4 RangeRing   = new(0.50f, 0.50f, 0.50f, 0.35f);
    public static readonly Vector4 PanelLabel  = new(0.70f, 0.70f, 0.70f, 1.00f);
    public static readonly Vector4 ApproachAhead  = new(0.95f, 0.30f, 0.30f, 1.00f);  // ahead = closing in
    public static readonly Vector4 BehindFalling  = new(0.45f, 0.85f, 0.55f, 1.00f);  // behind = drifting back
    public static readonly Vector4 NeutralText = new(0.85f, 0.85f, 0.85f, 1.00f);
    public static readonly Vector4 MutedText   = new(0.60f, 0.60f, 0.60f, 0.80f);
    public static readonly Vector4 Waiting     = new(0.95f, 0.75f, 0.20f, 1.00f);

    public const float DefaultBgAlpha = 0.45f;

    // Standard set of flags for every iRadar widget — no inputs (so CTO
    // keeps WS_EX_TRANSPARENT and clicks reach iRacing), no chrome.
    public const ImGuiWindowFlags WidgetFlags =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoInputs
        | ImGuiWindowFlags.NoSavedSettings;

    public static Vector4 ColorFor(ThreatLevel t) => t switch
    {
        ThreatLevel.Danger => Danger,
        ThreatLevel.Close  => Close,
        _                  => Safe,
    };

    public static uint U32(Vector4 c) => ImGui.ColorConvertFloat4ToU32(c);
}
