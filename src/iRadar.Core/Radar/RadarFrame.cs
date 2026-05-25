namespace iRadar.Core.Radar;

// Output of the radar engine for a single tick. Immutable, allocation-cheap
// for consumers. Renderers consume this to draw the overlay; nothing in this
// type depends on Win32 or any specific rendering backend.
public sealed record RadarFrame
{
    public required DateTimeOffset CapturedAt { get; init; }
    public required int SessionTick { get; init; }
    public required bool IsActive { get; init; }    // false when off-track or no session
    public required SpotterAlert Spotter { get; init; }
    public required IReadOnlyList<RadarDot> Dots { get; init; }
    public required IReadOnlyList<RelativeEntry> Ahead { get; init; }   // ordered closest first
    public required IReadOnlyList<RelativeEntry> Behind { get; init; }  // ordered closest first

    // Focused car's iRating and class — used by the Relative panel to render
    // iRating deltas and to dim entries in a different class (multiclass).
    // Defaults of 0 mean "unknown" and the widget falls back to neutral
    // rendering, which is the correct behavior for replay or non-licensed
    // sessions where iRacing doesn't publish iRating.
    public required int PlayerIRating { get; init; }
    public required int PlayerClassId { get; init; }

    public static RadarFrame Empty { get; } = new()
    {
        CapturedAt = DateTimeOffset.MinValue,
        SessionTick = 0,
        IsActive = false,
        Spotter = SpotterAlert.Clear,
        Dots = Array.Empty<RadarDot>(),
        Ahead = Array.Empty<RelativeEntry>(),
        Behind = Array.Empty<RelativeEntry>(),
        PlayerIRating = 0,
        PlayerClassId = 0,
    };
}
