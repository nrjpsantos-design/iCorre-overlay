namespace iRadar.Core.Telemetry;

// Mirrors iRacing's native CarLeftRight telemetry variable. iRacing already
// computes this on its side, so reading it is free spotter information.
// Values match the irsdk_CarLeftRight enum exactly.
public enum CarLeftRight
{
    Off = 0,
    Clear = 1,
    CarLeft = 2,
    CarRight = 3,
    CarLeftRight = 4,
    TwoCarsLeft = 5,
    TwoCarsRight = 6,
}
