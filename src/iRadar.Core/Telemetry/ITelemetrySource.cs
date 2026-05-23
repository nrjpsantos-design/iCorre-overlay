namespace iRadar.Core.Telemetry;

// Port for any telemetry producer — live IRSDK, recorded .ibt replay, or test
// fixtures. Keeping this in Core means the radar engine and tests never depend
// on Windows or any wrapper library.
public interface ITelemetrySource : IAsyncDisposable
{
    ConnectionState State { get; }

    // Latest snapshot, or null before the first one arrives. Lock-free read.
    TelemetrySnapshot? Latest { get; }

    event EventHandler<TelemetrySnapshot>? SnapshotReceived;
    event EventHandler<ConnectionState>? StateChanged;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
