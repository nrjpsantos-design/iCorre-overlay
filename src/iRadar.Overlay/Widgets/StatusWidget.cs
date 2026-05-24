using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Small connection-status HUD shown in the top-left of the overlay. Confirms
// at a glance that telemetry is flowing and shows the current track, tick,
// car count and high-level state. Useful while setting up; can be hidden
// from settings in Fase 6.
internal static class StatusWidget
{
    private const string Title = "iRadar — Status";
    private static readonly Vector2 DefaultPos = new(20f, 20f);
    private static readonly Vector2 DefaultSize = new(340f, 150f);

    public static void Draw(TelemetrySnapshot? snapshot, RadarFrame? frame)
    {
        ImGui.SetNextWindowPos(DefaultPos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(WidgetTheme.DefaultBgAlpha);

        if (!ImGui.Begin(Title, WidgetTheme.WidgetFlags))
        {
            ImGui.End();
            return;
        }

        if (snapshot is null)
        {
            ImGui.TextColored(WidgetTheme.Waiting, "Waiting for iRacing telemetry...");
            ImGui.TextColored(WidgetTheme.MutedText, "Open iRacing and load a session or replay.");
            ImGui.End();
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

        ImGui.End();
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
