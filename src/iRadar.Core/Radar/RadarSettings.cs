namespace iRadar.Core.Radar;

// Tunable knobs for the radar engine. All distances in meters; all times in
// seconds. Defaults are chosen for typical GT3/road-car racing — ovals or
// karts may want tighter values.
public sealed record RadarSettings
{
    // Cars farther than this are dropped from the radar (but still counted
    // for the Relative panel up to RelativePanelMaxGapSeconds).
    public required float RadarRangeMeters { get; init; }

    // Distance thresholds for ThreatLevel transitions, measured as straight-
    // line proximity (we don't yet know the true 2D position; this is the
    // along-track delta plus lateral hint).
    public required float DangerDistanceMeters { get; init; }
    public required float CloseDistanceMeters { get; init; }

    // Lateral offset applied to a dot when CarLeftRight tells us the other
    // car is alongside. Approximates a single lane-width — refined once the
    // track centerline geometry is wired up in a later phase.
    public required float SideBySideLateralMeters { get; init; }

    // How many consecutive snapshots a new CarLeftRight state must survive
    // before the SpotterClassifier publishes it. At 60Hz, 3 frames ≈ 50ms.
    public required int SpotterHysteresisFrames { get; init; }

    // Maximum |gap| (in seconds) shown in the Relative panel.
    public required float RelativePanelMaxGapSeconds { get; init; }

    // Cap on the number of cars listed in the Relative panel (per side).
    public required int RelativePanelMaxCarsPerSide { get; init; }

    public static RadarSettings Default { get; } = new()
    {
        RadarRangeMeters = 50f,
        DangerDistanceMeters = 8f,
        CloseDistanceMeters = 20f,
        SideBySideLateralMeters = 3.5f,
        SpotterHysteresisFrames = 3,
        RelativePanelMaxGapSeconds = 30f,
        RelativePanelMaxCarsPerSide = 3,
    };
}
