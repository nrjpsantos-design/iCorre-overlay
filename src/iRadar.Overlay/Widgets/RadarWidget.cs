using System.Numerics;
using iRadar.Core.Radar;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Circular radar — player at the center pointing up, other cars drawn as
// colored dots at scaled (x, y) positions taken from the RadarFrame.
//
// Coordinate translation:
//   world.X (ahead, +)   →  screen.Y (-)    (up is "ahead")
//   world.Y (right, +)   →  screen.X (+)    (right is "right")
//
// Range rings are drawn at half-radius and full-radius so the user has a
// quick scale reference. The full-radius ring matches DefaultRangeMeters.
internal static class RadarWidget
{
    private const string Title = "iRadar — Radar";
    private const float DefaultRangeMeters = 50f;
    private const float DotRadius = 4.0f;
    private const float PlayerTriangleSize = 7.0f;
    private const float HalfRingLabelPadding = 4f;

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

        // Player triangle, pointing up.
        var playerColor = WidgetTheme.U32(WidgetTheme.Player);
        drawList.AddTriangleFilled(
            new Vector2(center.X, center.Y - PlayerTriangleSize),
            new Vector2(center.X - (PlayerTriangleSize * 0.7f), center.Y + (PlayerTriangleSize * 0.6f)),
            new Vector2(center.X + (PlayerTriangleSize * 0.7f), center.Y + (PlayerTriangleSize * 0.6f)),
            playerColor);

        if (frame is not { IsActive: true })
        {
            ImGui.End();
            return;
        }

        foreach (var dot in frame.Dots)
        {
            // world.X = ahead (+) / behind (-). world.Y = right (+) / left (-).
            // Screen: +X right, +Y down. So screen.X = +world.Y, screen.Y = -world.X.
            var dx = dot.Y * pxPerMeter;
            var dy = -dot.X * pxPerMeter;
            var screen = new Vector2(center.X + dx, center.Y + dy);

            // Skip anything outside the visible ring (shouldn't happen if
            // RadarSettings.RadarRangeMeters <= rangeMeters, but be safe).
            var sqDist = (dx * dx) + (dy * dy);
            if (sqDist > radius * radius) continue;

            drawList.AddCircleFilled(screen, DotRadius, WidgetTheme.U32(WidgetTheme.ColorFor(dot.Threat)));
        }

        ImGui.End();
    }
}
