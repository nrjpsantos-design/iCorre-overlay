using iRadar.Core.Telemetry;

namespace iRadar.Core.Radar;

// Single-writer / multi-reader handoff for the latest RadarFrame plus the
// raw TelemetrySnapshot it was derived from. The producer thread (telemetry
// callback) calls Publish; the consumer thread (overlay render loop) reads
// Frame / Snapshot.
//
// Volatile reads/writes give per-field atomic visibility for references on
// .NET 8 — readers may see either the previous or the latest pair, but never
// a torn write within a single field. The reader explicitly fetches both
// fields once per frame and treats them as a consistent pair for that frame.
public sealed class RadarFrameBuffer
{
    private RadarFrame? _frame;
    private TelemetrySnapshot? _snapshot;

    public RadarFrame? Frame => Volatile.Read(ref _frame);
    public TelemetrySnapshot? Snapshot => Volatile.Read(ref _snapshot);

    public void Publish(TelemetrySnapshot snapshot, RadarFrame frame)
    {
        // Publish snapshot first; if a reader observes the new snapshot but
        // the old frame, that's a transient inconsistency that resolves on
        // the next render — preferable to a torn intermediate frame.
        Volatile.Write(ref _snapshot, snapshot);
        Volatile.Write(ref _frame, frame);
    }

    public void Clear()
    {
        Volatile.Write(ref _snapshot, null);
        Volatile.Write(ref _frame, null);
    }
}
