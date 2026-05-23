namespace iRadar.Core.Radar;

// Stateless geometry / circular-coordinate helpers shared by the radar
// engine. Kept as static pure functions so they're trivially testable and
// allocation-free.
public static class SpatialMath
{
    // Signed shortest-path delta on a unit circle.
    //
    // Inputs are LapDistPct values in [0, 1) where 0 == start/finish.
    // Returns the displacement needed to go from `from` to `to` on the
    // shortest path, in (-0.5, +0.5].
    //
    //   CircularDelta(0.9, 0.1) ==  0.2   (forward across the line)
    //   CircularDelta(0.1, 0.9) == -0.2   (backward across the line)
    //   CircularDelta(0.1, 0.6) ==  0.5   (tie — picks +0.5 by convention)
    public static float CircularDelta(float from, float to)
    {
        var d = to - from;
        if (d > 0.5f) d -= 1f;
        else if (d < -0.5f) d += 1f;
        return d;
    }

    // Convenience: same delta but expressed in meters on a track of length L.
    public static float CircularDeltaMeters(float fromPct, float toPct, float trackLengthMeters)
        => CircularDelta(fromPct, toPct) * trackLengthMeters;

    // Clamp a value to [min, max] without LINQ.
    public static float Clamp(float v, float min, float max)
        => v < min ? min : v > max ? max : v;
}
