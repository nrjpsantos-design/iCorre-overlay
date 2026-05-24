namespace iRadar.Core.Settings;

// User-customisable settings persisted to settings.json. Versioned so future
// schema migrations can detect old files and either upgrade them or fall
// back to defaults.
//
// Widgets is a Dictionary (rather than IReadOnlyDictionary) so it round-trips
// through System.Text.Json without custom converters. The dictionary keys
// are WidgetIds constants.
public sealed class UserSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public Dictionary<string, WidgetLayout> Widgets { get; set; } = new();

    public static UserSettings CreateDefaults() => new()
    {
        Version = CurrentVersion,
        Widgets = new Dictionary<string, WidgetLayout>
        {
            [WidgetIds.Status] = new WidgetLayout
            {
                Id = WidgetIds.Status,
                X = 20f,  Y = 20f,
                Width = 340f, Height = 150f,
                Visible = true,
            },
            [WidgetIds.Radar] = new WidgetLayout
            {
                Id = WidgetIds.Radar,
                X = 20f,  Y = 200f,
                Width = 260f, Height = 260f,
                Visible = true,
            },
            [WidgetIds.Relative] = new WidgetLayout
            {
                Id = WidgetIds.Relative,
                X = 300f, Y = 20f,
                Width = 340f, Height = 280f,
                Visible = true,
            },
        },
    };

    // After load: ensure every known widget has a layout (settings file from
    // an older version may be missing one). Missing entries get the default.
    public void EnsureAllWidgetsPresent()
    {
        var defaults = CreateDefaults();
        foreach (var id in WidgetIds.All)
        {
            if (!Widgets.ContainsKey(id))
            {
                Widgets[id] = defaults.Widgets[id];
            }
        }
    }
}
