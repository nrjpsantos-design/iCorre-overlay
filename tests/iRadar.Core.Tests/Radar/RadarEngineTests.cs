using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class RadarEngineTests
{
    private const float TrackLen = 5000f;

    private static TelemetrySnapshot BuildSnapshot(
        float playerLapDistPct,
        IEnumerable<CarState> others,
        CarLeftRight proximity = CarLeftRight.Clear,
        bool isOnTrack = true,
        bool isReplayPlaying = false,
        int playerCarIdx = 0,
        int? camCarIdx = null)
    {
        var cars = new List<CarState>
        {
            CarState.Empty(playerCarIdx) with
            {
                DriverName = "Player",
                CarNumber = "1",
                LapDistPct = playerLapDistPct,
                EstTime = playerLapDistPct * 90f,
                Lap = 5,
            },
        };
        cars.AddRange(others);

        return TelemetrySnapshot.Empty with
        {
            CapturedAt = DateTimeOffset.UtcNow,
            SessionTick = 1234,
            PlayerCarIdx = playerCarIdx,
            CamCarIdx = camCarIdx ?? playerCarIdx,
            PlayerLapDistPct = playerLapDistPct,
            PlayerYawRad = 0f,
            Proximity = proximity,
            IsOnTrack = isOnTrack,
            IsReplayPlaying = isReplayPlaying,
            Cars = cars,
            Session = new SessionData
            {
                TrackName = "Test Circuit",
                TrackConfigName = string.Empty,
                TrackLengthMeters = TrackLen,
                SessionType = "Practice",
                IsReplay = isReplayPlaying,
            },
        };
    }

    private static CarState MakeCar(int carIdx, float lapDistPct, string name = "Other", string number = "42")
        => CarState.Empty(carIdx) with
        {
            DriverName = name,
            CarNumber = number,
            LapDistPct = lapDistPct,
            EstTime = lapDistPct * 90f,
            Lap = 5,
            Position = carIdx + 1,
        };

    [Fact]
    public void NullSnapshot_ReturnsEmptyFrame()
    {
        var engine = new RadarEngine();
        Assert.Same(RadarFrame.Empty, engine.Build(null!));
    }

    [Fact]
    public void OffTrack_ReturnsInactiveFrame_ButProcessesSpotter()
    {
        var engine = new RadarEngine(RadarSettings.Default with { SpotterHysteresisFrames = 1 });
        var snap = BuildSnapshot(0.5f, new[] { MakeCar(1, 0.501f) },
            proximity: CarLeftRight.CarLeft, isOnTrack: false);

        var frame = engine.Build(snap);
        Assert.False(frame.IsActive);
        Assert.Empty(frame.Dots);
        Assert.Equal(SpotterAlert.CarLeft, frame.Spotter);
    }

    [Fact]
    public void PlayerCar_IsNeverADot()
    {
        var engine = new RadarEngine();
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.51f) });

        var frame = engine.Build(snap);
        Assert.True(frame.IsActive);
        Assert.DoesNotContain(frame.Dots, d => d.CarIdx == 0);
    }

    [Fact]
    public void CarAhead_AppearsAtPositiveX()
    {
        var engine = new RadarEngine();
        // 0.5% of 5000m = 25m ahead.
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.505f) });

        var frame = engine.Build(snap);
        var dot = Assert.Single(frame.Dots);
        Assert.Equal(25f, dot.X, precision: 1);
        Assert.Equal(0f, dot.Y);
    }

    [Fact]
    public void CarBehind_AppearsAtNegativeX()
    {
        var engine = new RadarEngine();
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.495f) });

        var frame = engine.Build(snap);
        var dot = Assert.Single(frame.Dots);
        Assert.Equal(-25f, dot.X, precision: 1);
    }

    [Fact]
    public void SideBySide_WithCarLeftHint_PlacesClosestCarAtNegativeY()
    {
        var engine = new RadarEngine(RadarSettings.Default with { SpotterHysteresisFrames = 1 });
        // Three cars within close range: 2m ahead, 5m ahead, 30m ahead.
        // The closest (2m) should get the Left hint.
        var cars = new[]
        {
            MakeCar(1, 0.50040f),   // ~2m ahead
            MakeCar(2, 0.50100f),   // ~5m ahead
            MakeCar(3, 0.50600f),   // ~30m ahead
        };
        var snap = BuildSnapshot(0.50f, cars, proximity: CarLeftRight.CarLeft);

        var frame = engine.Build(snap);
        var closest = frame.Dots.OrderBy(d => MathF.Abs(d.X)).First();
        Assert.Equal(1, closest.CarIdx);
        Assert.True(closest.Y < 0f, $"closest dot should be on the left (-Y), got {closest.Y}");

        // The other two should remain on the centerline.
        foreach (var d in frame.Dots.Where(d => d.CarIdx != 1))
        {
            Assert.Equal(0f, d.Y);
        }
    }

    [Fact]
    public void CarBeyondRadarRange_IsExcludedFromDots_ButMayAppearInRelativePanel()
    {
        var engine = new RadarEngine(RadarSettings.Default with
        {
            RadarRangeMeters = 50f,
            RelativePanelMaxGapSeconds = 30f,
            RelativePanelMaxCarsPerSide = 3,
        });
        // 5% ahead = 250m on a 5000m track ≈ 4.5s gap on a 90s lap.
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.55f) });

        var frame = engine.Build(snap);
        Assert.Empty(frame.Dots);            // too far for the radar
        Assert.Single(frame.Ahead);          // close enough for relative
        Assert.Empty(frame.Behind);
    }

    [Fact]
    public void RelativePanel_SortsClosestFirst_AndCapsPerSide()
    {
        var engine = new RadarEngine(RadarSettings.Default with
        {
            RadarRangeMeters = 1f,
            RelativePanelMaxGapSeconds = 60f,
            RelativePanelMaxCarsPerSide = 2,
        });

        var cars = new[]
        {
            MakeCar(1, 0.502f),    // ~+0.18s ahead
            MakeCar(2, 0.510f),    // ~+0.90s ahead
            MakeCar(3, 0.520f),    // ~+1.80s ahead — should be cut by cap
            MakeCar(4, 0.490f),    // ~-0.90s behind
            MakeCar(5, 0.480f),    // ~-1.80s behind
            MakeCar(6, 0.470f),    // ~-2.70s behind — should be cut
        };
        var snap = BuildSnapshot(0.50f, cars);

        var frame = engine.Build(snap);
        Assert.Equal(2, frame.Ahead.Count);
        Assert.Equal(2, frame.Behind.Count);
        Assert.Equal(1, frame.Ahead[0].CarIdx);    // closest ahead first
        Assert.Equal(2, frame.Ahead[1].CarIdx);
        Assert.Equal(4, frame.Behind[0].CarIdx);   // closest behind first
        Assert.Equal(5, frame.Behind[1].CarIdx);
    }

    [Fact]
    public void Spotter_HysteresisHoldsState_AcrossMultipleFrames()
    {
        var engine = new RadarEngine(RadarSettings.Default with { SpotterHysteresisFrames = 3 });
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.501f) },
            proximity: CarLeftRight.CarLeft);

        // First two frames: still Clear (under threshold).
        Assert.Equal(SpotterAlert.Clear, engine.Build(snap).Spotter);
        Assert.Equal(SpotterAlert.Clear, engine.Build(snap).Spotter);
        // Third consecutive frame: transition.
        Assert.Equal(SpotterAlert.CarLeft, engine.Build(snap).Spotter);
    }

    [Fact]
    public void ChangingSettings_ResetsSpotterState()
    {
        var engine = new RadarEngine(RadarSettings.Default with { SpotterHysteresisFrames = 1 });
        var snap = BuildSnapshot(0.50f, new[] { MakeCar(1, 0.501f) },
            proximity: CarLeftRight.CarRight);

        Assert.Equal(SpotterAlert.CarRight, engine.Build(snap).Spotter);

        engine.Settings = RadarSettings.Default with { SpotterHysteresisFrames = 5 };
        // Settings change resets the classifier; the very next snapshot must
        // start from Clear, not jump straight to CarRight again.
        Assert.Equal(SpotterAlert.Clear, engine.Build(snap).Spotter);
    }

    // ----- Replay mode regression coverage -----

    [Fact]
    public void ReplayMode_BuildsActiveFrame_EvenThoughIsOnTrackIsFalse()
    {
        // iRacing reports IsOnTrack=false in replay (there is no live driver).
        // The engine must still produce a usable RadarFrame because the radar
        // is one of the main zero-risk validation surfaces.
        var engine = new RadarEngine();
        var snap = BuildSnapshot(
            playerLapDistPct: 0.50f,
            others: new[] { MakeCar(1, 0.505f) },
            isOnTrack: false,
            isReplayPlaying: true);

        var frame = engine.Build(snap);

        Assert.True(frame.IsActive);
        var dot = Assert.Single(frame.Dots);
        Assert.Equal(25f, dot.X, precision: 1);   // 0.5% of 5000m ≈ 25m ahead
    }

    [Fact]
    public void ReplayMode_UsesCameraCar_NotPlayerCar()
    {
        // Player is on car 0 (at 0.10), camera is on car 7 (at 0.60).
        // Another car is at 0.605 — that's 25m ahead of CAR 7, not car 0.
        // The radar must orient around the camera car.
        var engine = new RadarEngine();
        var snap = BuildSnapshot(
            playerLapDistPct: 0.10f,
            others: new[]
            {
                MakeCar(7,  0.60f,  name: "Camera target"),
                MakeCar(12, 0.605f, name: "Ahead of camera"),
            },
            isOnTrack: false,
            isReplayPlaying: true,
            playerCarIdx: 0,
            camCarIdx: 7);

        var frame = engine.Build(snap);

        // The dots list excludes the focused car (car 7) and includes the
        // other car relative to car 7's position — 25m ahead.
        var dot = Assert.Single(frame.Dots);
        Assert.Equal(12, dot.CarIdx);
        Assert.Equal(25f, dot.X, precision: 1);
    }

}
