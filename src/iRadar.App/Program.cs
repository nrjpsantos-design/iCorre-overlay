using iRadar.Core.Radar;
using iRadar.Core.Settings;
using iRadar.Infrastructure.Settings;
using iRadar.Infrastructure.Telemetry;
using iRadar.Overlay.Widgets;
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
    {
        // Velopack hook temporarily removed pending diagnosis of the
        // Windows-runner restore failure. Once the package is back, this
        // becomes:  VelopackApp.Build().Run();
        return MainAsync(args).GetAwaiter().GetResult();
    }

    private static void Log(string message)
    {
        Console.WriteLine(message);
        FileLog.Write(message);
    }

    private static async Task<int> MainAsync(string[] args)
    {
        Log($"iRadar — starting (log: {FileLog.Path})");
        Log("Anti-cheat boundary: external process, IRSDK shared memory only.");
        Log("Close the overlay window (Alt+F4) or press Ctrl+C to stop.");
        Log(string.Empty);

        // Fire-and-forget — never blocks startup. No-op when running via
        // `dotnet run` (not installed).
        _ = Task.Run(() => AppUpdater.CheckOnStartupAsync(Log));

        var frames = new RadarFrameBuffer();
        var engine = new RadarEngine();

        var telemetry = new IrsdkTelemetrySource();
        telemetry.StateChanged += (_, state) => Log($"[telemetry] {state}");

        telemetry.SnapshotReceived += (_, snapshot) =>
        {
            try
            {
                var frame = engine.Build(snapshot);
                frames.Publish(snapshot, frame);
            }
            catch (Exception ex)
            {
                Log($"[engine] {ex.GetType().Name}: {ex.Message}");
            }
        };

        var hostDetector = new HostProcessDetector(
            new Win32ForegroundWindowQuery(),
            IRacingProcessNames.All);

        var monitorLocator = new MonitorLocator();
        var windowMover = new OverlayWindowMover();
        var styleManager = new WindowStyleManager();

        var settingsStore = new JsonUserSettingsStore(Log);
        var settings = settingsStore.Load();
        var layouts = new WidgetLayoutManager(settings.Widgets);

        var hotkey = new GlobalHotkey();
        var editMode = new EditModeController(hotkey, isActive =>
        {
            Log($"[overlay] edit mode {(isActive ? "ON" : "OFF")}");
            if (!isActive)
            {
                // Toggle OFF: persist whatever the user dragged to.
                settings.Widgets = layouts.Snapshot();
                settingsStore.TrySave(settings);
            }
        });

        // Size the overlay window to cover the full virtual desktop so the
        // user can drag widgets onto any monitor during Edit Mode. The
        // window is repositioned to (XVirtualScreen, YVirtualScreen) by
        // RadarOverlay.PostInitialized so monitors arranged left-of-primary
        // are covered too.
        var virtualDesktop = monitorLocator.GetVirtualDesktopBounds();
        Log($"[overlay] virtual desktop bounds: {virtualDesktop.Width}x{virtualDesktop.Height} at ({virtualDesktop.X}, {virtualDesktop.Y})");

        var overlay = new RadarOverlay(
            virtualDesktop.Width,
            virtualDesktop.Height,
            frames,
            hostDetector,
            monitorLocator,
            windowMover,
            styleManager,
            editMode,
            layouts,
            log: Log);

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
            Log("[overlay] starting render loop");
            // ClickableTransparentOverlay.Overlay.Run() runs the message
            // pump and blocks until the window is closed.
            await overlay.Run().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Catch-all so a startup crash is captured in the log file even
            // when the console isn't visible.
            Log($"[fatal] {ex.GetType().FullName}: {ex.Message}");
            Log(ex.StackTrace ?? "(no stack trace)");
            return 1;
        }
        finally
        {
            Log("[shutdown] stopping telemetry");
            await telemetry.DisposeAsync().ConfigureAwait(false);
            overlay.Dispose();
        }

        return 0;
    }
}
