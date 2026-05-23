using iRadar.Core.Radar;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class SpatialMathTests
{
    [Theory]
    [InlineData(0.5f, 0.5f, 0.0f)]
    [InlineData(0.0f, 0.25f, 0.25f)]
    [InlineData(0.25f, 0.0f, -0.25f)]
    public void CircularDelta_HandlesNonWrappingCases(float from, float to, float expected)
    {
        Assert.Equal(expected, SpatialMath.CircularDelta(from, to), precision: 5);
    }

    [Theory]
    // Crossing the line forward: from 0.9 to 0.1 should be +0.2 (forward), not -0.8.
    [InlineData(0.9f, 0.1f,  0.2f)]
    // Crossing backward: from 0.1 to 0.9 should be -0.2 (backward), not +0.8.
    [InlineData(0.1f, 0.9f, -0.2f)]
    [InlineData(0.95f, 0.05f, 0.10f)]
    [InlineData(0.05f, 0.95f, -0.10f)]
    public void CircularDelta_WrapsAroundStartFinishLine(float from, float to, float expected)
    {
        Assert.Equal(expected, SpatialMath.CircularDelta(from, to), precision: 5);
    }

    [Fact]
    public void CircularDelta_HalfLap_ConvergesToPositiveHalf()
    {
        // Exactly opposite on the lap — both directions equal. Convention
        // returns +0.5 (we picked > 0.5 → wrap, but < -0.5 → wrap; equality
        // at +0.5 stays positive).
        Assert.Equal(0.5f, SpatialMath.CircularDelta(0.0f, 0.5f), precision: 5);
    }

    [Fact]
    public void CircularDeltaMeters_ScalesByTrackLength()
    {
        // 7004m track (Spa). 0.5% of lap = 35.02m.
        var d = SpatialMath.CircularDeltaMeters(0.10f, 0.105f, 7004f);
        Assert.Equal(35.02f, d, precision: 2);
    }

    [Theory]
    [InlineData(5f,  0f,  10f,  5f)]
    [InlineData(-5f, 0f,  10f,  0f)]
    [InlineData(15f, 0f,  10f, 10f)]
    public void Clamp_LimitsValue(float v, float min, float max, float expected)
    {
        Assert.Equal(expected, SpatialMath.Clamp(v, min, max));
    }
}
