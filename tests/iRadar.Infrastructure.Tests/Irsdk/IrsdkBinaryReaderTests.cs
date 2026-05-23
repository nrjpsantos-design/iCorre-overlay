using System.Buffers.Binary;
using System.Text;
using iRadar.Infrastructure.Irsdk;
using Xunit;

namespace iRadar.Infrastructure.Tests.Irsdk;

public class IrsdkBinaryReaderTests
{
    [Fact]
    public void ParseHeader_DecodesAllScalarFields()
    {
        var bytes = new byte[IrsdkProtocol.HeaderSize];
        WriteHeader(
            bytes,
            version: 2,
            status: 1,
            tickRate: 60,
            sessionInfoUpdate: 7,
            sessionInfoLen: 1024,
            sessionInfoOffset: 8192,
            numVars: 200,
            varHeaderOffset: 144,
            numBuf: 3,
            bufLen: 4096);

        var header = IrsdkBinaryReader.ParseHeader(bytes);

        Assert.Equal(2, header.Version);
        Assert.Equal(1, header.Status);
        Assert.Equal(60, header.TickRate);
        Assert.Equal(7, header.SessionInfoUpdate);
        Assert.Equal(1024, header.SessionInfoLen);
        Assert.Equal(8192, header.SessionInfoOffset);
        Assert.Equal(200, header.NumVars);
        Assert.Equal(144, header.VarHeaderOffset);
        Assert.Equal(3, header.NumBuf);
        Assert.Equal(4096, header.BufLen);
    }

    [Fact]
    public void ParseHeader_DecodesAllFourBuffers()
    {
        var bytes = new byte[IrsdkProtocol.HeaderSize];
        WriteHeader(bytes, numBuf: 4, bufLen: 4096);
        WriteBuf(bytes, idx: 0, tickCount: 100, bufOffset: 1024);
        WriteBuf(bytes, idx: 1, tickCount: 101, bufOffset: 5120);
        WriteBuf(bytes, idx: 2, tickCount: 99,  bufOffset: 9216);
        WriteBuf(bytes, idx: 3, tickCount: 102, bufOffset: 13312);

        var header = IrsdkBinaryReader.ParseHeader(bytes);

        Assert.Equal(100, header.Buf0.TickCount);
        Assert.Equal(101, header.Buf1.TickCount);
        Assert.Equal(99,  header.Buf2.TickCount);
        Assert.Equal(102, header.Buf3.TickCount);
        Assert.Equal(13312, header.Buf3.BufOffset);
    }

    [Fact]
    public void ParseHeader_RejectsTruncatedSpan()
    {
        var tooSmall = new byte[IrsdkProtocol.HeaderSize - 1];
        Assert.Throws<ArgumentException>(() => IrsdkBinaryReader.ParseHeader(tooSmall));
    }

    [Fact]
    public void FindActiveBuffer_PicksHighestTickCount()
    {
        var bytes = new byte[IrsdkProtocol.HeaderSize];
        WriteHeader(bytes, numBuf: 4);
        WriteBuf(bytes, 0, tickCount: 100, bufOffset: 1000);
        WriteBuf(bytes, 1, tickCount: 105, bufOffset: 2000);  // winner
        WriteBuf(bytes, 2, tickCount: 103, bufOffset: 3000);
        WriteBuf(bytes, 3, tickCount: 99,  bufOffset: 4000);

        var header = IrsdkBinaryReader.ParseHeader(bytes);
        var (offset, tick) = IrsdkBinaryReader.FindActiveBuffer(header);

        Assert.Equal(2000, offset);
        Assert.Equal(105, tick);
    }

    [Fact]
    public void FindActiveBuffer_ClampsToDeclaredNumBuf()
    {
        // Buf3 has the highest tick but NumBuf says only 3 are valid.
        // The reader must ignore Buf3.
        var bytes = new byte[IrsdkProtocol.HeaderSize];
        WriteHeader(bytes, numBuf: 3);
        WriteBuf(bytes, 0, tickCount: 100, bufOffset: 1000);
        WriteBuf(bytes, 1, tickCount: 105, bufOffset: 2000);  // winner among first 3
        WriteBuf(bytes, 2, tickCount: 103, bufOffset: 3000);
        WriteBuf(bytes, 3, tickCount: 999, bufOffset: 9999);  // out of range, must be ignored

        var header = IrsdkBinaryReader.ParseHeader(bytes);
        var (offset, tick) = IrsdkBinaryReader.FindActiveBuffer(header);

        Assert.Equal(2000, offset);
        Assert.Equal(105, tick);
    }

    [Fact]
    public void FindActiveBuffer_OnEmptyHeader_ReturnsNegativeTick()
    {
        var bytes = new byte[IrsdkProtocol.HeaderSize];
        WriteHeader(bytes, numBuf: 0);
        var header = IrsdkBinaryReader.ParseHeader(bytes);

        var (_, tick) = IrsdkBinaryReader.FindActiveBuffer(header);
        Assert.Equal(-1, tick);
    }

