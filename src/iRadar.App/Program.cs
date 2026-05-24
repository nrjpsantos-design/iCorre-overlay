using iRadar.Core.Radar;
using iRadar.Infrastructure.Telemetry;
using iRadar.Overlay.Window;

namespace iRadar.App;

// Composition root. Wires:
//   IRSDK shared-memory reader (live or replay)
//        ↓ SnapshotReceived
//   RadarEngine.Build
//        ↓ Publish
//   RadarFrameBuffer (lock-free single-writer / single-reader)
//        ↓ poll on render thread
//   RadarOverlay (transparent Win32 window + ImGui via ClickableTransparentOverlay)
//
// Anti-cheat boundary holds: only the public IRSDK MMF + a foreground-window
// query (no process memory access) cross outside of our process.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
        => MainAsync(args).GetAwaiter().GetResult();

    private static async Task<int> MainAsync(string[] args)
    {
        Console.WriteLine("iRadar — starting");
        Console.WriteLine("Anti-cheat boundary: external process, IRSDK shared memory only.");
        Console.WriteLine("Close the overlay window (Alt+F4) or press Ctrl+C in this console to stop.");
        Console.WriteLine();

        var frames = new RadarFrameBuffer();
        var engine = new RadarEngine();

        var telemetry = new IrsdkTelemetrySource();
        telemetry.StateChanged += (_, state) =>
            Console.WriteLine($"[telemetry] {state}");

        telemetry.SnapshotReceived += (_, snapshot) =>
        {
            try
            {
                var frame = engine.Build(snapshot);
                frames.Publish(snapshot, frame);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[engine] {ex.GetType().Name}: {ex.Message}");
            }
        };

        var hostDetector = new HostProcessDetector(
            new Win32ForegroundWindowQuery(),
            IRacingProcessNames.All);

        var iRacingFinder = new IRacingWindowFinder();
        var monitorLocator = new MonitorLocator();
        var windowMover = new OverlayWindowMover();

        var overlay = new RadarOverlay(
            frames,
            hostDetector,
            iRacingFinder,
            monitorLocator,
            windowMover,
            log: msg => Console.WriteLine(msg));

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            overlay.Close();
        };

        try
        {
            await telemetry.StartAsync(cts.Token).ConfigureAwait(false);
            Console.WriteLine("[overlay] starting render loop");
            // ClickableTransparentOverlay.Overlay.Run() runs the message
            // pump and blocks until the window is closed.
            await overlay.Run().ConfigureAwait(false);
        }
        finally
        {
            Console.WriteLine("[shutdown] stopping telemetry");
            await telemetry.DisposeAsync().ConfigureAwait(false);
            overlay.Dispose();
        }

        return 0;
    }
}
