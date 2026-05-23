using System.Numerics;
using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Fase 4 placeholder — proves the rendering pipeline reaches the screen.
// Replaced by the real radar / spotter / relative widgets in Fase 5.
internal static class HelloWidget
{
    private const string Title = "iRadar — Fase 4";
    private static readonly Vector2 DefaultPos = new(40f, 40f);
    private static readonly Vector2 DefaultSize = new(360f, 180f);
    private static readonly Vector4 WaitingColor = new(0.95f, 0.75f, 0.20f, 1.0f);
    private static readonly Vector4 ReadyColor = new(0.30f, 0.85f, 0.45f, 1.0f);
    private static readonly Vector4 LabelColor = new(0.70f, 0.70f, 0.70f, 1.0f);

    public static void Draw(TelemetrySnapshot? snapshot, RadarFrame? frame)
    {
        ImGui.SetNextWindowPos(DefaultPos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);

        var flags =
            ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoSavedSettings;

        if (!ImGui.Begin(Title, flags))
        {
            ImGui.End();
            return;
        }

        if (snapshot is null)
        {
            ImGui.TextColored(WaitingColor, "Waiting for iRacing telemetry...");
            ImGui.TextColored(LabelColor, "Open iRacing and load a session or replay.");
            ImGui.End();
            return;
        }

        ImGui.TextColored(ReadyColor, "Telemetry connected");
        ImGui.Separator();

        ImGui.TextColored(LabelColor, "Track");
        ImGui.SameLine(80f);
        ImGui.TextUnformatted(snapshot.Session.TrackName);

        ImGui.TextColored(LabelColor, "Tick");
        ImGui.SameLine(80f);
        ImGui.Text(snapshot.SessionTick.ToString());

        ImGui.TextColored(LabelColor, "Cars");
        ImGui.SameLine(80f);
        ImGui.Text(snapshot.Cars.Count.ToString());

        ImGui.TextColored(LabelColor, "State");
        ImGui.SameLine(80f);
        var state = snapshot.IsReplayPlaying
            ? "replay"
            : snapshot.IsOnTrack ? "on-track" : "pit/garage";
        ImGui.TextUnformatted(state);

        if (frame is { IsActive: true })
        {
            ImGui.Separator();
            ImGui.TextColored(LabelColor, "Radar dots");
            ImGui.SameLine(110f);
            ImGui.Text(frame.Dots.Count.ToString());

            ImGui.TextColored(LabelColor, "Spotter");
            ImGui.SameLine(110f);
            ImGui.TextUnformatted(frame.Spotter.ToString());
        }

        ImGui.End();
    }
}
