namespace iRadar.Core.Telemetry;

// Single immutable snapshot of telemetry at a moment in time. Produced by an
// ITelemetrySource at the iRacing tick rate (~60Hz). Consumers must treat this
// as read-only — the source may publish a new snapshot at any time.
public sealed record TelemetrySnapshot
{
    public required DateTimeOffset CapturedAt { get; init; }
    public required int SessionTick { get; init; }
    public required SessionData Session { get; init; }

    // Player-only telemetry
    public required int PlayerCarIdx { get; init; }
    public required float PlayerSpeedMs { get; init; }
    public required int PlayerLap { get; init; }
    public required float PlayerLapDistPct { get; init; }
    public required float PlayerYawRad { get; init; }
    public required CarLeftRight Proximity { get; init; }
    public required bool IsOnTrack { get; init; }

    // All cars in the session, including the player. Order is by CarIdx.
    public required IReadOnlyList<CarState> Cars { get; init; }

    public CarState? Player => Cars.FirstOrDefault(c => c.CarIdx == PlayerCarIdx);

    public static TelemetrySnapshot Empty { get; } = new()
    {
        CapturedAt = DateTimeOffset.MinValue,
        SessionTick = 0,
        Session = SessionData.Unknown,
        PlayerCarIdx = -1,
        PlayerSpeedMs = 0f,
        PlayerLap = 0,
        PlayerLapDistPct = 0f,
        PlayerYawRad = 0f,
        Proximity = CarLeftRight.Off,
        IsOnTrack = false,
        Cars = Array.Empty<CarState>(),
    };
}
