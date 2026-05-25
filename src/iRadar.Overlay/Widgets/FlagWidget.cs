using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Settings;
using iRadar.Core.Telemetry;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Race-control flag banner. Renders a coloured stripe with the flag label
// (YELLOW / SAFETY CAR / BLUE / RED / BLACK / WHITE / CHECKERED) whenever
// iRacing's SessionFlags reports a meaningful condition.
//
// Visibility rules mirror RadarWidget:
//   - Edit Mode             → always visible with a translucent background
//                             showing a sample "SAFETY CAR" so the user can
//                             see/position the widget.
//   - Locked + no flag      → hidden (nothing to warn about).
//   - Locked + active flag  → vivid coloured banner, no window chrome.
internal static class FlagWidget
{
    private const string Title = "iRadar — Flag";

    public static void Draw(
        TelemetrySnapshot? snapshot,
        WidgetLayoutManager layouts,
        bool editMode)
    {
        var flag = snapshot is null
            ? FlagState.None
            : SessionFlagClassifier.Classify(snapshot.Flags);

        // Hide entirely when nothing is happening (locked mode).
        if (!editMode && flag == FlagState.None) return;

        var bgAlpha = editMode ? WidgetTheme.DefaultBgAlpha : 0f;

        if (!WidgetHelper.Begin(WidgetIds.Flag, Title, layouts, editMode, bgAlpha))
        {
            return;
        }

        try
        {
            if (editMode)
            {
                var visible = WidgetHelper.DrawVisibilityToggle(WidgetIds.Flag, "Flag", layouts);
                if (!visible)
                {
                    ImGui.TextColored(WidgetTheme.MutedText, "(hidden during racing)");
                    return;
                }
                ImGui.Separator();

                // Show a sample so the user can preview colors / positioning
                // even when no actual flag is active.
                var sample = flag == FlagState.None ? FlagState.SafetyCar : flag;
                DrawBanner(sample);
            }
            else
            {
                DrawBanner(flag);
            }
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Flag, layouts, editMode);
        }
    }

    private static void DrawBanner(FlagState flag)
    {
        if (flag == FlagState.None) return;

        var (label, fill, fg) = StyleFor(flag);

        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();

        // Coloured fill stripe sized to the widget's content region.
        var br = new Vector2(pos.X + avail.X, pos.Y + avail.Y);
        drawList.AddRectFilled(pos, br, WidgetTheme.U32(fill), 4f);

        // Center the label vertically and horizontally inside the stripe.
        var textSize = ImGui.CalcTextSize(label);
        var textPos = new Vector2(
            pos.X + ((avail.X - textSize.X) / 2f),
            pos.Y + ((avail.Y - textSize.Y) / 2f));
        drawList.AddText(textPos, WidgetTheme.U32(fg), label);

        // Reserve the space we drew into so ImGui's layout cursor advances.
        ImGui.Dummy(avail);
    }

    // Color + text per flag state. Picked to match the conventions a sim
    // racer already recognises from on-screen flag icons in iRacing itself.
    private static (string label, Vector4 fill, Vector4 fg) StyleFor(FlagState flag) => flag switch
    {
        FlagState.SafetyCar  => ("SAFETY CAR", new(1.00f, 0.85f, 0.15f, 0.92f), new(0.10f, 0.05f, 0.00f, 1f)),
        FlagState.Yellow     => ("YELLOW",     new(1.00f, 0.92f, 0.30f, 0.90f), new(0.10f, 0.05f, 0.00f, 1f)),
        FlagState.Red        => ("RED FLAG",   new(0.95f, 0.15f, 0.15f, 0.92f), new(1.00f, 1.00f, 1.00f, 1f)),
        FlagState.Blue       => ("BLUE",       new(0.18f, 0.45f, 0.95f, 0.92f), new(1.00f, 1.00f, 1.00f, 1f)),
        FlagState.White      => ("WHITE — LAST LAP", new(0.95f, 0.95f, 0.95f, 0.92f), new(0.10f, 0.10f, 0.10f, 1f)),
        FlagState.Checkered  => ("CHECKERED",  new(0.15f, 0.15f, 0.15f, 0.92f), new(1.00f, 1.00f, 1.00f, 1f)),
        FlagState.Green      => ("GREEN",      new(0.20f, 0.75f, 0.30f, 0.90f), new(1.00f, 1.00f, 1.00f, 1f)),
        FlagState.Black      => ("BLACK FLAG", new(0.05f, 0.05f, 0.05f, 0.95f), new(1.00f, 0.85f, 0.15f, 1f)),
        FlagState.Disqualify => ("DSQ",        new(0.70f, 0.05f, 0.05f, 0.95f), new(1.00f, 1.00f, 1.00f, 1f)),
        _                    => (string.Empty, default, default),
    };
}
