using iRadar.Core.Telemetry;
using iRadar.Infrastructure.Telemetry;

namespace iRadar.Replay;

// Fase 1 deliverable: live telemetry dumper.
//
// Usage:
//   1. Open iRacing and load a saved replay (Replays tab → pick a session).
//      This is the zero-risk way to feed real telemetry through this tool.
//   2. Run `dotnet run --project tests/iRadar.Replay`.
//   3. Watch the console — every 100ms it prints the player's Speed, Lap,
//      LapDistPct, the car count, PlayerCarIdx, and iRacing's built-in
//      LEFT/RIGHT spotter state.
//   4. Press Ctrl+C to stop.
//
// Fase 3 will extend this with `--ibt <file>` to play back .ibt files
// without iRacing running at all.
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("iRadar — Fase 1 telemetry dumper");
        Console.WriteLine("Open iRacing in replay mode for zero-risk testing.");
        Console.WriteLine("Ctrl+C to stop.");
        Console.WriteLine();

        await using var source = new IrsdkTelemetrySource();
        source.StateChanged += (_, state) =>
            Console.WriteLine($"[state] {state}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await source.StartAsync(cts.Token).ConfigureAwait(false);

        TelemetrySnapshot? lastPrinted = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (!cts.IsCancellationRequested)
        {
            var snap = source.Latest;
            if (snap is not null && !ReferenceEquals(snap, lastPrinted))
            {
                lastPrinted = snap;
                PrintSnapshot(snap);
            }

            try { await Task.Delay(100, cts.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            // Heartbeat every ~3s even when no snapshots arrive (e.g., iRacing closed)
            if (sw.Elapsed.TotalSeconds >= 3 && snap is null)
            {
                sw.Restart();
                Console.WriteLine($"[state] {source.State} (no snapshot yet)");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Stopping…");
        await source.StopAsync().ConfigureAwait(false);
        return 0;
    }

    private static void PrintSnapshot(TelemetrySnapshot s)
    {
        var speedKmh = s.PlayerSpeedMs * 3.6f;
        Console.WriteLine(
            $"tick={s.SessionTick,8} " +
            $"playerIdx={s.PlayerCarIdx,3} " +
            $"speed={speedKmh,6:F1}km/h " +
            $"lap={s.PlayerLap,3} " +
            $"lapPct={s.PlayerLapDistPct,5:F3} " +
            $"cars={s.Cars.Count,2} " +
            $"proximity={s.Proximity,-13} " +
            $"track=\"{s.Session.TrackName}\"" +
            (s.IsOnTrack ? " [on-track]" : " [pit/replay]"));
    }
}
