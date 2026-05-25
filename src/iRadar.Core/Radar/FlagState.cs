using iRadar.Core.Telemetry;

namespace iRadar.Core.Radar;

// Single dominant flag for the overlay to render. iRacing publishes a
// bitfield of flags simultaneously — under caution you typically get
// Caution | CautionWaving | Yellow | YellowWaving all at once, plus the
// stewards' situational bits. The UI only needs to pick one to highlight.
//
// Priority order (highest first):
//   Black/Disqualify  → driver penalty, must respond now
//   Red               → session stopped
//   SafetyCar         → Caution flag set (pace car or full-course yellow)
//   Yellow            → local yellow on the player's sector
//   Blue              → being lapped by a faster car
//   White             → final lap
//   Checkered         → race finished
//   Green             → green flag (only meaningful briefly at session start
//                       or restart, so we show it for that purpose only)
//   None              → no banner needed
public enum FlagState
{
    None,
    Green,
    White,
    Checkered,
    Blue,
    Yellow,
    SafetyCar,
    Red,
    Black,
    Disqualify,
}

public static class SessionFlagClassifier
{
    public static FlagState Classify(SessionFlag flags)
    {
        if (flags == SessionFlag.None) return FlagState.None;

        if ((flags & SessionFlag.Disqualify) != 0) return FlagState.Disqualify;
        if ((flags & SessionFlag.Black) != 0) return FlagState.Black;
        if ((flags & SessionFlag.Red) != 0) return FlagState.Red;

        // Caution / CautionWaving means full-course yellow / safety car —
        // distinct from a local yellow on a single sector.
        if ((flags & (SessionFlag.Caution | SessionFlag.CautionWaving)) != 0)
            return FlagState.SafetyCar;

        if ((flags & (SessionFlag.Yellow | SessionFlag.YellowWaving)) != 0)
            return FlagState.Yellow;

        if ((flags & SessionFlag.Blue) != 0) return FlagState.Blue;
        if ((flags & SessionFlag.White) != 0) return FlagState.White;
        if ((flags & SessionFlag.Checkered) != 0) return FlagState.Checkered;

        // Green only carries information during a start or restart sequence.
        // GreenHeld marks the pace-lap-out-to-green transition.
        if ((flags & (SessionFlag.Green | SessionFlag.GreenHeld)) != 0)
            return FlagState.Green;

        return FlagState.None;
    }
}
