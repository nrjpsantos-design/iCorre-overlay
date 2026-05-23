namespace iRadar.Core.Radar;

// Discrete spotter state with hysteresis applied. Distinct from the raw
// iRacing CarLeftRight: SpotterAlert is what the overlay actually displays,
// after debouncing oscillation around the threshold.
public enum SpotterAlert
{
    Clear = 0,
    CarLeft = 1,
    CarRight = 2,
    CarLeftRight = 3,
    TwoCarsLeft = 4,
    TwoCarsRight = 5,
}
