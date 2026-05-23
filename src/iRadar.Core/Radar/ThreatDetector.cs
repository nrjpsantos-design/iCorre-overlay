namespace iRadar.Core.Radar;

// Classifies a single dot's proximity into SAFE / CLOSE / DANGER. The
// distance metric is straight-line Euclidean from (0,0) — i.e., from the
// player to the dot. A lateral side-by-side hint forces at least CLOSE
// because side-by-side IS the dangerous case even if longitudinal delta is
// small.
public static class ThreatDetector
{
    public static ThreatLevel Classify(
        float x,
        float y,
        bool sideBySide,
        RadarSettings settings)
    {
        var distance = MathF.Sqrt((x * x) + (y * y));

        // Side-by-side overrides anything that would have been "Safe".
        var baseLevel = distance switch
        {
            var d when d <= settings.DangerDistanceMeters => ThreatLevel.Danger,
            var d when d <= settings.CloseDistanceMeters => ThreatLevel.Close,
            _ => ThreatLevel.Safe,
        };

        return (sideBySide, baseLevel) switch
        {
            (true, ThreatLevel.Safe) => ThreatLevel.Close,
            _ => baseLevel,
        };
    }
}
