namespace CongTy.Windows.Updates;

public interface IAppUpdateService
{
    AppUpdateSnapshot Current { get; }
    event EventHandler<AppUpdateSnapshot>? StateChanged;

    Task<AppUpdateSnapshot> CheckAsync(CancellationToken cancellationToken = default);
    Task<AppUpdateSnapshot> InstallAsync(CancellationToken cancellationToken = default);
}
