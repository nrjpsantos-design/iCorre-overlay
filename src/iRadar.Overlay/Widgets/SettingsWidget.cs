using iRadar.Core.Radar;
using iRadar.Core.Settings;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Tunable engine knobs exposed via ImGui sliders. Only ever rendered while
// the user is in Edit Mode — locked mode hides this entirely (returns
// early in Draw). The widget has a normal WidgetLayout so the user can
// drag/resize it the same way they reposition the radar or status HUD.
//
// Persistence: the widget calls `onChange` whenever a slider moves; the
// composition root keeps the live RadarSettings reference, pushes the new
// value into RadarEngine immediately, and writes UserSettings to disk on
// Edit Mode exit (same path that persists widget layouts).
internal static class SettingsWidget
{
    private const string Title = "iRadar — Settings";

    public static void Draw(
        RadarSettings current,
        WidgetLayoutManager layouts,
        bool editMode,
        Action<RadarSettings> onChange)
    {
        // The whole point of the settings panel is to be invisible during
        // racing. Skip rendering outside Edit Mode regardless of the
        // visibility flag — there's nothing useful here while driving.
        if (!editMode) return;

        if (!WidgetHelper.Begin(WidgetIds.Settings, Title, layouts, editMode, WidgetTheme.DefaultBgAlpha))
        {
            return;
        }

        try
        {
            ImGui.TextColored(WidgetTheme.PanelLabel, "Engine settings");
            ImGui.Separator();

            // Local mutable copies so ImGui can write into them by ref.
            // We assemble a `with` patch at the end so the engine sees one
            // coherent update per frame instead of partial mutations.
            var radarRange = current.RadarRangeMeters;
            var dangerDist = current.DangerDistanceMeters;
            var closeDist = current.CloseDistanceMeters;
            var sideLat = current.SideBySideLateralMeters;
            var spotterFrames = current.SpotterHysteresisFrames;
            var gapSeconds = current.RelativePanelMaxGapSeconds;
            var carsPerSide = current.RelativePanelMaxCarsPerSide;

            var changed = false;

            ImGui.PushItemWidth(130f);

            if (ImGui.SliderFloat("Radar range (m)", ref radarRange, 20f, 200f, "%.0f")) changed = true;
            if (ImGui.SliderFloat("Danger dist (m)", ref dangerDist, 2f, 30f, "%.1f")) changed = true;
            if (ImGui.SliderFloat("Close dist (m)", ref closeDist, 5f, 50f, "%.1f")) changed = true;
            if (ImGui.SliderFloat("Side-by-side lat (m)", ref sideLat, 1f, 6f, "%.1f")) changed = true;
            if (ImGui.SliderInt("Spotter hysteresis frames", ref spotterFrames, 1, 10)) changed = true;

            ImGui.Separator();
            ImGui.TextColored(WidgetTheme.PanelLabel, "Relative panel");

            if (ImGui.SliderFloat("Max gap (s)", ref gapSeconds, 5f, 60f, "%.0f")) changed = true;
            if (ImGui.SliderInt("Cars per side", ref carsPerSide, 1, 6)) changed = true;

            ImGui.PopItemWidth();

            ImGui.Separator();
            if (ImGui.Button("Reset to defaults"))
            {
                onChange(RadarSettings.Default);
                return;
            }

            if (changed)
            {
                // Clamp the two danger/close thresholds so danger <= close —
                // otherwise the threat ladder makes no sense and the cone
                // visualization gets the wrong color band.
                if (dangerDist > closeDist) dangerDist = closeDist;

                onChange(current with
                {
                    RadarRangeMeters = radarRange,
                    DangerDistanceMeters = dangerDist,
                    CloseDistanceMeters = closeDist,
                    SideBySideLateralMeters = sideLat,
                    SpotterHysteresisFrames = spotterFrames,
                    RelativePanelMaxGapSeconds = gapSeconds,
                    RelativePanelMaxCarsPerSide = carsPerSide,
                });
            }
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Settings, layouts, editMode);
        }
    }
}
