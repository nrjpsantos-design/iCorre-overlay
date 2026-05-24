namespace iRadar.Core.Settings;

// Position, size and visibility of a single overlay widget. Persisted to
// disk via JsonUserSettingsStore so layout customisation survives restarts.
//
// Init-only properties (not `required`) so System.Text.Json can deserialize
// via a parameterless constructor and the `with` expression works without
// re-listing every field. Defaults exist for safety — UserSettings.Defaults
// always populates real values, but a partial/corrupt JSON wouldn't crash.
public sealed record WidgetLayout
{
    public string Id { get; init; } = string.Empty;
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; } = 200f;
    public float Height { get; init; } = 150f;
    public bool Visible { get; init; } = true;
}
