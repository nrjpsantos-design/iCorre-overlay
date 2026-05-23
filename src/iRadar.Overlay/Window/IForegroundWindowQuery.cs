namespace iRadar.Overlay.Window;

// Thin abstraction over GetForegroundWindow + ProcessName lookup. Exists so
// HostProcessDetector can be unit-tested with a stub instead of hitting
// Win32 directly.
public interface IForegroundWindowQuery
{
    // Returns the executable name (without ".exe" extension and without path)
    // of the process owning the current foreground window. Returns null if
    // no window is in foreground or if the process can no longer be queried.
    string? GetForegroundProcessName();
}
