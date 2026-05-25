using System.Numerics;
using iRadar.Core.Settings;
using iRadar.Overlay.Widgets;
using Xunit;

namespace iRadar.Infrastructure.Tests.Widgets;

public class WidgetLayoutManagerTests
{
    private static WidgetLayoutManager BuildManager()
    {
        var defaults = UserSettings.CreateDefaults();
        return new WidgetLayoutManager(defaults.Widgets);
    }

    [Fact]
    public void Get_ReturnsSeededLayout()
    {
        var mgr = BuildManager();
        var layout = mgr.Get(WidgetIds.Status);
        Assert.Equal(WidgetIds.Status, layout.Id);
        Assert.True(layout.Visible);
    }

    [Fact]
    public void Update_PersistsNewPositionAndSize()
    {
        var mgr = BuildManager();
        mgr.Update(WidgetIds.Radar, new Vector2(500, 600), new Vector2(300, 300));

        var layout = mgr.Get(WidgetIds.Radar);
        Assert.Equal(500f, layout.X);
        Assert.Equal(600f, layout.Y);
        Assert.Equal(300f, layout.Width);
        Assert.Equal(300f, layout.Height);
    }

    [Fact]
    public void Update_NoOpWhenValuesUnchanged()
    {
        // No exception, no state change.
        var mgr = BuildManager();
        var before = mgr.Get(WidgetIds.Status);
        mgr.Update(WidgetIds.Status, new Vector2(before.X, before.Y), new Vector2(before.Width, before.Height));
        var after = mgr.Get(WidgetIds.Status);
        Assert.Equal(before, after);
    }

    [Fact]
    public void SetVisibility_TogglesFlagAndSurvivesSnapshot()
    {
        var mgr = BuildManager();
        Assert.True(mgr.Get(WidgetIds.Radar).Visible);

        mgr.SetVisibility(WidgetIds.Radar, false);

        Assert.False(mgr.Get(WidgetIds.Radar).Visible);

        // Snapshot (which is what gets persisted) must reflect the change.
        var snap = mgr.Snapshot();
        Assert.False(snap[WidgetIds.Radar].Visible);

        mgr.SetVisibility(WidgetIds.Radar, true);
        Assert.True(mgr.Get(WidgetIds.Radar).Visible);
    }

    [Fact]
    public void SetVisibility_NoOpForUnknownId()
    {
        var mgr = BuildManager();
        mgr.SetVisibility("nonexistent-widget", false);  // shouldn't throw
        // All known widgets still in their original state.
        foreach (var id in WidgetIds.All)
        {
            Assert.True(mgr.Get(id).Visible);
        }
    }

    [Fact]
    public void Snapshot_IsIndependentCopy()
    {
        var mgr = BuildManager();
        var snap1 = mgr.Snapshot();
        mgr.SetVisibility(WidgetIds.Status, false);
        var snap2 = mgr.Snapshot();

        Assert.True(snap1[WidgetIds.Status].Visible);   // original snapshot unaffected
        Assert.False(snap2[WidgetIds.Status].Visible);  // new snapshot reflects change
    }
}
