namespace iRadar.Core.Settings;

// Stable IDs used to key WidgetLayout entries in UserSettings. These are
// constants (not an enum) because they show up in the JSON file the user
// may edit by hand — clearer to read "radar" than "Radar = 1".
public static class WidgetIds
{
    public const string Status   = "status";
    public const string Radar    = "radar";
    public const string Relative = "relative";
    public const string Settings = "settings";
    public const string Flag     = "flag";

    public static IReadOnlyList<string> All { get; } = new[] { Status, Radar, Relative, Settings, Flag };
}
