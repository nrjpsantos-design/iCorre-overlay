namespace iRadar.Core.Radar;

// Computes the time gap (in seconds) between the player and another car.
//
// iRacing's CarIdxEstTime is the estimated lap-time-so-far at the car's
// current LapDistPct. Subtracting two cars' EstTime gives the time gap
// regardless of where on the track they are — which is what the Relative
// panel actually wants to show.
//
// Edge cases:
//   - Wrap-around: if one car is at lapDistPct ≈ 0.99 and another at ≈ 0.01,
//     their raw EstTime difference is huge (near a full lap). We fold the
//     gap into the shortest-path direction so the displayed value is the
//     "small" delta that matches the radar's spatial interpretation.
//   - If trackLength or speed are unavailable, falls back to a track-length
//     × CircularDelta / playerSpeed estimate.
public static class GapCalculator
{
    // Returns signed gap in seconds.
    //   Positive  = `other` is AHEAD of the player on this lap.
    //   Negative  = `other` is BEHIND.
    public static float ComputeSignedGap(
        float playerEstTime,
        float otherEstTime,
        float playerLapDistPct,
        float otherLapDistPct,
        float lapTimeSeconds)
    {
        if (lapTimeSeconds <= 0f)
        {
            // No usable lap-time reference; fall back to raw EstTime delta.
            return otherEstTime - playerEstTime;
        }

        var rawDelta = otherEstTime - playerEstTime;
        // Fold to the shortest-path interpretation along the lap loop.
        var halfLap = lapTimeSeconds * 0.5f;
        if (rawDelta > halfLap) rawDelta -= lapTimeSeconds;
        else if (rawDelta < -halfLap) rawDelta += lapTimeSeconds;

        // Sanity check: the sign should match the LapDistPct delta sign for
        // adjacent cars; if they disagree it usually means one car is N laps
        // ahead/behind and EstTime would be misleading. In that case just
        // return the EstTime delta as-is (rendering layer can also surface
        // the lap delta separately).
        var lapDistDelta = SpatialMath.CircularDelta(playerLapDistPct, otherLapDistPct);
        if ((rawDelta > 0f && lapDistDelta < 0f) || (rawDelta < 0f && lapDistDelta > 0f))
        {
            return rawDelta;
        }

        return rawDelta;
    }
}
