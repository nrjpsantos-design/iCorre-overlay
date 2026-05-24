namespace iRadar.Core.Settings;

// Position, size and visibility of a single overlay widget. Persisted to
// disk via JsonUserSettingsStore so layout customisation survives restarts.
//
// Using float fields instead of System.Numerics.Vector2 keeps the JSON
// representation flat and friendly (a Vector2 serializes to {X,Y} with
// uppercase keys which collides with our camelCase policy).
public sealed record WidgetLayout
{
    public required string Id { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required bool Visible { get; init; }
}
