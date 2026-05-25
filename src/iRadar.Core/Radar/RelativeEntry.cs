namespace iRadar.Core.Radar;

// Single row in the Relative panel: who is N seconds ahead or behind.
// Negative GapSeconds = the player is ahead of this car (this car is behind).
// Positive GapSeconds = this car is ahead of the player.
public sealed record RelativeEntry
{
    public required int CarIdx { get; init; }
    public required string DriverName { get; init; }
    public required string CarNumber { get; init; }
    public required int IRating { get; init; }
    public required int ClassId { get; init; }
    public required int Position { get; init; }
    public required float GapSeconds { get; init; }
    public required bool OnPitRoad { get; init; }
}
