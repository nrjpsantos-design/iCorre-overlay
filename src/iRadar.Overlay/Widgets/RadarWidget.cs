using System.Numerics;
using iRadar.Core.Radar;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Circular radar — player drawn as a white vertical rectangle at the center,
// other cars as colored vertical rectangles. Close cars get a translucent
// orange halo, Danger cars a translucent red halo, replacing the old
// side-bar SpotterWidget with proximity feedback rendered IN the radar
// itself (per user reference imagery).
//
// Coordinate translation:
//   world.X (ahead, +)   →  screen.Y (-)    (up is "ahead")
//   world.Y (right, +)   →  screen.X (+)    (right is "right")
//
// Range rings (full and half radius) give a scale reference; the half-ring
// shows a `Nm` label.
internal static class RadarWidget
{
    private const string Title = "iRadar — Radar";
    private const float DefaultRangeMeters = 50f;
    private const float HalfRingLabelPadding = 4f;

    // Car-rectangle dimensions in screen pixels. Vertical orientation
    // (taller than wide) so they read as "car bodies" pointed along the
    // direction of travel — same shape for player and others, just colored
    // differently.
    private const float CarWidthPx = 8f;
    private const float CarHeightPx = 16f;
    private const float HaloRadiusPx = 16f;
    private const float HaloOuterRadiusPx = 24f;

    private static readonly Vector2 DefaultPos = new(20f, 200f);
    private static readonly Vector2 DefaultSize = new(260f, 260f);

    public static void Draw(RadarFrame? frame, float rangeMeters = DefaultRangeMeters)
    {
        ImGui.SetNextWindowPos(DefaultPos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(WidgetTheme.DefaultBgAlpha);

        if (!ImGui.Begin(Title, WidgetTheme.WidgetFlags))
        {
            ImGui.End();
            return;
        }

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        var center = new Vector2(winPos.X + (winSize.X / 2f), winPos.Y + (winSize.Y / 2f));
        var radius = (MathF.Min(winSize.X, winSize.Y) / 2f) - 14f;
        if (radius <= 0f)
        {
            ImGui.End();
            return;
        }

        var pxPerMeter = radius / rangeMeters;
        var ringColor = WidgetTheme.U32(WidgetTheme.RangeRing);

        // Range rings (outer = full range, inner = half range).
        drawList.AddCircle(center, radius, ringColor, 64, 1.5f);
        drawList.AddCircle(center, radius * 0.5f, ringColor, 48, 1.0f);

        // Subtle cross-hair so it's clear which way is forward.
        drawList.AddLine(
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X, center.Y + radius),
            ringColor, 1.0f);
        drawList.AddLine(
            new Vector2(center.X - radius, center.Y),
            new Vector2(center.X + radius, center.Y),
            ringColor, 1.0f);

        // Range label on the half ring.
        var labelText = $"{rangeMeters / 2f:F0}m";
        drawList.AddText(
            new Vector2(center.X + (radius * 0.5f) + HalfRingLabelPadding, center.Y + HalfRingLabelPadding),
            WidgetTheme.U32(WidgetTheme.PanelLabel),
            labelText);

        // Other cars (drawn before the player so the player rectangle is on
        // top of any overlapping halos).
        if (frame is { IsActive: true })
        {
            foreach (var dot in frame.Dots)
            {
                // world.X = ahead (+) / behind (-). world.Y = right (+) / left (-).
                // Screen: +X right, +Y down. So screen.X = +world.Y, screen.Y = -world.X.
                var dx = dot.Y * pxPerMeter;
                var dy = -dot.X * pxPerMeter;
                var screen = new Vector2(center.X + dx, center.Y + dy);

                // Clip anything outside the visible ring.
                var sqDist = (dx * dx) + (dy * dy);
                if (sqDist > radius * radius) continue;

                DrawHalo(drawList, screen, dot.Threat);
                DrawCarRect(drawList, screen, WidgetTheme.ColorFor(dot.Threat), isPlayer: false);
            }
        }

        // Player at center — drawn last so it's on top.
        DrawCarRect(drawList, center, WidgetTheme.PlayerFill, isPlayer: true);

        ImGui.End();
    }

    private static void DrawHalo(ImDrawListPtr drawList, Vector2 center, ThreatLevel threat)
    {
        var halo = WidgetTheme.HaloFor(threat);
        if (halo.W <= 0f) return;

        // Two-layer halo: a brighter inner disc and a softer outer ring give
        // the "glow" feel from the reference images without needing any
        // shader work.
        var outer = halo;
        outer.W *= 0.5f;
        drawList.AddCircleFilled(center, HaloOuterRadiusPx, WidgetTheme.U32(outer), 24);
        drawList.AddCircleFilled(center, HaloRadiusPx, WidgetTheme.U32(halo), 24);
    }

    private static void DrawCarRect(ImDrawListPtr drawList, Vector2 center, Vector4 fill, bool isPlayer)
    {
        var halfW = CarWidthPx / 2f;
        var halfH = CarHeightPx / 2f;
        var tl = new Vector2(center.X - halfW, center.Y - halfH);
        var br = new Vector2(center.X + halfW, center.Y + halfH);

        drawList.AddRectFilled(tl, br, WidgetTheme.U32(fill), 2.0f);

        // Subtle dark outline only on the player so the white rectangle
        // doesn't blend into bright skies / track surfaces.
        if (isPlayer)
        {
            drawList.AddRect(tl, br, WidgetTheme.U32(WidgetTheme.PlayerBorder), 2.0f, ImDrawFlags.None, 1.2f);
        }
    }
}
