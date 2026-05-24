using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Settings;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Circular radar — player drawn as a white vertical rectangle at the center,
// other cars as colored vertical rectangles. Proximity feedback is a
// directional CONE (sector) with its apex on the player and opening toward
// the single worst nearby threat. Player stays at the geometric center; the
// cone is the only thing that "rotates" with the threat direction.
//
// Coordinate translation (player at origin):
//   world.X (ahead, +)   →  screen.Y (-)    (up is "ahead")
//   world.Y (right, +)   →  screen.X (+)    (right is "right")
//
// Range rings (full and half radius) give a scale reference; the half-ring
// shows a `Nm` label. Default visible range is 15m — tight enough to focus on
// imminent contact, which is where the radar matters most.
internal static class RadarWidget
{
    private const string Title = "iRadar — Radar";
    private const float DefaultRangeMeters = 10f;
    private const float HalfRingLabelPadding = 4f;

    // Car-rectangle dimensions in screen pixels.
    private const float CarWidthPx = 12f;
    private const float CarHeightPx = 24f;

    // Threat cone — apex at player center, opens toward worst threat. Inner
    // bright sector + outer fainter sector give the glow effect without any
    // shader work.
    private const float ConeInnerRadiusPx = 56f;
    private const float ConeOuterRadiusPx = 72f;
    private const float ConeInnerHalfAngleRad = 0.55f;   // ~31°  → ~62° opening
    private const float ConeOuterHalfAngleRad = 0.75f;   // ~43°  → ~86° opening
    private const int ConeArcSegments = 18;

    public static void Draw(
        RadarFrame? frame,
        WidgetLayoutManager layouts,
        bool editMode,
        float rangeMeters = DefaultRangeMeters)
    {
        if (!WidgetHelper.Begin(WidgetIds.Radar, Title, layouts, editMode, WidgetTheme.DefaultBgAlpha))
        {
            return;
        }

        try
        {
            DrawRadarContents(frame, rangeMeters);
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Radar, layouts, editMode);
        }
    }

    private static void DrawRadarContents(RadarFrame? frame, float rangeMeters)
    {
        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        var center = new Vector2(winPos.X + (winSize.X / 2f), winPos.Y + (winSize.Y / 2f));
        var radius = (MathF.Min(winSize.X, winSize.Y) / 2f) - 14f;
        if (radius <= 0f) return;

        var pxPerMeter = radius / rangeMeters;
        var ringColor = WidgetTheme.U32(WidgetTheme.RangeRing);

        // Range rings + crosshair for orientation.
        drawList.AddCircle(center, radius, ringColor, 64, 1.5f);
        drawList.AddCircle(center, radius * 0.5f, ringColor, 48, 1.0f);
        drawList.AddLine(
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X, center.Y + radius),
            ringColor, 1.0f);
        drawList.AddLine(
            new Vector2(center.X - radius, center.Y),
            new Vector2(center.X + radius, center.Y),
            ringColor, 1.0f);

        var labelText = $"{rangeMeters / 2f:F0}m";
        drawList.AddText(
            new Vector2(center.X + (radius * 0.5f) + HalfRingLabelPadding, center.Y + HalfRingLabelPadding),
            WidgetTheme.U32(WidgetTheme.PanelLabel),
            labelText);

        // First pass: find the worst nearby threat — used to position the
        // player's directional halo. "Worst" = highest ThreatLevel, ties
        // broken by closer Euclidean distance.
        RadarDot? worstThreat = null;
        var worstThreatDistSq = float.MaxValue;
        var worstThreatDirection = Vector2.Zero;

        if (frame is { IsActive: true })
        {
            foreach (var dot in frame.Dots)
            {
                if (dot.Threat == ThreatLevel.Safe) continue;

                var dx = dot.Y * pxPerMeter;
                var dy = -dot.X * pxPerMeter;
                var sqDist = (dx * dx) + (dy * dy);
                if (sqDist > radius * radius) continue;

                var isWorse = worstThreat is null
                    || (int)dot.Threat > (int)worstThreat.Threat
                    || ((int)dot.Threat == (int)worstThreat.Threat && sqDist < worstThreatDistSq);

                if (isWorse)
                {
                    worstThreat = dot;
                    worstThreatDistSq = sqDist;
                    worstThreatDirection = new Vector2(dx, dy);
                }
            }
        }

        // Threat cone, drawn FIRST so dots and the player rectangle paint on
        // top of it.
        if (worstThreat is not null)
        {
            DrawThreatCone(drawList, center, worstThreatDirection, worstThreat.Threat);
        }

        // Other cars — plain colored rectangles, no individual halos (the
        // directional player halo above is the single proximity indicator).
        if (frame is { IsActive: true })
        {
            foreach (var dot in frame.Dots)
            {
                var dx = dot.Y * pxPerMeter;
                var dy = -dot.X * pxPerMeter;
                var sqDist = (dx * dx) + (dy * dy);
                if (sqDist > radius * radius) continue;

                var screen = new Vector2(center.X + dx, center.Y + dy);
                DrawCarRect(drawList, screen, WidgetTheme.ColorFor(dot.Threat), isPlayer: false);
            }
        }

        // Player on top of everything.
        DrawCarRect(drawList, center, WidgetTheme.PlayerFill, isPlayer: true);
    }

    private static void DrawThreatCone(
        ImDrawListPtr drawList,
        Vector2 apex,
        Vector2 directionPx,
        ThreatLevel threat)
    {
        var color = WidgetTheme.HaloFor(threat);
        if (color.W <= 0f) return;

        var length = MathF.Sqrt((directionPx.X * directionPx.X) + (directionPx.Y * directionPx.Y));
        if (length < 0.01f) return;

        // Angle of the threat relative to the +X axis (ImGui screen space:
        // +X right, +Y down, atan2 returns radians in (-π, π]).
        var angle = MathF.Atan2(directionPx.Y, directionPx.X);

        // Outer (fainter, wider) layer first, then inner (brighter, tighter)
        // — same trick as the offset halo but as a sector that points exactly
        // at the threat.
        var outer = color;
        outer.W *= 0.45f;
        DrawCone(drawList, apex, angle, ConeOuterRadiusPx, ConeOuterHalfAngleRad, outer);
        DrawCone(drawList, apex, angle, ConeInnerRadiusPx, ConeInnerHalfAngleRad, color);
    }

    private static void DrawCone(
        ImDrawListPtr drawList,
        Vector2 apex,
        float centerAngle,
        float radius,
        float halfAngle,
        Vector4 color)
    {
        drawList.PathClear();
        drawList.PathLineTo(apex);
        drawList.PathArcTo(
            apex,
            radius,
            centerAngle - halfAngle,
            centerAngle + halfAngle,
            ConeArcSegments);
        drawList.PathFillConvex(WidgetTheme.U32(color));
    }

    private static void DrawCarRect(ImDrawListPtr drawList, Vector2 center, Vector4 fill, bool isPlayer)
    {
        var halfW = CarWidthPx / 2f;
        var halfH = CarHeightPx / 2f;
        var tl = new Vector2(center.X - halfW, center.Y - halfH);
        var br = new Vector2(center.X + halfW, center.Y + halfH);

        drawList.AddRectFilled(tl, br, WidgetTheme.U32(fill), 2.5f);

        // Subtle dark outline only on the player so the white rectangle
        // doesn't disappear against bright skies or pale tarmac.
        if (isPlayer)
        {
            drawList.AddRect(tl, br, WidgetTheme.U32(WidgetTheme.PlayerBorder), 2.5f, ImDrawFlags.None, 1.4f);
        }
    }
}
