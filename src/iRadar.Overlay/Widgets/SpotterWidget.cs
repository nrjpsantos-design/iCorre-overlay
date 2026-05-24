using System.Numerics;
using iRadar.Core.Radar;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Spotter "blinkers" rendered as two narrow vertical bars on the left and
// right edges of the overlay window. Color reflects the current
// SpotterAlert:
//
//   Clear / CarLeft|Right on the other side  → bar invisible (alpha 0)
//   CarLeft or CarRight                       → yellow
//   CarLeftRight (both sides)                 → yellow on both edges
//   TwoCarsLeft / TwoCarsRight                → red (more urgent)
//
// The bars span the full height of the overlay viewport. Because the
// overlay window covers a region of the iRacing monitor, the bars sit at
// the edges of that region — close to the user's peripheral vision when
// the overlay covers most of the screen.
//
// Caveat for replay testing: iRacing only computes CarLeftRight while the
// player is driving live. In replay this widget will stay invisible — see
// docs/TESTING-FASE1.md.
internal static class SpotterWidget
{
    private const float BarWidth = 14f;
    private const float EdgeMargin = 6f;

    public static void Draw(SpotterAlert alert)
    {
        var leftColor = LeftColor(alert);
        var rightColor = RightColor(alert);

        // If both sides are clear, skip the work — no ImGui window opened.
        if (leftColor.W <= 0f && rightColor.W <= 0f) return;

        var viewport = ImGui.GetMainViewport();
        var vp = viewport.WorkPos;
        var vs = viewport.WorkSize;

        if (leftColor.W > 0f)
        {
            DrawBar("iRadar.Spotter.Left",
                pos: new Vector2(vp.X + EdgeMargin, vp.Y),
                size: new Vector2(BarWidth, vs.Y),
                color: leftColor);
        }
        if (rightColor.W > 0f)
        {
            DrawBar("iRadar.Spotter.Right",
                pos: new Vector2(vp.X + vs.X - BarWidth - EdgeMargin, vp.Y),
                size: new Vector2(BarWidth, vs.Y),
                color: rightColor);
        }
    }

    private static Vector4 LeftColor(SpotterAlert alert) => alert switch
    {
        SpotterAlert.CarLeft or SpotterAlert.CarLeftRight => WidgetTheme.Close,
        SpotterAlert.TwoCarsLeft => WidgetTheme.Danger,
        _ => default,
    };

    private static Vector4 RightColor(SpotterAlert alert) => alert switch
    {
        SpotterAlert.CarRight or SpotterAlert.CarLeftRight => WidgetTheme.Close,
        SpotterAlert.TwoCarsRight => WidgetTheme.Danger,
        _ => default,
    };

    private static void DrawBar(string id, Vector2 pos, Vector2 size, Vector4 color)
    {
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0f);  // window background invisible — we draw the bar manually

        var flags = WidgetTheme.WidgetFlags | ImGuiWindowFlags.NoBackground;
        if (!ImGui.Begin(id, flags))
        {
            ImGui.End();
            return;
        }

        var drawList = ImGui.GetWindowDrawList();
        var p = ImGui.GetWindowPos();
        var s = ImGui.GetWindowSize();
        drawList.AddRectFilled(
            p,
            new Vector2(p.X + s.X, p.Y + s.Y),
            WidgetTheme.U32(color));

        ImGui.End();
    }
}
