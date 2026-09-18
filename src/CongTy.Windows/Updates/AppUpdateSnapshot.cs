namespace CongTy.Windows.Updates;

public enum AppUpdatePhase
{
    Idle,
    Unsupported,
    Checking,
    Available,
    Downloading,
    Ready,
    Installing,
    UpToDate,
    Updated,
    Error
}

public sealed record AppUpdateProgress(
    double Percent,
    long Transferred,
    long Total,
    double BytesPerSecond);

public sealed record AppUpdateSnapshot(
    AppUpdatePhase Phase,
    string CurrentVersion,
    string? AvailableVersion,
    string? PreviousVersion,
    string ReleaseNotes,
    AppUpdateProgress? Progress,
    string Message,
    string? Error);

public static class AppUpdatePhaseRules
{
    public static bool IsAllowed(AppUpdatePhase current, AppUpdatePhase next)
    {
        if (current == next || next == AppUpdatePhase.Error) return true;

        return current switch
        {
            AppUpdatePhase.Idle or AppUpdatePhase.UpToDate or AppUpdatePhase.Updated or AppUpdatePhase.Error =>
                next == AppUpdatePhase.Checking,
            AppUpdatePhase.Checking =>
                next is AppUpdatePhase.Available or AppUpdatePhase.UpToDate,
            AppUpdatePhase.Available =>
                next == AppUpdatePhase.Downloading,
            AppUpdatePhase.Downloading =>
                next == AppUpdatePhase.Ready,
            AppUpdatePhase.Ready =>
                next == AppUpdatePhase.Installing,
            _ => false
        };
    }
}
