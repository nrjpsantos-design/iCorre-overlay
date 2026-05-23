namespace iRadar.Core.Radar;

// One car positioned on the radar relative to the player.
//
// Coordinate convention:
//   +X = ahead of the player (driving direction)
//   -X = behind
//   +Y = to the player's right
//   -Y = to the player's left
// Both in meters. The player is always at (0,0).
public sealed record RadarDot
{
    public required int CarIdx { get; init; }
    public required string DriverName { get; init; }
    public required string CarNumber { get; init; }
    public required int IRating { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required ThreatLevel Threat { get; init; }
    public required float GapSeconds { get; init; }
}
