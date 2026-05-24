using iRadar.Core.Telemetry;

namespace iRadar.Core.Radar;

// Single entry point for turning a TelemetrySnapshot into a RadarFrame.
// Owns the small amount of state required for hysteresis on the spotter
// alert; the rest of the computation is stateless.
//
// Threading: not safe. Construct one engine per consumer and feed it
// snapshots sequentially.
public sealed class RadarEngine
{
    private SpotterClassifier _spotter;
    private RadarSettings _settings;

    public RadarEngine(RadarSettings? settings = null)
    {
        _settings = settings ?? RadarSettings.Default;
        _spotter = new SpotterClassifier(_settings.SpotterHysteresisFrames);
    }

    public RadarSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            // Hysteresis frames can change with settings, so we rebuild the
            // classifier rather than carrying state from the previous window.
            _spotter = new SpotterClassifier(_settings.SpotterHysteresisFrames);
        }
    }

    public void Reset() => _spotter.Reset();

    public RadarFrame Build(TelemetrySnapshot snapshot)
    {
        if (snapshot is null) return RadarFrame.Empty;

        // The "focused car" is whatever the camera is following — in live this
        // equals the player's own car, in replay it's whoever the user is
        // watching. Using FocusedCar (instead of Player) makes the radar work
        // in replay mode, which is the primary zero-risk testing surface.
        var focused = snapshot.FocusedCar;

        // The engine is active any time there is meaningful telemetry to
        // render — either the user is driving live (IsOnTrack) or iRacing
        // is playing back a recording (IsReplayPlaying). IsOnTrack alone
        // would exclude every replay, which is wrong.
        var isActive =
            (snapshot.IsOnTrack || snapshot.IsReplayPlaying)
            && snapshot.Session.TrackLengthMeters > 0f
            && focused is not null;

        var spotterAlert = _spotter.Observe(snapshot.Proximity);

        if (!isActive)
        {
            return RadarFrame.Empty with
            {
                CapturedAt = snapshot.CapturedAt,
                SessionTick = snapshot.SessionTick,
                Spotter = spotterAlert,
            };
        }

        var trackLen = snapshot.Session.TrackLengthMeters;

        var entries = new List<RadarComputedEntry>(snapshot.Cars.Count);
        var closestX = float.MaxValue;
        var closestIdx = -1;

        foreach (var car in snapshot.Cars)
        {
            if (car.CarIdx == focused!.CarIdx) continue;

            var x = SpatialMath.CircularDeltaMeters(
                focused.LapDistPct, car.LapDistPct, trackLen);

            var absX = MathF.Abs(x);
            if (absX <= _settings.CloseDistanceMeters && absX < closestX)
            {
                closestX = absX;
                closestIdx = car.CarIdx;
            }

            entries.Add(new RadarComputedEntry(car, x));
        }

        var dots = new List<RadarDot>(entries.Count);
        var ahead = new List<RelativeEntry>(entries.Count);
        var behind = new List<RelativeEntry>(entries.Count);

        foreach (var entry in entries)
        {
            var hint = LateralHint.None;
            if (entry.Car.CarIdx == closestIdx)
            {
                hint = RelativePositionSolver.InferLateralHint(
                    entry.X, spotterAlert, _settings.CloseDistanceMeters);
            }

            var (x, y) = RelativePositionSolver.Solve(
                focused!.LapDistPct,
                entry.Car.LapDistPct,
                trackLen,
                hint,
                _settings.SideBySideLateralMeters);

            var sideBySide = hint != LateralHint.None;
            var threat = ThreatDetector.Classify(x, y, sideBySide, _settings);

            var distance = MathF.Sqrt((x * x) + (y * y));
            var gap = GapCalculator.ComputeSignedGap(
                focused.EstTime,
                entry.Car.EstTime,
                focused.LapDistPct,
                entry.Car.LapDistPct,
                lapTimeSeconds: 0f);

            if (distance <= _settings.RadarRangeMeters)
            {
                dots.Add(new RadarDot
                {
                    CarIdx = entry.Car.CarIdx,
                    DriverName = entry.Car.DriverName,
                    CarNumber = entry.Car.CarNumber,
                    IRating = entry.Car.IRating,
                    X = x,
                    Y = y,
                    Threat = threat,
                    GapSeconds = gap,
                });
            }

            if (MathF.Abs(gap) <= _settings.RelativePanelMaxGapSeconds)
            {
                var rel = new RelativeEntry
                {
                    CarIdx = entry.Car.CarIdx,
                    DriverName = entry.Car.DriverName,
                    CarNumber = entry.Car.CarNumber,
                    IRating = entry.Car.IRating,
                    Position = entry.Car.Position,
                    GapSeconds = gap,
                    OnPitRoad = entry.Car.OnPitRoad,
                };
                if (gap > 0f) ahead.Add(rel); else behind.Add(rel);
            }
        }

        ahead.Sort((a, b) => a.GapSeconds.CompareTo(b.GapSeconds));
        behind.Sort((a, b) => b.GapSeconds.CompareTo(a.GapSeconds));   // closest (least negative) first
        if (ahead.Count > _settings.RelativePanelMaxCarsPerSide)
        {
            ahead.RemoveRange(_settings.RelativePanelMaxCarsPerSide,
                ahead.Count - _settings.RelativePanelMaxCarsPerSide);
        }
        if (behind.Count > _settings.RelativePanelMaxCarsPerSide)
        {
            behind.RemoveRange(_settings.RelativePanelMaxCarsPerSide,
                behind.Count - _settings.RelativePanelMaxCarsPerSide);
        }

        return new RadarFrame
        {
            CapturedAt = snapshot.CapturedAt,
            SessionTick = snapshot.SessionTick,
            IsActive = true,
            Spotter = spotterAlert,
            Dots = dots,
            Ahead = ahead,
            Behind = behind,
        };
    }

    private readonly record struct RadarComputedEntry(CarState Car, float X);
}
