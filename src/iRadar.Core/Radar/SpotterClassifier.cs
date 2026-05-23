using iRadar.Core.Telemetry;

namespace iRadar.Core.Radar;

// Debounces iRacing's CarLeftRight into a SpotterAlert that doesn't flicker.
//
// iRacing's value can oscillate on the boundary (e.g., a wheel tip drifting
// across the threshold). Showing every flip on screen is distracting. We
// require N consecutive snapshots of the same incoming state before
// publishing the transition; anything below that is held back.
//
// Stateful: hold an instance per ITelemetrySource — the radar engine does.
public sealed class SpotterClassifier
{
    private readonly int _hysteresisFrames;

    private SpotterAlert _published = SpotterAlert.Clear;
    private CarLeftRight _candidate = CarLeftRight.Clear;
    private int _candidateStreak;

    public SpotterClassifier(int hysteresisFrames)
    {
        if (hysteresisFrames < 1) throw new ArgumentOutOfRangeException(nameof(hysteresisFrames));
        _hysteresisFrames = hysteresisFrames;
    }

    public SpotterAlert Current => _published;

    public SpotterAlert Observe(CarLeftRight incoming)
    {
        var mapped = MapToAlert(incoming);

        if (mapped == _published)
        {
            _candidate = incoming;
            _candidateStreak = 0;
            return _published;
        }

        if (incoming == _candidate)
        {
            _candidateStreak++;
        }
        else
        {
            _candidate = incoming;
            _candidateStreak = 1;
        }

        if (_candidateStreak >= _hysteresisFrames)
        {
            _published = mapped;
            _candidateStreak = 0;
        }

        return _published;
    }

    public void Reset()
    {
        _published = SpotterAlert.Clear;
        _candidate = CarLeftRight.Clear;
        _candidateStreak = 0;
    }

    private static SpotterAlert MapToAlert(CarLeftRight raw) => raw switch
    {
        CarLeftRight.CarLeft => SpotterAlert.CarLeft,
        CarLeftRight.CarRight => SpotterAlert.CarRight,
        CarLeftRight.CarLeftRight => SpotterAlert.CarLeftRight,
        CarLeftRight.TwoCarsLeft => SpotterAlert.TwoCarsLeft,
        CarLeftRight.TwoCarsRight => SpotterAlert.TwoCarsRight,
        _ => SpotterAlert.Clear,  // Off and Clear both map to Clear
    };
}
