namespace iRadar.Core.Telemetry;

// Per-car snapshot. Field set is the minimum needed for radar + spotter +
// relative-gap panel; extra iRacing variables can be added as later phases
// need them. Coordinates are in iRacing native units:
//   - LapDistPct: 0.0 to 1.0 around the track
//   - SpeedMs:    meters per second
//   - Position:   1-based race position (0 if unknown)
public sealed record CarState
{
    public required int CarIdx { get; init; }
    public required string DriverName { get; init; }
    public required string CarNumber { get; init; }
    public required int IRating { get; init; }
    public required int ClassId { get; init; }

    public required float LapDistPct { get; init; }
    public required int Lap { get; init; }
    public required int Position { get; init; }
    public required float EstTime { get; init; }
    public required bool OnPitRoad { get; init; }

    // True when iRacing reports the car as anywhere in the world (on track,
    // off track, in pit lane, approaching pit). False for retired / DNF /
    // garage-only slots that iRacing keeps in CarIdx arrays as ghosts at
    // LapDistPct=0. RadarEngine uses this to suppress phantom markers that
    // would otherwise hover over the start/finish line every lap.
    public required bool IsInWorld { get; init; }

    public static CarState Empty(int carIdx) => new()
    {
        CarIdx = carIdx,
        DriverName = string.Empty,
        CarNumber = string.Empty,
        IRating = 0,
        ClassId = 0,
        LapDistPct = 0f,
        Lap = 0,
        Position = 0,
        EstTime = 0f,
        OnPitRoad = false,
        IsInWorld = true,
    };
}
