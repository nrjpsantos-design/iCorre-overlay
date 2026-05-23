using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Telemetry;

public class CarStateTests
{
    [Fact]
    public void Empty_ZeroesAllNumericFields()
    {
        var c = CarState.Empty(carIdx: 3);

        Assert.Equal(3, c.CarIdx);
        Assert.Equal(string.Empty, c.DriverName);
        Assert.Equal(string.Empty, c.CarNumber);
        Assert.Equal(0, c.IRating);
        Assert.Equal(0, c.ClassId);
        Assert.Equal(0f, c.LapDistPct);
        Assert.Equal(0, c.Lap);
        Assert.Equal(0, c.Position);
        Assert.Equal(0f, c.EstTime);
        Assert.False(c.OnPitRoad);
    }

    [Fact]
    public void Record_With_Mutates_OnlyTargetedFields()
    {
        var original = CarState.Empty(5);
        var moved = original with { LapDistPct = 0.42f, Lap = 3 };

        Assert.NotSame(original, moved);
        Assert.Equal(0.42f, moved.LapDistPct);
        Assert.Equal(3, moved.Lap);
        Assert.Equal(0f, original.LapDistPct);  // original unchanged
        Assert.Equal(5, moved.CarIdx);
    }
}
