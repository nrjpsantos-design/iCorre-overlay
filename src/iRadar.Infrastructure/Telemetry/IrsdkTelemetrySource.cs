using iRadar.Core.Telemetry;
using iRadar.Infrastructure.Irsdk;

namespace iRadar.Infrastructure.Telemetry;

// Connects iRadar.Core's ITelemetrySource port to the live IRSDK shared-memory
// stream. Runs a single background thread that:
//   1. Tries to open the IRSDK memory-mapped file (Local\IRSDKMemMapFileName).
//   2. Waits on the IRSDKDataValidEvent for new-frame notifications (~60Hz).
//   3. On each tick: refreshes the active buffer, builds a TelemetrySnapshot,
//      and publishes it.
//
// Anti-cheat boundary: this class touches ONLY shared memory + a named event,
// both of which are the publicly documented IRSDK contract. No process is
// opened, no DLL injected, no Win32 hook installed.
public sealed class IrsdkTelemetrySource : ITelemetrySource
{
    private const int ReconnectIntervalMs = 1000;
    private const int DataWaitTimeoutMs = 250;

    private readonly object _lock = new();
    private readonly IrsdkClient _client = new();

    private CancellationTokenSource? _cts;
    private Thread? _thread;
    private EventWaitHandle? _dataEvent;

    private TelemetrySnapshot? _latest;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ParsedSessionInfo _session = ParsedSessionInfo.Empty;
    private int _lastSessionInfoUpdate = -1;

    public ConnectionState State
    {
        get { lock (_lock) return _state; }
    }

    public TelemetrySnapshot? Latest => Volatile.Read(ref _latest);

