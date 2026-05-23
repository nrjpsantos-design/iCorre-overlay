using iRadar.Overlay.Window;
using Xunit;

namespace iRadar.Infrastructure.Tests.Overlay;

public class HostProcessDetectorTests
{
    private sealed class StubQuery : IForegroundWindowQuery
    {
        public string? CurrentName { get; set; }
        public string? GetForegroundProcessName() => CurrentName;
    }

    [Fact]
    public void Detects_KnownDx11Process()
    {
        var stub = new StubQuery { CurrentName = "iRacingSim64DX11" };
        var detector = new HostProcessDetector(stub, IRacingProcessNames.All);
        Assert.True(detector.IsHostInForeground());
    }

    [Fact]
    public void Detects_KnownDx12Process()
    {
        var stub = new StubQuery { CurrentName = "iRacingSim64DX12" };
        var detector = new HostProcessDetector(stub, IRacingProcessNames.All);
        Assert.True(detector.IsHostInForeground());
    }

    [Fact]
    public void Matching_IsCaseInsensitive()
    {
        var stub = new StubQuery { CurrentName = "iracingsim64dx11" };
        var detector = new HostProcessDetector(stub, IRacingProcessNames.All);
        Assert.True(detector.IsHostInForeground());
    }

    [Fact]
    public void ReturnsFalse_WhenForegroundIsUnrelated()
    {
        var stub = new StubQuery { CurrentName = "notepad" };
        var detector = new HostProcessDetector(stub, IRacingProcessNames.All);
        Assert.False(detector.IsHostInForeground());
    }

    [Fact]
    public void ReturnsFalse_WhenNoForegroundWindow()
    {
        var stub = new StubQuery { CurrentName = null };
        var detector = new HostProcessDetector(stub, IRacingProcessNames.All);
        Assert.False(detector.IsHostInForeground());
    }

    [Fact]
    public void Constructor_RejectsEmptyTargetList()
    {
        var stub = new StubQuery();
        Assert.Throws<ArgumentException>(() =>
            new HostProcessDetector(stub, Array.Empty<string>()));
    }

    [Fact]
    public void Constructor_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HostProcessDetector(null!, IRacingProcessNames.All));
        Assert.Throws<ArgumentNullException>(() =>
            new HostProcessDetector(new StubQuery(), null!));
    }

    [Fact]
    public void CustomTargetList_OverridesDefaults()
    {
        var stub = new StubQuery { CurrentName = "custom-sim" };
        var detector = new HostProcessDetector(stub, new[] { "custom-sim" });
        Assert.True(detector.IsHostInForeground());

        stub.CurrentName = "iRacingSim64DX11";
        Assert.False(detector.IsHostInForeground());
    }
}
