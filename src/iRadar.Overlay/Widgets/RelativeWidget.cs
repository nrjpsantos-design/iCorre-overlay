using System.Globalization;
using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Settings;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Textual list of nearby cars: who's ahead and who's behind, each with
// position, time gap, car number, driver name, iRating delta vs. the
// focused car, and a "(pit)" suffix when applicable. The RadarFrame
// already provides the lists sorted closest-first and capped by
// RadarSettings.RelativePanelMaxCarsPerSide.
//
// Multiclass: entries whose ClassId differs from the focused car's are
// dimmed to muted gray. This is the cheapest readable cue — in a true
// multiclass field the player only races their own class for position,
// and other-class cars are just traffic.
internal static class RelativeWidget
{
    private const string Title = "iRadar — Relative";
    private const int DriverNameMaxLength = 14;

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
            if (editMode)
            {
                var visible = WidgetHelper.DrawVisibilityToggle(WidgetIds.Relative, "Relative", layouts);
                if (!visible)
                {
                    ImGui.TextColored(WidgetTheme.MutedText, "(hidden during racing)");
                    return;
                }
                ImGui.Separator();
            }

            if (frame is null || !frame.IsActive)
            {
                if (editMode)
                {
                    ImGui.TextColored(WidgetTheme.MutedText, "(Relative panel — no data yet)");
                }
                return;
            }

            ImGui.TextColored(WidgetTheme.PanelLabel, "AHEAD");
            DrawSection(frame.Ahead, WidgetTheme.ApproachAhead, frame);

            ImGui.Separator();

            ImGui.TextColored(WidgetTheme.PanelLabel, "BEHIND");
            DrawSection(frame.Behind, WidgetTheme.BehindFalling, frame);
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Relative, layouts, editMode);
        }
    }

    private static void DrawSection(IReadOnlyList<RelativeEntry> entries, Vector4 sectionColor, RadarFrame frame)
    {
        if (entries.Count == 0)
        {
            ImGui.TextColored(WidgetTheme.MutedText, "  (none in range)");
            return;
        }

        foreach (var e in entries)
        {
            var sameClass = frame.PlayerClassId == 0
                || e.ClassId == 0
                || e.ClassId == frame.PlayerClassId;

            var color = e.OnPitRoad ? WidgetTheme.MutedText
                : sameClass ? sectionColor
                : WidgetTheme.OtherClassText;

            ImGui.TextColored(color, FormatRow(e, frame.PlayerIRating));
        }
    }

    private static string FormatRow(RelativeEntry e, int playerIRating)
    {
        var pos = e.Position > 0 ? $"P{e.Position,-2}" : "P--";
        var gap = FormatGap(e.GapSeconds);
        var num = string.IsNullOrEmpty(e.CarNumber) ? "--" : e.CarNumber;
        var name = TruncateName(e.DriverName);
        var ir = FormatIRatingDelta(e.IRating, playerIRating);
        var pit = e.OnPitRoad ? "  (pit)" : string.Empty;
        return $"  {pos}  {gap,7}  #{num,-4} {name,-14} {ir,6}{pit}";
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

    // Compact iRating delta. Empty when either side is unknown (0).
    // Above ±1000 we collapse to "+1.2k" / "-2.4k" to stay readable in
    // the narrow column. iRacing iRating is an integer, never fractional.
    private static string FormatIRatingDelta(int otherIRating, int playerIRating)
    {
        if (otherIRating <= 0 || playerIRating <= 0) return string.Empty;

        var diff = otherIRating - playerIRating;
        if (diff == 0) return "  ±0";

        var sign = diff > 0 ? "+" : "-";
        var mag = Math.Abs(diff);
        if (mag >= 1000)
        {
            var k = mag / 1000.0;
            return $"{sign}{k.ToString("0.0", CultureInfo.InvariantCulture)}k";
        }
        return $"{sign}{mag}";
    }

    private static string TruncateName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "(unknown)";
        return name.Length <= DriverNameMaxLength ? name : name[..DriverNameMaxLength];
    }
}
