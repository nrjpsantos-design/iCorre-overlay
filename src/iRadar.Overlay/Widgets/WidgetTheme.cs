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
    // Palette inspired by the reference radar: white player marker, orange
    // for close cars, red for danger, with translucent halos as the
    // proximity indicator (replaced the old side-bar SpotterWidget).
    public static readonly Vector4 Safe         = new(0.55f, 0.55f, 0.55f, 0.70f);  // dim gray
    public static readonly Vector4 Close        = new(1.00f, 0.55f, 0.10f, 1.00f);  // orange
    public static readonly Vector4 Danger       = new(1.00f, 0.20f, 0.20f, 1.00f);  // red
    public static readonly Vector4 CloseHalo    = new(1.00f, 0.55f, 0.10f, 0.28f);  // translucent orange
    public static readonly Vector4 DangerHalo   = new(1.00f, 0.20f, 0.20f, 0.38f);  // translucent red
    public static readonly Vector4 PlayerFill   = new(0.98f, 0.98f, 0.98f, 1.00f);  // white
    public static readonly Vector4 PlayerBorder = new(0.15f, 0.15f, 0.15f, 0.90f);  // dark outline
    public static readonly Vector4 RangeRing    = new(0.50f, 0.50f, 0.50f, 0.35f);
    public static readonly Vector4 PanelLabel   = new(0.70f, 0.70f, 0.70f, 1.00f);
    public static readonly Vector4 ApproachAhead  = new(0.95f, 0.30f, 0.30f, 1.00f);  // ahead = closing in
    public static readonly Vector4 BehindFalling  = new(0.45f, 0.85f, 0.55f, 1.00f);  // behind = drifting back
    public static readonly Vector4 NeutralText = new(0.85f, 0.85f, 0.85f, 1.00f);
    public static readonly Vector4 MutedText   = new(0.60f, 0.60f, 0.60f, 0.80f);
    public static readonly Vector4 Waiting     = new(0.95f, 0.75f, 0.20f, 1.00f);

    public const float DefaultBgAlpha = 0.45f;

    public static Vector4 ColorFor(ThreatLevel t) => t switch
    {
        ThreatLevel.Danger => Danger,
        ThreatLevel.Close  => Close,
        _                  => Safe,
    };

    public static Vector4 HaloFor(ThreatLevel t) => t switch
    {
        ThreatLevel.Danger => DangerHalo,
        ThreatLevel.Close  => CloseHalo,
        _                  => default,   // no halo for Safe
    };

    public static uint U32(Vector4 c) => ImGui.ColorConvertFloat4ToU32(c);
}
