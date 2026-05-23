namespace iRadar.Core.Telemetry;

// Session-level info that doesn't change frame-to-frame. Parsed from the
// iRacing YAML session string. Track length matters for converting LapDistPct
// to absolute distance; SessionType matters to know if we are in replay,
// practice, qualifying or race.
public sealed record SessionData
{
    public required string TrackName { get; init; }
    public required string TrackConfigName { get; init; }
    public required float TrackLengthMeters { get; init; }
    public required string SessionType { get; init; }
    public required bool IsReplay { get; init; }

    public static SessionData Unknown { get; } = new()
    {
        TrackName = "(unknown)",
        TrackConfigName = string.Empty,
        TrackLengthMeters = 0f,
        SessionType = "(unknown)",
        IsReplay = false,
    };
}
