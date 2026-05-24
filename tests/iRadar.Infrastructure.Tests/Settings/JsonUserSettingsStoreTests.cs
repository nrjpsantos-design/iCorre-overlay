using iRadar.Core.Settings;
using iRadar.Infrastructure.Settings;
using Xunit;

namespace iRadar.Infrastructure.Tests.Settings;

public class JsonUserSettingsStoreTests
{
    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        // Use the real store but redirect via a unique LocalAppData. We can't
        // easily mock the path, so we accept it writes into the test runner's
        // LocalAppData under iRadar — and clean up afterward.
        var store = new JsonUserSettingsStore();
        var backup = File.Exists(store.FilePath) ? File.ReadAllText(store.FilePath) : null;

        try
        {
            var original = UserSettings.CreateDefaults();
            original.Widgets[WidgetIds.Status] = new WidgetLayout
            {
                Id = WidgetIds.Status,
                X = 123f, Y = 456f,
                Width = 200f, Height = 100f,
                Visible = false,
            };

            Assert.True(store.TrySave(original));
            var loaded = store.Load();

            Assert.Equal(original.Version, loaded.Version);
            Assert.Equal(original.Widgets.Count, loaded.Widgets.Count);
            Assert.Equal(123f, loaded.Widgets[WidgetIds.Status].X);
            Assert.Equal(456f, loaded.Widgets[WidgetIds.Status].Y);
            Assert.False(loaded.Widgets[WidgetIds.Status].Visible);
            Assert.True(loaded.Widgets[WidgetIds.Radar].Visible);
        }
        finally
        {
            // Restore previous file if any, otherwise remove the test file.
            if (backup is not null) File.WriteAllText(store.FilePath, backup);
            else if (File.Exists(store.FilePath)) File.Delete(store.FilePath);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var store = new JsonUserSettingsStore();
        var backup = File.Exists(store.FilePath) ? File.ReadAllText(store.FilePath) : null;
        if (File.Exists(store.FilePath)) File.Delete(store.FilePath);

        try
        {
            var loaded = store.Load();
            Assert.Equal(UserSettings.CurrentVersion, loaded.Version);
            Assert.Equal(WidgetIds.All.Count, loaded.Widgets.Count);
        }
        finally
        {
            if (backup is not null) File.WriteAllText(store.FilePath, backup);
        }
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        var store = new JsonUserSettingsStore();
        var backup = File.Exists(store.FilePath) ? File.ReadAllText(store.FilePath) : null;
        File.WriteAllText(store.FilePath, "{ not valid json !!!");

        try
        {
            var loaded = store.Load();
            Assert.Equal(UserSettings.CurrentVersion, loaded.Version);
            Assert.True(loaded.Widgets.Count > 0);
        }
        finally
        {
            if (backup is not null) File.WriteAllText(store.FilePath, backup);
            else if (File.Exists(store.FilePath)) File.Delete(store.FilePath);
        }
    }
}
