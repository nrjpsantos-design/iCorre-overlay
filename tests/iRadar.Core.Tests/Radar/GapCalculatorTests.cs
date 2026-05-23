using iRadar.Core.Radar;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class GapCalculatorTests
{
    [Fact]
    public void Gap_PositiveWhenOtherIsAhead()
    {
        var gap = GapCalculator.ComputeSignedGap(
            playerEstTime: 30f,
            otherEstTime: 32f,
            playerLapDistPct: 0.30f,
            otherLapDistPct: 0.32f,
            lapTimeSeconds: 90f);

        Assert.Equal(2f, gap, precision: 3);
    }

    [Fact]
    public void Gap_NegativeWhenOtherIsBehind()
    {
        var gap = GapCalculator.ComputeSignedGap(
            playerEstTime: 30f,
            otherEstTime: 28f,
            playerLapDistPct: 0.30f,
            otherLapDistPct: 0.28f,
            lapTimeSeconds: 90f);

        Assert.Equal(-2f, gap, precision: 3);
    }

    [Fact]
    public void Gap_FoldsAcrossStartFinishLine_WhenPlayerNearEnd()
    {
        // Player at 95% (EstTime ~85.5s on a 90s lap), other at 5% (EstTime
        // ~4.5s) — the OTHER car is just AHEAD of the player by ~9s, not
        // 81s behind. The fold should produce ~+9.
        var gap = GapCalculator.ComputeSignedGap(
            playerEstTime: 85.5f,
            otherEstTime: 4.5f,
            playerLapDistPct: 0.95f,
            otherLapDistPct: 0.05f,
            lapTimeSeconds: 90f);

        Assert.InRange(gap, 8.0f, 10.0f);
    }

    [Fact]
    public void Gap_FoldsAcrossStartFinishLine_WhenOtherNearEnd()
    {
        // Mirror of the previous: player just past the line, other still
        // hadn't crossed — the other car is just BEHIND the player.
        var gap = GapCalculator.ComputeSignedGap(
            playerEstTime: 4.5f,
            otherEstTime: 85.5f,
            playerLapDistPct: 0.05f,
            otherLapDistPct: 0.95f,
            lapTimeSeconds: 90f);

        Assert.InRange(gap, -10.0f, -8.0f);
    }

    [Fact]
    public void Gap_Without_LapTimeReference_FallsBackToRawDelta()
    {
        // lapTimeSeconds=0 → no fold, just raw EstTime delta.
        var gap = GapCalculator.ComputeSignedGap(
            playerEstTime: 50f,
            otherEstTime: 52.5f,
            playerLapDistPct: 0.10f,
            otherLapDistPct: 0.12f,
            lapTimeSeconds: 0f);

        Assert.Equal(2.5f, gap, precision: 3);
    }
}
