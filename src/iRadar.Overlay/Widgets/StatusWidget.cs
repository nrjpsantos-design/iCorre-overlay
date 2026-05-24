using iRadar.Core.Radar;
using iRadar.Core.Settings;
using iRadar.Core.Telemetry;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Small connection-status HUD shown in the top-left of the overlay. Confirms
// at a glance that telemetry is flowing and shows the current track, tick,
// car count and high-level state. Position/size/visibility come from
// WidgetLayoutManager so the user can reposition this in Edit Mode.
internal static class StatusWidget
{
    private const string Title = "iRadar — Status";

    public static void Draw(
        TelemetrySnapshot? snapshot,
        RadarFrame? frame,
        WidgetLayoutManager layouts,
        bool editMode)
    {
        if (!WidgetHelper.Begin(WidgetIds.Status, Title, layouts, editMode, WidgetTheme.DefaultBgAlpha))
        {
            return;
        }

        try
        {
            if (snapshot is null)
            {
                ImGui.TextColored(WidgetTheme.Waiting, "Waiting for iRacing telemetry...");
                ImGui.TextColored(WidgetTheme.MutedText, "Open iRacing and load a session or replay.");
                return;
            }

            Label("Track", snapshot.Session.TrackName);
            Label("Tick",  snapshot.SessionTick.ToString());
            Label("Cars",  snapshot.Cars.Count.ToString());
            Label("State", StateText(snapshot));

            if (frame is { IsActive: true })
            {
                ImGui.Separator();
                Label("Dots",    frame.Dots.Count.ToString());
                Label("Spotter", frame.Spotter.ToString());
            }
        }
        finally
        {
            WidgetHelper.End(WidgetIds.Status, layouts, editMode);
        }
    }

    private static void Label(string name, string value)
    {
        ImGui.TextColored(WidgetTheme.PanelLabel, name);
        ImGui.SameLine(90f);
        ImGui.TextUnformatted(value);
    }

    private static string StateText(TelemetrySnapshot s) =>
        s.IsReplayPlaying ? "replay" : s.IsOnTrack ? "on-track" : "pit/garage";
}
