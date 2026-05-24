using System.Diagnostics;

namespace iRadar.Overlay.Window;

// Locates the main window handle of the running iRacing process. Uses only
// the public Process API — no direct handle escalation, no memory access,
// no enumeration of internal handles. This is the same lookup any
// task-management or streaming-overlay app does.
public sealed class IRacingWindowFinder
{
    private readonly IReadOnlyCollection<string> _processNames;

    public IRacingWindowFinder()
        : this(IRacingProcessNames.All) { }

    public IRacingWindowFinder(IReadOnlyCollection<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);
        if (processNames.Count == 0)
        {
            throw new ArgumentException(
                "At least one process name is required.",
                nameof(processNames));
        }
        _processNames = processNames;
    }

    // Returns the main window HWND of the first running iRacing process, or
    // null if iRacing is not running / has not yet created its main window.
    public IntPtr? TryFindMainWindow()
    {
        foreach (var name in _processNames)
        {
            var processes = Process.GetProcessesByName(name);
            try
            {
                foreach (var p in processes)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        return p.MainWindowHandle;
                    }
                }
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }
        return null;
    }
}
