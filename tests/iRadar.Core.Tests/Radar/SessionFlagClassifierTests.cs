using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class SessionFlagClassifierTests
{
    [Fact]
    public void NoFlags_ReturnsNone()
    {
        Assert.Equal(FlagState.None, SessionFlagClassifier.Classify(SessionFlag.None));
    }

    [Fact]
    public void CautionDominatesYellow()
    {
        // Under full-course yellow iRacing sets both Caution and Yellow
        // bits; we must surface "SafetyCar" rather than the plain Yellow
        // local-sector flag.
        var flags = SessionFlag.Caution | SessionFlag.Yellow | SessionFlag.YellowWaving;
        Assert.Equal(FlagState.SafetyCar, SessionFlagClassifier.Classify(flags));
    }

    [Fact]
    public void LocalYellow()
    {
        Assert.Equal(FlagState.Yellow, SessionFlagClassifier.Classify(SessionFlag.Yellow));
        Assert.Equal(FlagState.Yellow, SessionFlagClassifier.Classify(SessionFlag.YellowWaving));
    }

    [Fact]
    public void RedOverridesYellow()
    {
        var flags = SessionFlag.Red | SessionFlag.Yellow;
        Assert.Equal(FlagState.Red, SessionFlagClassifier.Classify(flags));
    }

    [Fact]
    public void DisqualifyHasHighestPriority()
    {
        var flags = SessionFlag.Disqualify | SessionFlag.Black | SessionFlag.Red | SessionFlag.Caution;
        Assert.Equal(FlagState.Disqualify, SessionFlagClassifier.Classify(flags));
    }

    [Fact]
    public void BlueFlag()
    {
        Assert.Equal(FlagState.Blue, SessionFlagClassifier.Classify(SessionFlag.Blue));
    }

    [Fact]
    public void WhiteFlag()
    {
        Assert.Equal(FlagState.White, SessionFlagClassifier.Classify(SessionFlag.White));
    }

    [Fact]
    public void Checkered()
    {
        Assert.Equal(FlagState.Checkered, SessionFlagClassifier.Classify(SessionFlag.Checkered));
    }

    [Fact]
    public void GreenHeldClassifiesAsGreen()
    {
        Assert.Equal(FlagState.Green, SessionFlagClassifier.Classify(SessionFlag.GreenHeld));
    }
}
