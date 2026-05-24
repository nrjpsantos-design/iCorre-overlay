using System.Numerics;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Visual indicator that Edit Mode is active. Always positioned at the top-
// center of the overlay window (not draggable by design — it's a hint
// telling the user the mode is on, plus the hotkey to leave it). Disappears
// the moment Edit Mode toggles off.
internal static class EditModeBanner
{
    private const string Title = "iRadar — EditModeBanner";
    private const string Line1 = "EDIT MODE — drag widgets to reposition";
    private const string Line2 = "Ctrl+Alt+E to lock";

    private static readonly Vector4 BannerBg = new(0.10f, 0.10f, 0.10f, 0.85f);
    private static readonly Vector4 BannerAccent = new(0.97f, 0.79f, 0.20f, 1.00f);

    private const ImGuiWindowFlags Flags =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoInputs
        | ImGuiWindowFlags.NoSavedSettings
        | ImGuiWindowFlags.AlwaysAutoResize;

    public static void Draw()
    {
        var viewport = ImGui.GetMainViewport();
        var vp = viewport.WorkPos;
        var vs = viewport.WorkSize;

        // Place near the top, horizontally centered. AlwaysAutoResize gives
        // us the actual width after the first frame; we re-center each call
        // using a simple offset that's a good visual approximation.
        const float estimatedWidth = 320f;
        var pos = new Vector2(vp.X + ((vs.X - estimatedWidth) / 2f), vp.Y + 16f);

        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, BannerBg);

        if (!ImGui.Begin(Title, Flags))
        {
            ImGui.End();
            ImGui.PopStyleColor();
            return;
        }

        ImGui.TextColored(BannerAccent, Line1);
        ImGui.TextColored(WidgetTheme.MutedText, Line2);

        ImGui.End();
        ImGui.PopStyleColor();
    }
}
