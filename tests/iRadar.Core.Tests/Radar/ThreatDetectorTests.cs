using iRadar.Core.Radar;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class ThreatDetectorTests
{
    private static readonly RadarSettings Settings = RadarSettings.Default with
    {
        DangerDistanceMeters = 8f,
        CloseDistanceMeters = 20f,
    };

    [Fact]
    public void FarAway_IsSafe()
    {
        var lvl = ThreatDetector.Classify(x: 40f, y: 0f, sideBySide: false, Settings);
        Assert.Equal(ThreatLevel.Safe, lvl);
    }

    [Fact]
    public void WithinCloseRing_IsClose()
    {
        var lvl = ThreatDetector.Classify(x: 15f, y: 0f, sideBySide: false, Settings);
        Assert.Equal(ThreatLevel.Close, lvl);
    }

    [Fact]
    public void WithinDangerRing_IsDanger()
    {
        var lvl = ThreatDetector.Classify(x: 5f, y: 0f, sideBySide: false, Settings);
        Assert.Equal(ThreatLevel.Danger, lvl);
    }

    [Fact]
    public void EuclideanDistance_IsUsed()
    {
        // (3, 4) → distance 5, which is well inside DangerDistance (8).
        var lvl = ThreatDetector.Classify(x: 3f, y: 4f, sideBySide: false, Settings);
        Assert.Equal(ThreatLevel.Danger, lvl);
    }

    [Fact]
    public void SideBySide_EscalatesSafeToClose()
    {
        // A car 30m away would be Safe normally, but side-by-side promotes
        // it to Close — the spotter is telling us the OTHER car is alongside,
        // so distance alone is misleading.
        var lvl = ThreatDetector.Classify(x: 30f, y: 0f, sideBySide: true, Settings);
        Assert.Equal(ThreatLevel.Close, lvl);
    }

    [Fact]
    public void SideBySide_DoesNotDowngradeDanger()
    {
        var lvl = ThreatDetector.Classify(x: 1f, y: 0f, sideBySide: true, Settings);
        Assert.Equal(ThreatLevel.Danger, lvl);
    }
}