    [Fact]
    public void ParseVarDescriptor_DecodesTypeOffsetCount()
    {
        var bytes = new byte[IrsdkProtocol.VarHeaderSize];
        WriteVarHeader(bytes,
            type: IrsdkVarType.Float,
            offset: 256,
            count: 64,
            name: "CarIdxLapDistPct",
            desc: "Percent distance around lap by car index",
            unit: "%");

        var desc = IrsdkBinaryReader.ParseVarDescriptor(bytes);

        Assert.Equal(IrsdkVarType.Float, desc.Type);
        Assert.Equal(256, desc.Offset);
        Assert.Equal(64, desc.Count);
        Assert.Equal("CarIdxLapDistPct", desc.Name);
        Assert.Equal("Percent distance around lap by car index", desc.Description);
        Assert.Equal("%", desc.Unit);
    }

    [Fact]
    public void ParseVarDescriptor_TruncatesAtNullTerminator()
    {
        var bytes = new byte[IrsdkProtocol.VarHeaderSize];
        WriteVarHeader(bytes,
            type: IrsdkVarType.Int,
            offset: 0,
            count: 1,
            name: "Speed\0XXXXX",  // null in the middle, garbage after
            desc: "x",
            unit: "m/s");

        var desc = IrsdkBinaryReader.ParseVarDescriptor(bytes);

        Assert.Equal("Speed", desc.Name);  // not "Speed\0XXXXX"
    }

    [Fact]
    public void ParseVarDescriptor_RejectsTruncatedSpan()
    {
        var tooSmall = new byte[IrsdkProtocol.VarHeaderSize - 1];
        Assert.Throws<ArgumentException>(() => IrsdkBinaryReader.ParseVarDescriptor(tooSmall));
    }

    [Theory]
    [InlineData("Hello", "Hello")]
    [InlineData("", "")]
    [InlineData("ABC\0XYZ", "ABC")]
    public void ReadFixedString_TrimsAtNullByte(string input, string expected)
    {
        var bytes = new byte[32];
        var encoded = Encoding.ASCII.GetBytes(input);
        encoded.CopyTo(bytes, 0);
        // remainder is already zeroed by `new byte[]`
        Assert.Equal(expected, IrsdkBinaryReader.ReadFixedString(bytes));
    }

    // ----- helpers -----

    private static void WriteHeader(
        Span<byte> bytes,
        int version = 1,
        int status = 0,
        int tickRate = 60,
        int sessionInfoUpdate = 0,
        int sessionInfoLen = 0,
        int sessionInfoOffset = 0,
        int numVars = 0,
        int varHeaderOffset = 0,
        int numBuf = 0,
        int bufLen = 0)
    {
        WriteI32(bytes, 0, version);
        WriteI32(bytes, 4, status);
        WriteI32(bytes, 8, tickRate);
        WriteI32(bytes, 12, sessionInfoUpdate);
        WriteI32(bytes, 16, sessionInfoLen);
        WriteI32(bytes, 20, sessionInfoOffset);
        WriteI32(bytes, 24, numVars);
        WriteI32(bytes, 28, varHeaderOffset);
        WriteI32(bytes, 32, numBuf);
        WriteI32(bytes, 36, bufLen);
        // bytes 40..47 are pad
    }

    private static void WriteBuf(Span<byte> bytes, int idx, int tickCount, int bufOffset)
    {
        // VarBuf array starts at offset 48 in the header.
        var bufBase = 48 + (idx * IrsdkProtocol.VarBufSize);
        WriteI32(bytes, bufBase + 0, tickCount);
        WriteI32(bytes, bufBase + 4, bufOffset);
        // remaining 8 bytes are pad
    }

    private static void WriteVarHeader(
        Span<byte> bytes,
        IrsdkVarType type,
        int offset,
        int count,
        string name,
        string desc,
        string unit)
    {
        WriteI32(bytes, 0, (int)type);
        WriteI32(bytes, 4, offset);
        WriteI32(bytes, 8, count);
        // bytes 12..15: countAsTime + 3 pad
        WriteString(bytes, IrsdkBinaryReader.VarHeaderNameOffset, IrsdkProtocol.MaxString, name);
        WriteString(bytes, IrsdkBinaryReader.VarHeaderDescOffset, IrsdkProtocol.MaxDesc, desc);
        WriteString(bytes, IrsdkBinaryReader.VarHeaderUnitOffset, IrsdkProtocol.MaxString, unit);
    }

    private static void WriteI32(Span<byte> bytes, int offset, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..(offset + 4)], value);

    private static void WriteString(Span<byte> bytes, int offset, int maxLen, string value)
    {
        var encoded = Encoding.ASCII.GetBytes(value);
        var copyLen = Math.Min(encoded.Length, maxLen);
        encoded.AsSpan(0, copyLen).CopyTo(bytes[offset..(offset + maxLen)]);
        // remainder stays as whatever was there; tests pass zeroed buffers in.
    }
}
