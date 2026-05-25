using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Settings;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Minimal radar — only paints the player rectangle, nearby car rectangles
// and the directional threat cone. No range rings, no crosshair, no half-
// ring label, no panel background (per user feedback: keep the in-game
// space clean and only show information when something needs attention).
//
// Visibility rules:
//   - Edit Mode             → always visible with a translucent background
//                             so the user can drag/resize the widget.
//   - Locked mode + no dots → hidden entirely (no anchor rectangle visible).
//   - Locked mode + dots    → visible, fully transparent background.
//
// Coordinate translation (player at origin):
//   world.X (ahead, +)   →  screen.Y (-)    (up is "ahead")
//   world.Y (right, +)   →  screen.X (+)    (right is "right")
//
// Default visible range is 10 m — tight enough to focus on imminent
// contact, which is where the radar matters most.
internal static class RadarWidget
{
    private const string Title = "iRadar — Radar";
    private const float DefaultRangeMeters = 10f;

    // Real-world car dimensions, used to size rectangles in pixels via
    // pxPerMeter at render time. The visual passing/overtaking transition
    // now mirrors the physical one: the other car's rectangle only fully
    // clears the player's when their bodies have actually fully cleared.
    // A 4.7×2 m approximation matches modern GT3 / TCR / road-touring cars;
    // open-wheelers and prototypes are within ±15%.
    private const float CarLengthMeters = 4.7f;
    private const float CarWidthMeters = 2.0f;

    // Floor sizes so the radar is still readable if the user shrinks the
    // widget far enough that proportional pixels would round to nothing.
    private const float MinCarWidthPx = 8f;
    private const float MinCarHeightPx = 16f;

    private const float ConeInnerRadiusPx = 56f;
    private const float ConeOuterRadiusPx = 72f;
    private const float ConeInnerHalfAngleRad = 0.55f;
    private const float ConeOuterHalfAngleRad = 0.75f;
    private const int ConeArcSegments = 18;

    public static void Draw(
        RadarFrame? frame,
        WidgetLayoutManager layouts,
        bool editMode,
        float rangeMeters = DefaultRangeMeters)
    {
        var hasDots = frame is { IsActive: true } && frame.Dots.Count > 0;

        // Locked mode hides the radar entirely when there's nothing to warn
        // about. Edit Mode always renders so the user can find and reposition
        // the widget.
        if (!editMode && !hasDots) return;

        // Transparent background in locked mode; visible background in Edit
        // Mode so the user can see/grab the widget bounds.
        var bgAlpha = editMode ? WidgetTheme.DefaultBgAlpha : 0f;

        if (!WidgetHelper.Begin(WidgetIds.Radar, Title, layouts, editMode, bgAlpha))
        {
            return;
        }

        try
        {
            if (editMode)
            {
                var visible = WidgetHelper.DrawVisibilityToggle(WidgetIds.Radar, "Radar", layouts);
                if (!visible)
                {
                    ImGui.TextColored(WidgetTheme.MutedText, "(hidden during racing)");
                    return;
                }
                ImGui.Separator();
            }

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

        // Scale the car rectangles to their real-world dimensions on the
        // same pxPerMeter scale the dots use. With a 10 m range at radius
        // ~116 px, this yields ~23 px wide × ~55 px tall — which means
        // bumper-to-bumper centre delta (~4.7 m) really IS bumper-to-bumper
        // visually, and a full pass on track matches a full pass on screen.
        var carWidthPx = MathF.Max(MinCarWidthPx, pxPerMeter * CarWidthMeters);
        var carHeightPx = MathF.Max(MinCarHeightPx, pxPerMeter * CarLengthMeters);

        // First pass: find the worst nearby threat for the directional cone.
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

        // Cone first so the rectangles paint on top of it.
        if (worstThreat is not null)
        {
            DrawThreatCone(drawList, center, worstThreatDirection, worstThreat.Threat);
        }

        // Other cars.
        if (frame is { IsActive: true })
        {
            foreach (var dot in frame.Dots)
            {
                var dx = dot.Y * pxPerMeter;
                var dy = -dot.X * pxPerMeter;
                var sqDist = (dx * dx) + (dy * dy);
                if (sqDist > radius * radius) continue;

                var screen = new Vector2(center.X + dx, center.Y + dy);
                DrawCarRect(drawList, screen, WidgetTheme.ColorFor(dot.Threat), carWidthPx, carHeightPx, isPlayer: false);
            }
        }

        // Player on top.
        DrawCarRect(drawList, center, WidgetTheme.PlayerFill, carWidthPx, carHeightPx, isPlayer: true);
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

        var angle = MathF.Atan2(directionPx.Y, directionPx.X);

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

    private static void DrawCarRect(
        ImDrawListPtr drawList,
        Vector2 center,
        Vector4 fill,
        float widthPx,
        float heightPx,
        bool isPlayer)
    {
        var halfW = widthPx / 2f;
        var halfH = heightPx / 2f;
        var tl = new Vector2(center.X - halfW, center.Y - halfH);
        var br = new Vector2(center.X + halfW, center.Y + halfH);

        drawList.AddRectFilled(tl, br, WidgetTheme.U32(fill), 2.5f);

        if (isPlayer)
        {
            drawList.AddRect(tl, br, WidgetTheme.U32(WidgetTheme.PlayerBorder), 2.5f, ImDrawFlags.None, 1.4f);
        }
    }
}
