namespace iRadar.Core.Radar;

// Tunable knobs for the radar engine. All distances in meters; all times in
// seconds. Defaults are chosen for typical GT3/road-car racing — ovals or
// karts may want tighter values.
public sealed record RadarSettings
{
    // Cars farther than this are dropped from the radar (but still counted
    // for the Relative panel up to RelativePanelMaxGapSeconds).
    public float RadarRangeMeters { get; init; } = 50f;

    // Distance thresholds for ThreatLevel transitions, measured as straight-
    // line proximity (we don't yet know the true 2D position; this is the
    // along-track delta plus lateral hint).
    public float DangerDistanceMeters { get; init; } = 8f;
    public float CloseDistanceMeters { get; init; } = 20f;

    // Lateral offset applied to a dot when CarLeftRight tells us the other
    // car is alongside. Approximates a single lane-width — refined once the
    // track centerline geometry is wired up in a later phase.
    public float SideBySideLateralMeters { get; init; } = 3.5f;

    // How many consecutive snapshots a new CarLeftRight state must survive
    // before the SpotterClassifier publishes it. At 60Hz, 3 frames ≈ 50ms.
    public int SpotterHysteresisFrames { get; init; } = 3;

    // Maximum |gap| (in seconds) shown in the Relative panel.
    public float RelativePanelMaxGapSeconds { get; init; } = 30f;

    // Cap on the number of cars listed in the Relative panel (per side).
    public int RelativePanelMaxCarsPerSide { get; init; } = 3;

    public static RadarSettings Default { get; } = new();
}
