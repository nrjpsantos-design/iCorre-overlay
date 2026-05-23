namespace iRadar.Core.Radar;

// Threat level used to color a dot in the radar visualization. Mapped from
// proximity + lateral side. SAFE = green, CLOSE = yellow, DANGER = red.
public enum ThreatLevel
{
    Safe = 0,
    Close = 1,
    Danger = 2,
}
