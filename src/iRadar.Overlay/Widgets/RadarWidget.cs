using System.Numerics;
using iRadar.Core.Radar;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Circular radar — player drawn as a white vertical rectangle at the center,
// other cars as colored vertical rectangles. The translucent proximity halo
// is centered on the PLAYER (not on the other cars) and is offset slightly
// in the direction of the worst nearby threat, so the user gets a directional
// cue ("car closing in from the left-rear") without losing the player as the
// visual anchor.
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
    private const float DefaultRangeMeters = 15f;
    private const float HalfRingLabelPadding = 4f;

    // Car-rectangle dimensions in screen pixels. Made larger than the Fase 5.2
    // version per user feedback for easier glanceability while racing.
    private const float CarWidthPx = 12f;
    private const float CarHeightPx = 24f;

    // Directional player halo — leans toward the worst threat. Offset is the
    // pixel distance from player center to halo center; radii are the inner
    // (bright) and outer (softer) glow rings.
    private const float HaloOffsetPx = 14f;
    private const float HaloInnerRadiusPx = 22f;
    private const float HaloOuterRadiusPx = 34f;

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

        // Player halo, drawn FIRST so dots and the player rectangle paint on
        // top of it.
        if (worstThreat is not null)
        {
            DrawPlayerHalo(drawList, center, worstThreatDirection, worstThreat.Threat);
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

        ImGui.End();
    }

    private static void DrawPlayerHalo(
        ImDrawListPtr drawList,
        Vector2 playerCenter,
        Vector2 directionPx,
        ThreatLevel threat)
    {
        var halo = WidgetTheme.HaloFor(threat);
        if (halo.W <= 0f) return;

        // Normalize the direction so the halo offset is constant in pixels
        // regardless of how far away the threat is.
        var length = MathF.Sqrt((directionPx.X * directionPx.X) + (directionPx.Y * directionPx.Y));
        if (length < 0.01f) return;
        var ux = directionPx.X / length;
        var uy = directionPx.Y / length;

        var haloCenter = new Vector2(
            playerCenter.X + (ux * HaloOffsetPx),
            playerCenter.Y + (uy * HaloOffsetPx));

        // Two-layer glow: a softer outer ring + brighter inner disc.
        var outer = halo;
        outer.W *= 0.5f;
        drawList.AddCircleFilled(haloCenter, HaloOuterRadiusPx, WidgetTheme.U32(outer), 28);
        drawList.AddCircleFilled(haloCenter, HaloInnerRadiusPx, WidgetTheme.U32(halo), 24);
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
