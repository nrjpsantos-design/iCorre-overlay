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
                PlayerIRating = focused?.IRating ?? 0,
                PlayerClassId = focused?.ClassId ?? 0,
            };
        }

        var trackLen = snapshot.Session.TrackLengthMeters;

        var entries = new List<RadarComputedEntry>(snapshot.Cars.Count);
        var closestX = float.MaxValue;
        var closestIdx = -1;

        foreach (var car in snapshot.Cars)
        {
            if (car.CarIdx == focused!.CarIdx) continue;
            // Skip phantom slots — cars that iRacing keeps in the CarIdx
            // arrays but aren't actually anywhere in the world (DNF, garage
            // only). Their LapDistPct is stuck at 0 and would otherwise
            // paint a permanent marker at the start/finish line.
            if (!car.IsInWorld) continue;

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

            var (x, realY) = RelativePositionSolver.Solve(
                focused!.LapDistPct,
                entry.Car.LapDistPct,
                trackLen,
                hint,
                _settings.SideBySideLateralMeters);

            // Threat is computed BEFORE any heuristic Y is layered on, so the
            // cosmetic visualization spread can't accidentally elevate danger.
            var sideBySide = hint != LateralHint.None;
            var threat = ThreatDetector.Classify(x, realY, sideBySide, _settings);

            // Heuristic lateral spread for visualization when the live spotter
            // isn't available (replay mode — iRacing only computes CarLeftRight
            // for the live driver). Without this, every dot stacks on the
            // radar's vertical centerline and you can't tell adjacent cars
            // apart. NOT real lateral position — purely cosmetic, gated on
            // the spotter being Off so live mode never sees it.
            var y = realY;
            if (hint == LateralHint.None && snapshot.Proximity == CarLeftRight.Off)
            {
                y = HeuristicLateralOffset(entry.Car.CarIdx);
            }

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
                    ClassId = entry.Car.ClassId,
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
            PlayerIRating = focused!.IRating,
            PlayerClassId = focused.ClassId,
        };
    }

    private readonly record struct RadarComputedEntry(CarState Car, float X);

    // Spacing between adjacent heuristic lanes, in meters. Independent of
    // SideBySideLateralMeters (which is for the live spotter hint and
    // represents a real lane width) — this is purely a visual nudge for the
    // replay heuristic and is kept tight on purpose so cars look close to
    // the player, amplifying the "risco lateral" sensation.
    private const float HeuristicLaneStepMeters = 0.5f;

    // Five stable lanes derived from CarIdx so adjacent cars in a cluster
    // get visibly distinct lateral positions on the radar even when iRacing
    // hasn't published a real spotter hint. Returns offsets in meters in
    // the range [-2 × step, +2 × step]:
    //   lane = -2 → -2.0 m
    //   lane = -1 → -1.0 m
    //   lane =  0 →  0.0 m
    //   lane = +1 → +1.0 m
    //   lane = +2 → +2.0 m
    private static float HeuristicLateralOffset(int carIdx)
    {
        // (((idx % 5) + 5) % 5) is the standard "positive modulo" trick — keeps
        // the lane index in 0..4 even when carIdx is somehow negative.
        var lane = (((carIdx % 5) + 5) % 5) - 2;
        return lane * HeuristicLaneStepMeters;
    }
}