    public event EventHandler<TelemetrySnapshot>? SnapshotReceived;
    public event EventHandler<ConnectionState>? StateChanged;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_thread is not null) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _thread = new Thread(PollLoop)
            {
                Name = "iRadar-Telemetry",
                IsBackground = true,
            };
            _thread.Start();
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Thread? thread;
        lock (_lock)
        {
            thread = _thread;
            _cts?.Cancel();
            _thread = null;
        }
        thread?.Join(2000);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _dataEvent?.Dispose();
        _dataEvent = null;
        _client.Dispose();
        _cts?.Dispose();
        _cts = null;
    }

    private void PollLoop()
    {
        var ct = _cts!.Token;
        var carIdxBuffer = new float[64];
        var carIdxIntBuffer = new int[64];
        var carIdxBoolBuffer = new bool[64];

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsOpen)
                {
                    SetState(ConnectionState.Connecting);
                    if (!_client.TryOpen())
                    {
                        SetState(ConnectionState.Disconnected);
                        WaitOrCancel(ReconnectIntervalMs, ct);
                        continue;
                    }
                    TryOpenDataEvent();
                }

                // Wait for next-frame signal (efficient) or fall back to short sleep.
                if (_dataEvent is not null)
                {
                    _dataEvent.WaitOne(DataWaitTimeoutMs);
                }
                else
                {
                    WaitOrCancel(15, ct);
                }
                if (ct.IsCancellationRequested) break;

                var newer = _client.Refresh();
                if (!_client.IsConnected)
                {
                    SetState(ConnectionState.Connecting);
                    continue;
                }
                if (!newer) continue;

                if (_client.SessionInfoUpdate != _lastSessionInfoUpdate)
                {
                    _session = IrsdkSessionStringParser.Parse(_client.SessionInfoYaml);
                    _lastSessionInfoUpdate = _client.SessionInfoUpdate;
                }

                var snapshot = BuildSnapshot(carIdxBuffer, carIdxIntBuffer, carIdxBoolBuffer);
                if (snapshot is null) continue;

                SetState(snapshot.IsOnTrack ? ConnectionState.InCar : ConnectionState.InSession);
                Volatile.Write(ref _latest, snapshot);
                SnapshotReceived?.Invoke(this, snapshot);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Connection died mid-read; drop everything and try to reopen.
                _client.Dispose();
                _dataEvent?.Dispose();
                _dataEvent = null;
                _lastSessionInfoUpdate = -1;
                SetState(ConnectionState.Disconnected);
                WaitOrCancel(ReconnectIntervalMs, ct);
            }
        }

        SetState(ConnectionState.Disconnected);
    }

    private TelemetrySnapshot? BuildSnapshot(float[] floatBuf, int[] intBuf, bool[] boolBuf)
    {
        if (!_client.ReadInt("SessionTick", out var sessionTick)) return null;
        _client.ReadInt("PlayerCarIdx", out var playerCarIdx);
        if (!_client.ReadInt("CamCarIdx", out var camCarIdx))
        {
            // ReadInt sets value=0 on miss; rewrite to -1 so FocusedCar falls
            // back to PlayerCarIdx instead of pointing at a real car 0.
            camCarIdx = -1;
        }
        _client.ReadFloat("Speed", out var speed);
        _client.ReadInt("Lap", out var lap);
        _client.ReadFloat("LapDistPct", out var playerLapDist);
        _client.ReadFloat("Yaw", out var yaw);
        _client.ReadInt("CarLeftRight", out var proximityRaw);
        _client.ReadBool("IsOnTrack", out var isOnTrack);
        _client.ReadBool("IsReplayPlaying", out var isReplayPlaying);
        _client.ReadInt("SessionFlags", out var sessionFlagsRaw);

        _client.ReadFloatArray("CarIdxLapDistPct", floatBuf, out var lapDistCount);
        var estTime = new float[lapDistCount];
        Array.Clear(estTime);
        _client.ReadFloatArray("CarIdxEstTime", estTime, out _);

        _client.ReadIntArray("CarIdxLap", intBuf, out var lapCount);
        var positions = new int[Math.Max(lapDistCount, lapCount)];
        Array.Clear(positions);
        _client.ReadIntArray("CarIdxPosition", positions, out _);

        var carCount = Math.Max(lapDistCount, lapCount);
        var onPit = new bool[carCount];
        _client.ReadBoolArray("CarIdxOnPitRoad", onPit, out _);

        // CarIdxTrackSurface per iRacing's irsdk_TrkLoc enum:
        //   0 = NotInWorld (retired / DNF / never started — phantom slot)
        //   1 = OffTrack
        //   2 = InPitStall
        //   3 = ApproachingPits
        //   4 = OnTrack
        // We treat anything other than NotInWorld as "in world" — even pit
        // road cars count, because they can re-enter the race.
        var trackSurface = new int[carCount];
        _client.ReadIntArray("CarIdxTrackSurface", trackSurface, out _);

        var cars = new List<CarState>(carCount);
        for (var i = 0; i < carCount; i++)
        {
            // A CarIdx with Lap == 0 AND LapDistPct == 0 AND no driver entry
            // is an empty slot — iRacing pre-allocates 64 slots.
            var hasDriver = TryFindDriver(i, out var driver);
            var lapForCar = i < lapCount ? intBuf[i] : 0;
            var distForCar = i < lapDistCount ? floatBuf[i] : 0f;

            if (!hasDriver && lapForCar == 0 && distForCar == 0f) continue;

            var surface = i < trackSurface.Length ? trackSurface[i] : 0;
            var inWorld = surface != 0;   // 0 = NotInWorld

            cars.Add(new CarState
            {
                CarIdx = i,
                DriverName = driver?.UserName ?? string.Empty,
                CarNumber = driver?.CarNumber ?? string.Empty,
                IRating = driver?.IRating ?? 0,
                ClassId = driver?.CarClassId ?? 0,
                LapDistPct = distForCar,
                Lap = lapForCar,
                Position = i < positions.Length ? positions[i] : 0,
                EstTime = i < estTime.Length ? estTime[i] : 0f,
                OnPitRoad = i < onPit.Length && onPit[i],
                IsInWorld = inWorld,
            });
        }

        return new TelemetrySnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            SessionTick = sessionTick,
            Session = new SessionData
            {
                TrackName = _session.TrackName.Length > 0 ? _session.TrackName : SessionData.Unknown.TrackName,
                TrackConfigName = _session.TrackConfigName,
                TrackLengthMeters = _session.TrackLengthMeters,
                SessionType = _session.SessionType,
                IsReplay = _session.SessionType.Equals("Replay", StringComparison.OrdinalIgnoreCase),
            },
            PlayerCarIdx = playerCarIdx >= 0 ? playerCarIdx : _session.DriverCarIdx,
            CamCarIdx = camCarIdx,
            PlayerSpeedMs = speed,
            PlayerLap = lap,
            PlayerLapDistPct = playerLapDist,
            PlayerYawRad = yaw,
            Proximity = (CarLeftRight)proximityRaw,
            IsOnTrack = isOnTrack,
            IsReplayPlaying = isReplayPlaying,
            Flags = unchecked((SessionFlag)(uint)sessionFlagsRaw),
            Cars = cars,
        };
    }

    private bool TryFindDriver(int carIdx, out ParsedDriver? driver)
    {
        foreach (var d in _session.Drivers)
        {
            if (d.CarIdx == carIdx) { driver = d; return true; }
        }
        driver = null;
        return false;
    }

    private void TryOpenDataEvent()
    {
        try
        {
            _dataEvent = EventWaitHandle.OpenExisting(IrsdkProtocol.DataValidEventName);
        }
        catch
        {
            _dataEvent = null;
        }
    }

    private void SetState(ConnectionState next)
    {
        ConnectionState previous;
        lock (_lock)
        {
            previous = _state;
            if (previous == next) return;
            _state = next;
        }
        StateChanged?.Invoke(this, next);
    }

    private static void WaitOrCancel(int ms, CancellationToken ct)
    {
        try { Task.Delay(ms, ct).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
    }
}
