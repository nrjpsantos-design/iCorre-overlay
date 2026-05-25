namespace iRadar.App;

// Temporarily stubbed while we diagnose the Velopack restore failure on the
// Windows CI runner. Once Velopack is back, this returns to checking
// GitHub Releases on startup and queueing updates for next exit.
internal static class AppUpdater
{
    public static Task CheckOnStartupAsync(Action<string> log)
    {
        log("[update] auto-update temporarily disabled (Velopack restore issue)");
        return Task.CompletedTask;
    }
}
