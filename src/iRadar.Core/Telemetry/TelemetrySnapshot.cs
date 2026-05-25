namespace iRadar.Core.Telemetry;

// Single immutable snapshot of telemetry at a moment in time. Produced by an
// ITelemetrySource at the iRacing tick rate (~60Hz). Consumers must treat this
// as read-only — the source may publish a new snapshot at any time.
public sealed record TelemetrySnapshot
{
    public required DateTimeOffset CapturedAt { get; init; }
    public required int SessionTick { get; init; }
    public required SessionData Session { get; init; }

    // Player-only telemetry. PlayerCarIdx is the index of the logged-in
    // driver's car (constant for the whole session). CamCarIdx is the car
    // currently followed by the camera — equals PlayerCarIdx in live mode
    // but differs in replay or when watching another car.
    public required int PlayerCarIdx { get; init; }
    public required int CamCarIdx { get; init; }
    public required float PlayerSpeedMs { get; init; }
    public required int PlayerLap { get; init; }
    public required float PlayerLapDistPct { get; init; }
    public required float PlayerYawRad { get; init; }

    // Proximity and IsOnTrack are only meaningful when the player is driving
    // a live session — iRacing returns Off / false in replay because there
    // is no live driver to compute them for.
    public required CarLeftRight Proximity { get; init; }
    public required bool IsOnTrack { get; init; }
    public required bool IsReplayPlaying { get; init; }

    // Session-level race-control flag bitfield (yellow, SC, red, blue, etc.).
    // Populated in live and replay alike — iRacing keeps SessionFlags in the
    // shared memory through both modes.
    public SessionFlag Flags { get; init; } = SessionFlag.None;

    // All cars in the session, including the player. Order is by CarIdx.
    public required IReadOnlyList<CarState> Cars { get; init; }

    public CarState? Player => Cars.FirstOrDefault(c => c.CarIdx == PlayerCarIdx);

    // Effective car for radar/spotter purposes: follows the camera in replay
    // and falls back to the logged-in driver in live mode.
    public CarState? FocusedCar =>
        Cars.FirstOrDefault(c => c.CarIdx == CamCarIdx)
        ?? Cars.FirstOrDefault(c => c.CarIdx == PlayerCarIdx);

    public static TelemetrySnapshot Empty { get; } = new()
    {
        CapturedAt = DateTimeOffset.MinValue,
        SessionTick = 0,
        Session = SessionData.Unknown,
        PlayerCarIdx = -1,
        CamCarIdx = -1,
        PlayerSpeedMs = 0f,
        PlayerLap = 0,
        PlayerLapDistPct = 0f,
        PlayerYawRad = 0f,
        Proximity = CarLeftRight.Off,
        IsOnTrack = false,
        IsReplayPlaying = false,
        Cars = Array.Empty<CarState>(),
    };
}
