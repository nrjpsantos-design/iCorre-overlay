using System.Numerics;
using ImGuiNET;

namespace iRadar.Overlay.Widgets;

// Reduces the per-widget boilerplate: looks up the layout, applies position
// / size on first frame, picks the right ImGui flags depending on Edit Mode,
// and on End captures any user-driven changes back into the layout manager.
//
// Usage from a widget:
//
//   if (!WidgetHelper.Begin(id, title, layouts, editMode)) return;
//   try
//   {
//       // ... ImGui calls ...
//   }
//   finally
//   {
//       WidgetHelper.End(id, layouts, editMode);
//   }
internal static class WidgetHelper
{
    // Flags used when the user is NOT in Edit Mode. Everything locked down.
    private const ImGuiWindowFlags LockedFlags =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoInputs
        | ImGuiWindowFlags.NoSavedSettings;

    // Flags for Edit Mode: drop NoInputs / NoMove / NoResize so the user
    // can drag the widget by its body. Keep NoSavedSettings so ImGui doesn't
    // try to manage state via its own .ini (we own persistence).
    private const ImGuiWindowFlags EditFlags =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoSavedSettings;

    public static bool Begin(
        string id,
        string title,
        WidgetLayoutManager layouts,
        bool editMode,
        float backgroundAlpha)
    {
        if (!layouts.TryGet(id, out var layout) || !layout.Visible) return false;

        // On first render (or after a layout reload), force ImGui to the
        // saved position. Subsequent frames let ImGui keep its own state
        // (which honors user drag in Edit Mode).
        if (layouts.NeedsRepositioning(id))
        {
            ImGui.SetNextWindowPos(new Vector2(layout.X, layout.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(layout.Width, layout.Height), ImGuiCond.Always);
            layouts.MarkRepositioned(id);
        }

        ImGui.SetNextWindowBgAlpha(backgroundAlpha);

        return ImGui.Begin(title, editMode ? EditFlags : LockedFlags);
    }

    public static void End(string id, WidgetLayoutManager layouts, bool editMode)
    {
        if (editMode)
        {
            // Capture whatever the user dragged/resized to so we can persist
            // it when Edit Mode toggles off.
            layouts.Update(id, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        }
        ImGui.End();
    }
}
