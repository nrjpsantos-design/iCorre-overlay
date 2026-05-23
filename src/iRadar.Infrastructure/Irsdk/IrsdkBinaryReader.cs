using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace iRadar.Infrastructure.Irsdk;

// Pure-function decoders for the IRSDK binary layout. Extracted from
// IrsdkClient so the byte-level parsing can be exercised against synthetic
// fixtures without needing a memory-mapped file. Everything here is
// allocation-free except for the resulting String objects.
internal static class IrsdkBinaryReader
{
    public const int VarHeaderNameOffset = 16;
    public const int VarHeaderDescOffset = VarHeaderNameOffset + IrsdkProtocol.MaxString;     // 48
    public const int VarHeaderUnitOffset = VarHeaderDescOffset + IrsdkProtocol.MaxDesc;       // 112

    // Reads the 112-byte fixed-layout header. The span must be at least
    // IrsdkProtocol.HeaderSize long. Uses the [StructLayout(Sequential, Pack=4)]
    // shape of IrsdkHeader so MemoryMarshal does the field unpacking — that is
    // exactly the layout iRacing writes.
    public static IrsdkHeader ParseHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < IrsdkProtocol.HeaderSize)
        {
            throw new ArgumentException(
                $"Header span too small: need {IrsdkProtocol.HeaderSize}, got {bytes.Length}",
                nameof(bytes));
        }
        return MemoryMarshal.Read<IrsdkHeader>(bytes);
    }

    public static IrsdkVarDescriptor ParseVarDescriptor(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < IrsdkProtocol.VarHeaderSize)
        {
            throw new ArgumentException(
                $"Var header span too small: need {IrsdkProtocol.VarHeaderSize}, got {bytes.Length}",
                nameof(bytes));
        }

        var type = (IrsdkVarType)BinaryPrimitives.ReadInt32LittleEndian(bytes[..4]);
        var offset = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..8]);
        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes[8..12]);
        // bytes[12..16] = countAsTime (1 byte) + 3 pad bytes; we don't read them.

        var name = ReadFixedString(bytes.Slice(VarHeaderNameOffset, IrsdkProtocol.MaxString));
        var desc = ReadFixedString(bytes.Slice(VarHeaderDescOffset, IrsdkProtocol.MaxDesc));
        var unit = ReadFixedString(bytes.Slice(VarHeaderUnitOffset, IrsdkProtocol.MaxString));

        return new IrsdkVarDescriptor(name, type, offset, count, unit, desc);
    }

    // Picks the buffer with the highest TickCount. iRacing rotates between up
    // to MaxBuffers buffers so a slow reader can grab a coherent snapshot
    // while the producer writes the next one.
    public static (int Offset, int Tick) FindActiveBuffer(in IrsdkHeader header)
    {
        ReadOnlySpan<IrsdkVarBuf> bufs =
        [
            header.Buf0, header.Buf1, header.Buf2, header.Buf3,
        ];

        var tick = -1;
        var offset = 0;
        var count = Math.Clamp(header.NumBuf, 0, IrsdkProtocol.MaxBuffers);
        for (var i = 0; i < count; i++)
        {
            if (bufs[i].TickCount > tick)
            {
                tick = bufs[i].TickCount;
                offset = bufs[i].BufOffset;
            }
        }
        return (offset, tick);
    }

    public static string ReadFixedString(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        if (end < 0) end = bytes.Length;
        return Encoding.ASCII.GetString(bytes[..end]);
    }
}
