namespace iRadar.Core.Telemetry;

// Bitfield mirroring iRacing's irsdk_Flags. The IRSDK exposes SessionFlags
// as a single uint with multiple bits set simultaneously — e.g. a full-
// course caution is usually Caution | CautionWaving | Yellow | YellowWaving.
// Renderers should classify a single dominant condition via the
// SessionFlagClassifier helper rather than enumerating every bit.
[Flags]
public enum SessionFlag : uint
{
    None             = 0,
    Checkered        = 0x00000001,
    White            = 0x00000002,
    Green            = 0x00000004,
    Yellow           = 0x00000008,
    Red              = 0x00000010,
    Blue             = 0x00000020,
    Debris           = 0x00000040,
    Crossed          = 0x00000080,
    YellowWaving     = 0x00000100,
    OneLapToGreen    = 0x00000200,
    GreenHeld        = 0x00000400,
    TenToGo          = 0x00000800,
    FiveToGo         = 0x00001000,
    RandomWaving     = 0x00002000,
    Caution          = 0x00004000,
    CautionWaving    = 0x00008000,
    Black            = 0x00010000,
    Disqualify       = 0x00020000,
    Servicible       = 0x00040000,
    Furled           = 0x00080000,
    Repair           = 0x00100000,
    StartHidden      = 0x10000000,
    StartReady       = 0x20000000,
    StartSet         = 0x40000000,
    StartGo          = 0x80000000,
}
