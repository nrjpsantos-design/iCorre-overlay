using iRadar.Core.Settings;
using Xunit;

namespace iRadar.Core.Tests.Settings;

public class UserSettingsTests
{
    [Fact]
    public void CreateDefaults_HasAllKnownWidgets()
    {
        var s = UserSettings.CreateDefaults();
        Assert.Equal(UserSettings.CurrentVersion, s.Version);
        Assert.Equal(WidgetIds.All.Count, s.Widgets.Count);
        foreach (var id in WidgetIds.All)
        {
            Assert.True(s.Widgets.ContainsKey(id), $"Missing layout for widget '{id}'");
            Assert.Equal(id, s.Widgets[id].Id);
            Assert.True(s.Widgets[id].Visible);
            Assert.True(s.Widgets[id].Width > 0f);
            Assert.True(s.Widgets[id].Height > 0f);
        }
    }

    [Fact]
    public void EnsureAllWidgetsPresent_AddsMissing_LeavesExistingAlone()
    {
        // Settings file written by a previous version that didn't know about
        // the relative widget — simulate by hand-crafting a partial dict.
        var s = new UserSettings
        {
            Version = UserSettings.CurrentVersion,
            Widgets = new Dictionary<string, WidgetLayout>
            {
                [WidgetIds.Status] = new WidgetLayout
                {
                    Id = WidgetIds.Status,
                    X = 999f, Y = 999f, Width = 100f, Height = 100f, Visible = false,
                },
            },
        };

        s.EnsureAllWidgetsPresent();

        // Status layout preserved as-is (custom values).
        Assert.Equal(999f, s.Widgets[WidgetIds.Status].X);
        Assert.False(s.Widgets[WidgetIds.Status].Visible);

        // Radar and Relative filled with defaults.
        Assert.True(s.Widgets.ContainsKey(WidgetIds.Radar));
        Assert.True(s.Widgets.ContainsKey(WidgetIds.Relative));
        Assert.True(s.Widgets[WidgetIds.Radar].Visible);
    }
}
