using System.IO.MemoryMappedFiles;
using System.Text;

namespace iRadar.Infrastructure.Irsdk;

// Low-level IRSDK reader: opens the named memory-mapped file, parses the
// header + variable descriptors, picks the most recent buffer, and lets
// callers extract values by variable name.
//
// Lifecycle:
//   1. TryOpen() — opens the MMF if iRacing is running. Returns false otherwise.
//   2. Refresh() — re-reads the header and selects the active buffer.
//   3. ReadXxx() — extract typed values from the active buffer.
//   4. Dispose — releases the view.
//
// Thread-safety: not safe. Callers must use a single thread (the polling thread).
internal sealed class IrsdkClient : IDisposable
{
    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _view;
    private long _viewLength;

    private IrsdkHeader _header;
    private int _activeBufferOffset;
    private int _activeBufferTick;
    private int _sessionInfoUpdate = -1;
    private string _sessionInfoYaml = string.Empty;

    private readonly Dictionary<string, IrsdkVarDescriptor> _vars = new(StringComparer.Ordinal);
    private readonly byte[] _headerBuffer = new byte[IrsdkProtocol.HeaderSize];
    private readonly byte[] _varHeaderBuffer = new byte[IrsdkProtocol.VarHeaderSize];

    public bool IsOpen => _view is not null;
    public int TickRate => _header.TickRate;
    public bool IsConnected => (_header.Status & IrsdkProtocol.StatusConnected) != 0;
    public int ActiveBufferTick => _activeBufferTick;
    public string SessionInfoYaml => _sessionInfoYaml;
    public int SessionInfoUpdate => _sessionInfoUpdate;
    public IReadOnlyDictionary<string, IrsdkVarDescriptor> Variables => _vars;

    public bool TryOpen()
    {
        if (IsOpen) return true;

        try
        {
            _mmf = MemoryMappedFile.OpenExisting(IrsdkProtocol.MemoryMapName, MemoryMappedFileRights.Read);
            _view = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _viewLength = _view.Capacity;
            return true;
        }
        catch (FileNotFoundException)
        {
            Cleanup();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Cleanup();
            return false;
        }
    }

    // Re-reads the header and parses variable descriptors if the buffer set
    // changed. Returns true if a NEWER buffer is now active than last call.
    public bool Refresh()
    {
        if (_view is null) return false;

        _view.ReadArray(0, _headerBuffer, 0, _headerBuffer.Length);
        _header = IrsdkBinaryReader.ParseHeader(_headerBuffer);

        var (bestOffset, bestTick) = IrsdkBinaryReader.FindActiveBuffer(_header);
        var newer = bestTick > _activeBufferTick;
        _activeBufferTick = bestTick;
        _activeBufferOffset = bestOffset;

        if (_vars.Count == 0 && _header.NumVars > 0)
        {
            LoadVariableDescriptors();
        }

        if (_header.SessionInfoUpdate != _sessionInfoUpdate && _header.SessionInfoLen > 0)
        {
            LoadSessionInfo();
        }

        return newer;
    }

    public bool TryGetVar(string name, out IrsdkVarDescriptor descriptor)
        => _vars.TryGetValue(name, out descriptor!);

    public bool ReadInt(string name, out int value)
    {
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type is not (IrsdkVarType.Int or IrsdkVarType.Bitfield))
        {
            value = 0;
            return false;
        }
        value = _view.ReadInt32(_activeBufferOffset + v.Offset);
        return true;
    }

    public bool ReadFloat(string name, out float value)
    {
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type != IrsdkVarType.Float)
        {
            value = 0f;
            return false;
        }
        value = _view.ReadSingle(_activeBufferOffset + v.Offset);
        return true;
    }

    public bool ReadDouble(string name, out double value)
    {
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type != IrsdkVarType.Double)
        {
            value = 0d;
            return false;
        }
        value = _view.ReadDouble(_activeBufferOffset + v.Offset);
        return true;
    }

    public bool ReadBool(string name, out bool value)
    {
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type != IrsdkVarType.Bool)
        {
            value = false;
            return false;
        }
        value = _view.ReadByte(_activeBufferOffset + v.Offset) != 0;
        return true;
    }

    public bool ReadFloatArray(string name, Span<float> destination, out int written)
    {
        written = 0;
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type != IrsdkVarType.Float)
        {
            return false;
        }

        var count = Math.Min(v.Count, destination.Length);
        var baseOffset = _activeBufferOffset + v.Offset;
        for (var i = 0; i < count; i++)
        {
            destination[i] = _view.ReadSingle(baseOffset + (i * sizeof(float)));
        }
        written = count;
        return true;
    }

    public bool ReadIntArray(string name, Span<int> destination, out int written)
    {
        written = 0;
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type is not (IrsdkVarType.Int or IrsdkVarType.Bitfield))
        {
            return false;
        }

        var count = Math.Min(v.Count, destination.Length);
        var baseOffset = _activeBufferOffset + v.Offset;
        for (var i = 0; i < count; i++)
        {
            destination[i] = _view.ReadInt32(baseOffset + (i * sizeof(int)));
        }
        written = count;
        return true;
    }

    public bool ReadBoolArray(string name, Span<bool> destination, out int written)
    {
        written = 0;
        if (_view is null || !_vars.TryGetValue(name, out var v) || v.Type != IrsdkVarType.Bool)
        {
            return false;
        }

        var count = Math.Min(v.Count, destination.Length);
        var baseOffset = _activeBufferOffset + v.Offset;
        for (var i = 0; i < count; i++)
        {
            destination[i] = _view.ReadByte(baseOffset + i) != 0;
        }
        written = count;
        return true;
    }

    private void LoadVariableDescriptors()
    {
        if (_view is null) return;

        _vars.Clear();

        for (var i = 0; i < _header.NumVars; i++)
        {
            var pos = _header.VarHeaderOffset + (i * IrsdkProtocol.VarHeaderSize);
            if (pos + IrsdkProtocol.VarHeaderSize > _viewLength) break;
            _view.ReadArray(pos, _varHeaderBuffer, 0, _varHeaderBuffer.Length);

            var descriptor = IrsdkBinaryReader.ParseVarDescriptor(_varHeaderBuffer);
            if (!string.IsNullOrEmpty(descriptor.Name))
            {
                _vars[descriptor.Name] = descriptor;
            }
        }
    }

    private void LoadSessionInfo()
    {
        if (_view is null) return;
        if (_header.SessionInfoLen <= 0) return;

        var len = _header.SessionInfoLen;
        var buf = new byte[len];
        _view.ReadArray(_header.SessionInfoOffset, buf, 0, len);

        // iRacing emits the YAML as Latin-1; null-terminated. Strip trailing NULs.
        var realLen = len;
        while (realLen > 0 && buf[realLen - 1] == 0) realLen--;

        _sessionInfoYaml = Encoding.Latin1.GetString(buf, 0, realLen);
        _sessionInfoUpdate = _header.SessionInfoUpdate;
    }

    public void Dispose() => Cleanup();

    private void Cleanup()
    {
        _view?.Dispose();
        _view = null;
        _mmf?.Dispose();
        _mmf = null;
        _vars.Clear();
        _sessionInfoUpdate = -1;
        _sessionInfoYaml = string.Empty;
        _activeBufferTick = 0;
        _activeBufferOffset = 0;
    }
}
