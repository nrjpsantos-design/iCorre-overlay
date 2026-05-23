using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Telemetry;

public class TelemetrySnapshotTests
{
    [Fact]
    public void Empty_HasSentinelDefaults()
    {
        var s = TelemetrySnapshot.Empty;

        Assert.Equal(-1, s.PlayerCarIdx);
        Assert.Equal(-1, s.CamCarIdx);
        Assert.Equal(0, s.SessionTick);
        Assert.Equal(0f, s.PlayerSpeedMs);
        Assert.False(s.IsOnTrack);
        Assert.False(s.IsReplayPlaying);
        Assert.Equal(CarLeftRight.Off, s.Proximity);
        Assert.Empty(s.Cars);
        Assert.Same(SessionData.Unknown, s.Session);
    }

    [Fact]
    public void FocusedCar_PrefersCamCarIdx_FallsBackToPlayer()
    {
        var snap = TelemetrySnapshot.Empty with
        {
            PlayerCarIdx = 5,
            CamCarIdx = 12,
            Cars = new[]
            {
                CarState.Empty(5) with { DriverName = "Me" },
                CarState.Empty(12) with { DriverName = "Other" },
            },
        };

        Assert.Equal(12, snap.FocusedCar!.CarIdx);
        Assert.Equal("Other", snap.FocusedCar.DriverName);
    }

    [Fact]
    public void FocusedCar_FallsBackToPlayer_WhenCamIdxAbsent()
    {
        var snap = TelemetrySnapshot.Empty with
        {
            PlayerCarIdx = 5,
            CamCarIdx = -1,
            Cars = new[] { CarState.Empty(5) with { DriverName = "Me" } },
        };

        Assert.Equal(5, snap.FocusedCar!.CarIdx);
    }

    [Fact]
    public void Player_ReturnsCar_WhenPlayerCarIdxMatches()
    {
        var snap = TelemetrySnapshot.Empty with
        {
            PlayerCarIdx = 7,
            Cars = new[]
            {
                CarState.Empty(0),
                CarState.Empty(7) with { DriverName = "Me" },
                CarState.Empty(9),
            },
        };

        Assert.NotNull(snap.Player);
        Assert.Equal(7, snap.Player!.CarIdx);
        Assert.Equal("Me", snap.Player.DriverName);
    }

    [Fact]
    public void Player_IsNull_WhenNoMatchingCarExists()
    {
        var snap = TelemetrySnapshot.Empty with
        {
            PlayerCarIdx = 42,
            Cars = new[] { CarState.Empty(0), CarState.Empty(1) },
        };
        Assert.Null(snap.Player);
    }

    [Fact]
    public void Snapshot_RecordEquality_IsValueBased_ForScalarFields()
    {
        var a = TelemetrySnapshot.Empty with { SessionTick = 5, PlayerCarIdx = 1 };
        var b = TelemetrySnapshot.Empty with { SessionTick = 5, PlayerCarIdx = 1 };
        // Cars is a reference (Array.Empty<CarState>()) but equal-by-reference
        // for the Empty instance — both come from the same singleton.
        Assert.Equal(a, b);
    }
}
