using Velopack;
using Velopack.Sources;

namespace iRadar.App;

// Background check against GitHub Releases for newer iRadar builds. Downloads
// any update silently but does NOT restart mid-session — Velopack's
// WaitExitThenApplyUpdates queues the install for when the user closes the
// overlay normally. Next launch starts on the new version.
//
// Skipped entirely in dev / portable runs (UpdateManager.IsInstalled == false),
// so `dotnet run` is unaffected by network or Velopack quirks.
internal static class AppUpdater
{
    private const string GitHubRepoUrl = "https://github.com/nrjpsantos-design/iCorre-overlay";

    public static async Task CheckOnStartupAsync(Action<string> log)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false));

            if (!mgr.IsInstalled)
            {
                log("[update] dev / portable run — skipping update check");
                return;
            }

            log("[update] checking GitHub Releases…");
            var newVersion = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (newVersion is null)
            {
                log("[update] already on the latest version");
                return;
            }

            log($"[update] {newVersion.TargetFullRelease.Version} available — downloading in background");
            await mgr.DownloadUpdatesAsync(newVersion).ConfigureAwait(false);

            // Queue the install for whenever the user exits the overlay
            // normally — do NOT restart mid-race.
            mgr.WaitExitThenApplyUpdates(newVersion);
            log($"[update] {newVersion.TargetFullRelease.Version} downloaded — will install on next launch");
        }
        catch (Exception ex)
        {
            // Updates failing is never a reason to crash the app. Most
            // common cause: no internet during pre-race setup.
            log($"[update] check failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
