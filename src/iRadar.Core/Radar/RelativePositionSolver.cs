namespace iRadar.Core.Radar;

// Translates per-car telemetry into (x, y) positions relative to the player.
//
// Coordinate system (player at origin):
//   +X = ahead along the racing line
//   -X = behind
//   +Y = to the right
//   -Y = to the left
//
// Without per-car (x, y) telemetry (iRacing does not expose absolute world
// coordinates for other cars), we infer the longitudinal axis from
// CarIdxLapDistPct and assign the lateral axis from the iRacing-computed
// CarLeftRight hint. This is accurate for proximity scenarios (side-by-side
// on a straight) and degrades gracefully on tight corners — improving it
// requires a track centerline geometry, which is a later-phase enhancement.
public static class RelativePositionSolver
{
    // Builds a single (x, y) for `other` relative to `player`. The caller
    // decides which CarLeftRight context applies to which car — typically
    // only the closest car on either side gets the side-by-side treatment.
    public static (float X, float Y) Solve(
        float playerLapDistPct,
        float otherLapDistPct,
        float trackLengthMeters,
        LateralHint lateralHint,
        float sideBySideLateralMeters)
    {
        var x = SpatialMath.CircularDeltaMeters(playerLapDistPct, otherLapDistPct, trackLengthMeters);

        var y = lateralHint switch
        {
            LateralHint.Left => -sideBySideLateralMeters,
            LateralHint.Right => +sideBySideLateralMeters,
            _ => 0f,
        };

        return (x, y);
    }

    // Decide which side-by-side hint (if any) applies to a given car given
    // the (debounced) spotter alert and the longitudinal proximity.
    //
    // Uses the post-hysteresis SpotterAlert rather than the raw CarLeftRight
    // so the dot's lateral position doesn't flicker with the spotter signal.
    //
    // Rules:
    //   - If |x| > sideBySideMaxLongitudinal, no lateral hint (car is plain
    //     ahead/behind).
    //   - If the spotter indicates a single side and `x` is near zero, that
    //     side wins.
    //   - If the spotter indicates both sides, we cannot disambiguate which
    //     car is which — leave the hint at None and let the renderer fall
    //     back to centerline placement.
    public static LateralHint InferLateralHint(
        float x,
        SpotterAlert spotter,
        float sideBySideMaxLongitudinalMeters)
    {
        if (MathF.Abs(x) > sideBySideMaxLongitudinalMeters) return LateralHint.None;

        return spotter switch
        {
            SpotterAlert.CarLeft or SpotterAlert.TwoCarsLeft => LateralHint.Left,
            SpotterAlert.CarRight or SpotterAlert.TwoCarsRight => LateralHint.Right,
            _ => LateralHint.None,
        };
    }
}

public enum LateralHint
{
    None,
    Left,
    Right,
}
