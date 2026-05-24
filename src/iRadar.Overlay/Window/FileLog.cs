namespace iRadar.Overlay.Window;

// Single-file diagnostic log at %LocalAppData%\iRadar\debug.log. Writes are
// best-effort: any I/O failure is swallowed so logging never crashes the
// app. Use this for events the user may need to inspect when the console
// isn't visible (production WinExe, headless launches, etc).
//
// Open from PowerShell:
//   notepad $env:LOCALAPPDATA\iRadar\debug.log
public static class FileLog
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Init();

    public static string Path => LogPath;

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}";
        try
        {
            lock (Gate)
            {
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
            // Logging never throws.
        }
    }

    private static string Init()
    {
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iRadar");
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "debug.log");
        }
        catch
        {
            // Fall back to temp directory.
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iRadar.debug.log");
        }
    }
}
