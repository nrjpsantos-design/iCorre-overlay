namespace iRadar.Overlay.Window;

// Decides whether the overlay should be visible by checking if any of the
// known host process names owns the current foreground window. Stateless,
// thread-safe for read.
public sealed class HostProcessDetector
{
    private readonly IForegroundWindowQuery _query;
    private readonly HashSet<string> _targets;

    public HostProcessDetector(
        IForegroundWindowQuery query,
        IEnumerable<string> targetProcessNames)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(targetProcessNames);

        _query = query;
        _targets = new HashSet<string>(targetProcessNames, StringComparer.OrdinalIgnoreCase);

        if (_targets.Count == 0)
        {
            throw new ArgumentException(
                "At least one target process name is required.",
                nameof(targetProcessNames));
        }
    }

    public bool IsHostInForeground()
    {
        var name = _query.GetForegroundProcessName();
        return name is not null && _targets.Contains(name);
    }
}
