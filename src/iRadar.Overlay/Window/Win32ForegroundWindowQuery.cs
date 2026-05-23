using System.Diagnostics;
using System.Runtime.InteropServices;

namespace iRadar.Overlay.Window;

// Live Win32 implementation. Uses the same APIs Task Manager uses to identify
// the foreground app — no process memory access, no handle escalation. The
// process handle .NET opens internally to fetch ProcessName uses only
// PROCESS_QUERY_LIMITED_INFORMATION, which is what every benign system tool
// (Task Manager, Process Explorer, every overlay app) does.
public sealed class Win32ForegroundWindowQuery : IForegroundWindowQuery
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public string? GetForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return null;

        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0) return null;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            // Process exited between handle acquisition and query.
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
