using System.Runtime.InteropServices;

namespace iRadar.Infrastructure.Irsdk;

// Constants and binary layouts mirroring iRacing's irsdk_defines.h. Reference:
// https://github.com/kutu/pyirsdk/blob/master/irsdk_defines.h (mirrors the
// official header distributed with iRacing in Documents\iRacing\telemetry).
//
// We DO NOT call any forbidden API. We only:
//   - Open a publicly named, read-only memory-mapped file (MapViewOfFile via
//     System.IO.MemoryMappedFiles — same primitive Discord/OBS use)
//   - Wait on a publicly named event for new-data notifications
//   - Read bytes
//
// No process is opened, no DLL injected, no hook installed.
internal static class IrsdkProtocol
{
    public const string MemoryMapName = "Local\\IRSDKMemMapFileName";
    public const string DataValidEventName = "Local\\IRSDKDataValidEvent";
    public const string BroadcastMessageName = "IRSDK_BROADCASTMSG";

    public const int MaxBuffers = 4;
    public const int MaxString = 32;
    public const int MaxDesc = 64;

    // Status bitfield: bit 0 set means iRacing is running and broadcasting.
    public const int StatusConnected = 1;

    // Sizes of the binary structs — must match the irsdk header exactly.
    // Header: 12 ints (48 bytes) + 4 × VarBuf (16 bytes each) = 112 bytes.
    public const int HeaderSize = 112;
    public const int VarBufSize = 16;          // TickCount(4) + BufOffset(4) + Pad(8)
    public const int VarHeaderSize = 144;      // type(4)+offset(4)+count(4)+countAsTime(1)+pad(3)+name(32)+desc(64)+unit(32)
}

internal enum IrsdkVarType
{
    Char = 0,
    Bool = 1,
    Int = 2,
    Bitfield = 3,
    Float = 4,
    Double = 5,
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct IrsdkVarBuf
{
    public int TickCount;
    public int BufOffset;
    public int Pad0;
    public int Pad1;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct IrsdkHeader
{
    public int Version;
    public int Status;
    public int TickRate;

    public int SessionInfoUpdate;
    public int SessionInfoLen;
    public int SessionInfoOffset;

    public int NumVars;
    public int VarHeaderOffset;

    public int NumBuf;
    public int BufLen;

    public int Pad0;
    public int Pad1;

    public IrsdkVarBuf Buf0;
    public IrsdkVarBuf Buf1;
    public IrsdkVarBuf Buf2;
    public IrsdkVarBuf Buf3;
}

internal sealed record IrsdkVarDescriptor(
    string Name,
    IrsdkVarType Type,
    int Offset,
    int Count,
    string Unit,
    string Description);

internal static class IrsdkVarTypeExtensions
{
    public static int SizeBytes(this IrsdkVarType type) => type switch
    {
        IrsdkVarType.Char => 1,
        IrsdkVarType.Bool => 1,
        IrsdkVarType.Int => 4,
        IrsdkVarType.Bitfield => 4,
        IrsdkVarType.Float => 4,
        IrsdkVarType.Double => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown IRSDK var type"),
    };
}
