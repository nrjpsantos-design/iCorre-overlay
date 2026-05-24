using System.Globalization;
using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Settings;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Textual list of nearby cars: who's ahead and who's behind, each with
// time gap, car number, driver name, and a "(pit)" suffix when applicable.
// The RadarFrame already provides the lists sorted closest-first and capped
// by RadarSettings.RelativePanelMaxCarsPerSide.
internal static class RelativeWidget
{
    private const string Title = "iRadar — Relative";
    private const int DriverNameMaxLength = 16;

    public static void Draw(RadarFrame? frame, WidgetLayoutManager layouts, bool editMode)
    {
        // In Edit Mode we still want this draggable even when there's no
        // active frame yet, so the user can position it pre-race. Show a
        // tiny placeholder when frame is null/inactive.
        if (!WidgetHelper.Begin(WidgetIds.Relative, Title, layouts, editMode, WidgetTheme.DefaultBgAlpha))
        {
            return;
        }

        try
        {
            if (frame is null || !frame.IsActive)
            {
                if (editMode)
                {
                    ImGui.TextColored(WidgetTheme.MutedText, "(Relative panel — no data yet)");
                }
                return;
            }

            ImGui.TextColored(WidgetTheme.PanelLabel, "AHEAD");
            DrawSection(frame.Ahead, WidgetTheme.ApproachAhead);

            ImGui.Separator();

            ImGui.TextColored(WidgetTheme.PanelLabel, "BEHIND");
            DrawSection(frame.Behind, WidgetTheme.BehindFalling);
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Relative, layouts, editMode);
        }
    }

    private static void DrawSection(IReadOnlyList<RelativeEntry> entries, Vector4 sectionColor)
    {
        if (entries.Count == 0)
        {
            ImGui.TextColored(WidgetTheme.MutedText, "  (none in range)");
            return;
        }

        foreach (var e in entries)
        {
            var color = e.OnPitRoad ? WidgetTheme.MutedText : sectionColor;
            ImGui.TextColored(color, FormatRow(e));
        }
    }

    private static string FormatRow(RelativeEntry e)
    {
        var gap = FormatGap(e.GapSeconds);
        var num = string.IsNullOrEmpty(e.CarNumber) ? "--" : e.CarNumber;
        var name = TruncateName(e.DriverName);
        var pit = e.OnPitRoad ? "  (pit)" : string.Empty;
        return $"  {gap,7}  #{num,-4} {name}{pit}";
    }

    private static string FormatGap(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            return "  --";
        }
        var sign = seconds > 0f ? "+" : seconds < 0f ? "-" : " ";
        var magnitude = MathF.Abs(seconds);
        return $"{sign}{magnitude.ToString("F2", CultureInfo.InvariantCulture)}s";
    }

    private static string TruncateName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "(unknown)";
        return name.Length <= DriverNameMaxLength ? name : name[..DriverNameMaxLength];
    }
}
