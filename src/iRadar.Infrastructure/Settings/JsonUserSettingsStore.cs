using System.Text.Json;
using iRadar.Core.Settings;

namespace iRadar.Infrastructure.Settings;

// Reads and writes UserSettings to %LocalAppData%\iRadar\settings.json.
// Best-effort: any I/O or parse failure falls back to defaults so a corrupt
// settings file never prevents the overlay from starting. The user can
// always delete the file to reset.
public sealed class JsonUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;
    private readonly Action<string> _log;

    public JsonUserSettingsStore(Action<string>? log = null)
    {
        _log = log ?? (_ => { });

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "iRadar");
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
            // If LocalAppData isn't writable for some reason, fall back to
            // %TEMP%. Users can override later.
            dir = Path.GetTempPath();
        }
        _path = Path.Combine(dir, "settings.json");
    }

    // Exposed for diagnostics ("[settings] saved to ..."). Named FilePath
    // (not Path) to avoid shadowing System.IO.Path inside this class.
    public string FilePath => _path;

    public UserSettings Load()
    {
        if (!File.Exists(_path))
        {
            _log($"[settings] no file at {_path} — using defaults");
            return UserSettings.CreateDefaults();
        }

        try
        {
            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOpts)
                ?? UserSettings.CreateDefaults();
            settings.EnsureAllWidgetsPresent();
            _log($"[settings] loaded from {_path} (version {settings.Version})");
            return settings;
        }
        catch (Exception ex)
        {
            _log($"[settings] could not load {_path}: {ex.GetType().Name} — using defaults");
            return UserSettings.CreateDefaults();
        }
    }

    public bool TrySave(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOpts);
            File.WriteAllText(_path, json);
            _log($"[settings] saved to {_path}");
            return true;
        }
        catch (Exception ex)
        {
            _log($"[settings] could not save {_path}: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
