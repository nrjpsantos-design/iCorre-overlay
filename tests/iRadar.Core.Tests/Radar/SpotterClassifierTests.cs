using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class SpotterClassifierTests
{
    [Fact]
    public void StartsAtClear()
    {
        var s = new SpotterClassifier(hysteresisFrames: 3);
        Assert.Equal(SpotterAlert.Clear, s.Current);
    }

    [Fact]
    public void TransitionRequires_N_ConsecutiveFrames()
    {
        var s = new SpotterClassifier(hysteresisFrames: 3);

        Assert.Equal(SpotterAlert.Clear, s.Observe(CarLeftRight.CarLeft));   // 1
        Assert.Equal(SpotterAlert.Clear, s.Observe(CarLeftRight.CarLeft));   // 2
        // 3rd consecutive observation publishes the new state.
        Assert.Equal(SpotterAlert.CarLeft, s.Observe(CarLeftRight.CarLeft));
    }

    [Fact]
    public void StreakResets_WhenStateChanges()
    {
        var s = new SpotterClassifier(hysteresisFrames: 3);

        s.Observe(CarLeftRight.CarLeft);    // candidate=Left streak=1
        s.Observe(CarLeftRight.CarRight);   // candidate=Right streak=1 (reset)
        s.Observe(CarLeftRight.CarLeft);    // candidate=Left streak=1 (reset)
        // Still under threshold — no transition.
        Assert.Equal(SpotterAlert.Clear, s.Current);

        s.Observe(CarLeftRight.CarLeft);    // streak=2
        s.Observe(CarLeftRight.CarLeft);    // streak=3 -> publish
        Assert.Equal(SpotterAlert.CarLeft, s.Current);
    }

    [Fact]
    public void HoldsCurrent_WhenIncomingEqualsCurrent()
    {
        var s = new SpotterClassifier(hysteresisFrames: 2);

        Assert.Equal(SpotterAlert.Clear, s.Observe(CarLeftRight.Clear));
        Assert.Equal(SpotterAlert.Clear, s.Observe(CarLeftRight.Off));
        Assert.Equal(SpotterAlert.Clear, s.Observe(CarLeftRight.Clear));
    }

    [Fact]
    public void Reset_GoesBackToClear()
    {
        var s = new SpotterClassifier(hysteresisFrames: 2);
        s.Observe(CarLeftRight.CarRight);
        s.Observe(CarLeftRight.CarRight);
        Assert.Equal(SpotterAlert.CarRight, s.Current);

        s.Reset();
        Assert.Equal(SpotterAlert.Clear, s.Current);
    }

    [Theory]
    [InlineData(CarLeftRight.Off, SpotterAlert.Clear)]
    [InlineData(CarLeftRight.Clear, SpotterAlert.Clear)]
    [InlineData(CarLeftRight.CarLeft, SpotterAlert.CarLeft)]
    [InlineData(CarLeftRight.CarRight, SpotterAlert.CarRight)]
    [InlineData(CarLeftRight.CarLeftRight, SpotterAlert.CarLeftRight)]
    [InlineData(CarLeftRight.TwoCarsLeft, SpotterAlert.TwoCarsLeft)]
    [InlineData(CarLeftRight.TwoCarsRight, SpotterAlert.TwoCarsRight)]
    public void AllCarLeftRight_ValuesMapToAlert(CarLeftRight raw, SpotterAlert expected)
    {
        // hysteresisFrames=1 means each observation publishes immediately.
        var s = new SpotterClassifier(hysteresisFrames: 1);
        Assert.Equal(expected, s.Observe(raw));
    }

    [Fact]
    public void ConstructorRejectsInvalidHysteresis()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpotterClassifier(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpotterClassifier(-1));
    }
}
