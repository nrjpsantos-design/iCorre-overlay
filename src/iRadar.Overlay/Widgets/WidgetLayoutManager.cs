using System.Numerics;
using iRadar.Core.Settings;

namespace iRadar.Overlay.Widgets;

// Central source of truth for where widgets are positioned and sized at
// runtime. Seeded from UserSettings on startup; mutated in-place by
// WidgetHelper when the user drags or resizes a widget in Edit Mode.
//
// Pattern:
//   1. App startup     → new WidgetLayoutManager(loadedSettings.Widgets)
//   2. Each render     → WidgetHelper.BeginWidget queries layout, applies
//                        SetNextWindowPos/Size on the first frame so the
//                        widget appears at the saved location
//   3. In Edit Mode    → WidgetHelper.EndWidget captures the current
//                        ImGui window pos/size back into this manager
//   4. Edit Mode exit  → composition root reads .Snapshot() and persists
//
// Thread-safe: not. Single-render-thread consumer.
public sealed class WidgetLayoutManager
{
    private readonly Dictionary<string, WidgetLayout> _layouts;
    private readonly HashSet<string> _pendingReposition;

    public WidgetLayoutManager(IReadOnlyDictionary<string, WidgetLayout> initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        _layouts = new Dictionary<string, WidgetLayout>(initial);
        // Force every widget to be repositioned on its first render so the
        // saved layout takes effect even if ImGui has stale internal state.
        _pendingReposition = new HashSet<string>(_layouts.Keys, StringComparer.Ordinal);
    }

    public WidgetLayout Get(string id)
    {
        return _layouts.TryGetValue(id, out var layout)
            ? layout
            : throw new KeyNotFoundException($"No layout registered for widget '{id}'.");
    }

    public bool TryGet(string id, out WidgetLayout layout)
        => _layouts.TryGetValue(id, out layout!);

    public bool NeedsRepositioning(string id) => _pendingReposition.Contains(id);

    public void MarkRepositioned(string id) => _pendingReposition.Remove(id);

    // Forces the next render of `id` to re-apply the saved position/size —
    // used after a settings reload or when a layout is set programmatically.
    public void ForceReposition(string id) => _pendingReposition.Add(id);

    public void Update(string id, Vector2 position, Vector2 size)
    {
        if (!_layouts.TryGetValue(id, out var current)) return;
        if (current.X == position.X && current.Y == position.Y
            && current.Width == size.X && current.Height == size.Y)
        {
            return;
        }
        _layouts[id] = current with
        {
            X = position.X,
            Y = position.Y,
            Width = size.X,
            Height = size.Y,
        };
    }

    // Snapshot for persistence. Returns a deep copy so callers can serialize
    // without worrying about concurrent mutations from the render loop.
    public Dictionary<string, WidgetLayout> Snapshot()
        => new(_layouts);
}
