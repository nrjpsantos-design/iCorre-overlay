using iRadar.Infrastructure.Irsdk;
using Xunit;

namespace iRadar.Infrastructure.Tests.Irsdk;

public class IrsdkSessionStringParserTests
{
    private static string LoadFixture(string name)
    {
        var dir = Path.GetDirectoryName(typeof(IrsdkSessionStringParserTests).Assembly.Location)!;
        var path = Path.Combine(dir, "Fixtures", name);
        return File.ReadAllText(path);
    }

    private static readonly string SpaYaml = LoadFixture("session-spa-3drivers.yaml");

    [Fact]
    public void EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Same(ParsedSessionInfo.Empty, IrsdkSessionStringParser.Parse(string.Empty));
        Assert.Same(ParsedSessionInfo.Empty, IrsdkSessionStringParser.Parse("   \n  "));
    }

    [Fact]
    public void Parses_TrackDisplayName_PreferredOverInternalCode()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal("Circuit de Spa-Francorchamps", info.TrackName);
    }

    [Fact]
    public void Parses_TrackConfigName()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal("Grand Prix Pits", info.TrackConfigName);
    }

    [Fact]
    public void Parses_TrackLength_FromKilometers_IntoMeters()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal(7004f, info.TrackLengthMeters, precision: 1);
    }

    [Fact]
    public void Parses_SessionType_FromFirstSession()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal("Open Practice", info.SessionType);
    }

    [Fact]
    public void Parses_DriverCarIdx_FromDriverInfo()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal(5, info.DriverCarIdx);
    }

    [Fact]
    public void Parses_All_Drivers_InOrder()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal(3, info.Drivers.Count);
        Assert.Collection(
            info.Drivers,
            d => Assert.Equal(0, d.CarIdx),
            d => Assert.Equal(2, d.CarIdx),
            d => Assert.Equal(5, d.CarIdx));
    }

    [Fact]
    public void Parses_DriverFields_PerEntry()
    {
        var info = IrsdkSessionStringParser.Parse(SpaYaml);

        var alice = info.Drivers[0];
        Assert.Equal("Alice Driver", alice.UserName);
        Assert.Equal("11", alice.CarNumber);
        Assert.Equal(1234, alice.IRating);
        Assert.Equal(4011, alice.CarClassId);

        var bob = info.Drivers[1];
        Assert.Equal("Bob Other", bob.UserName);
        Assert.Equal("42", bob.CarNumber);
        Assert.Equal(4567, bob.IRating);

        var me = info.Drivers[2];
        Assert.Equal("Joao Paulo", me.UserName);
        Assert.Equal("99", me.CarNumber);
        Assert.Equal(2987, me.IRating);
    }

    [Fact]
    public void IRating_From_FirstDriver_Doesnt_Bleed_Into_NextDriver()
    {
        // Regression: original lazy multi-line regex could grab the next entry's
        // IRating because of greedy/lazy backtracking across entries.
        var info = IrsdkSessionStringParser.Parse(SpaYaml);
        Assert.Equal(1234, info.Drivers[0].IRating);
        Assert.NotEqual(info.Drivers[1].IRating, info.Drivers[0].IRating);
    }

    [Fact]
    public void Parser_Ignores_Drivers_Without_CarIdx()
    {
        // A malformed entry without CarIdx should be silently dropped.
        const string yaml = """
WeekendInfo:
 TrackName: monza
 TrackLength: 5.793 km
SessionInfo:
 Sessions:
 - SessionType: Race
DriverInfo:
 DriverCarIdx: 0
 Drivers:
 - UserName: Orphan
   CarNumber: "1"
 - CarIdx: 7
   UserName: Real Driver
   CarNumber: "7"
   IRating: 100
SplitTimeInfo:
""";
        var info = IrsdkSessionStringParser.Parse(yaml);
        Assert.Single(info.Drivers);
        Assert.Equal(7, info.Drivers[0].CarIdx);
    }
}
