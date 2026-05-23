using iRadar.Core.Radar;
using iRadar.Core.Telemetry;
using Xunit;

namespace iRadar.Core.Tests.Radar;

public class RadarFrameBufferTests
{
    [Fact]
    public void NewBuffer_ReturnsNull()
    {
        var b = new RadarFrameBuffer();
        Assert.Null(b.Frame);
        Assert.Null(b.Snapshot);
    }

    [Fact]
    public void Publish_StoresLatestPair()
    {
        var b = new RadarFrameBuffer();
        var snap = TelemetrySnapshot.Empty with { SessionTick = 100 };
        var frame = RadarFrame.Empty with { SessionTick = 100, IsActive = true };

        b.Publish(snap, frame);

        Assert.Same(snap, b.Snapshot);
        Assert.Same(frame, b.Frame);
    }

    [Fact]
    public void Publish_OverwritesPreviousPair()
    {
        var b = new RadarFrameBuffer();
        b.Publish(
            TelemetrySnapshot.Empty with { SessionTick = 1 },
            RadarFrame.Empty with { SessionTick = 1 });
        b.Publish(
            TelemetrySnapshot.Empty with { SessionTick = 2 },
            RadarFrame.Empty with { SessionTick = 2 });

        Assert.Equal(2, b.Snapshot!.SessionTick);
        Assert.Equal(2, b.Frame!.SessionTick);
    }

    [Fact]
    public void Clear_ResetsToNull()
    {
        var b = new RadarFrameBuffer();
        b.Publish(TelemetrySnapshot.Empty, RadarFrame.Empty);
        b.Clear();
        Assert.Null(b.Frame);
        Assert.Null(b.Snapshot);
    }
}
