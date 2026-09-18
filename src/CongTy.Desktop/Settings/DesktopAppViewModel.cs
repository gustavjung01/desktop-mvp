using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.Windows.Updates;

namespace CongTy.Desktop.Settings;

public sealed class DesktopAppViewModel : INotifyPropertyChanged
{
    private readonly IAppUpdateService _updateService;
    private AppUpdateSnapshot _snapshot;
    private bool _requesting;

    public DesktopAppViewModel(IAppUpdateService updateService)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _snapshot = updateService.Current;
        _updateService.StateChanged += UpdateService_OnStateChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentVersion => $"v{_snapshot.CurrentVersion}";
    public string AvailableVersion =>
        string.IsNullOrWhiteSpace(_snapshot.AvailableVersion)
            ? "—"
            : $"v{_snapshot.AvailableVersion}";

    public string StatusLabel => _snapshot.Phase switch
    {
        AppUpdatePhase.Checking => "Đang kiểm tra",
        AppUpdatePhase.Available => "Có bản mới",
        AppUpdatePhase.Downloading => "Đang tải",
        AppUpdatePhase.Ready => "Sẵn sàng cài",
        AppUpdatePhase.Installing => "Đang cập nhật",
        AppUpdatePhase.UpToDate => "Mới nhất",
        AppUpdatePhase.Updated => "Đã cập nhật",
        AppUpdatePhase.Error => "Có lỗi",
        AppUpdatePhase.Unsupported => "Chỉ bản đã cài",
        _ => "Sẵn sàng"
    };

    public string Message => _snapshot.Message;
    public string Error => _snapshot.Error ?? string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(_snapshot.Error);
    public string ReleaseNotes =>
        string.IsNullOrWhiteSpace(_snapshot.ReleaseNotes)
            ? "Chưa có ghi chú phát hành."
            : _snapshot.ReleaseNotes;

    public bool HasReleaseNotes => !string.IsNullOrWhiteSpace(_snapshot.ReleaseNotes);
    public bool IsProgressVisible =>
        _snapshot.Phase is AppUpdatePhase.Downloading or AppUpdatePhase.Ready;
    public double ProgressPercent => Math.Clamp(_snapshot.Progress?.Percent ?? 0, 0, 100);

    public string ProgressText
    {
        get
        {
            var progress = _snapshot.Progress;
            if (progress is null) return "0%";

            var parts = new List<string>
            {
                $"{Math.Round(ProgressPercent):0}%"
            };
            if (progress.Total > 0)
                parts.Add($"{FormatBytes(progress.Transferred)} / {FormatBytes(progress.Total)}");
            if (progress.BytesPerSecond > 0)
                parts.Add($"{FormatBytes((long)progress.BytesPerSecond)}/s");
            return string.Join(" · ", parts);
        }
    }

    public string InstallModeText =>
        _snapshot.Phase == AppUpdatePhase.Unsupported
            ? "Development mode"
            : "Bản đã cài Windows";

    public bool CanInstall => !_requesting && _snapshot.Phase == AppUpdatePhase.Ready;
    public bool CanCheck =>
        !_requesting
        && _snapshot.Phase is not (
            AppUpdatePhase.Unsupported
            or AppUpdatePhase.Checking
            or AppUpdatePhase.Available
            or AppUpdatePhase.Downloading
            or AppUpdatePhase.Ready
            or AppUpdatePhase.Installing);

    public bool CanPrimaryAction => CanInstall || CanCheck;

    public string PrimaryActionLabel => _snapshot.Phase switch
    {
        AppUpdatePhase.Ready => "KHỞI ĐỘNG LẠI & CẬP NHẬT",
        AppUpdatePhase.Downloading or AppUpdatePhase.Available => "ĐANG TẢI...",
        AppUpdatePhase.Checking => "ĐANG KIỂM TRA...",
        AppUpdatePhase.Installing => "ĐANG CẬP NHẬT...",
        _ => "KIỂM TRA CẬP NHẬT"
    };

    public async Task CheckAsync()
    {
        if (!CanCheck) return;

        _requesting = true;
        NotifyAll();
        try
        {
            Apply(await _updateService.CheckAsync());
        }
        finally
        {
            _requesting = false;
            NotifyAll();
        }
    }

    public async Task<bool> InstallAsync()
    {
        if (!CanInstall) return false;

        _requesting = true;
        NotifyAll();
        try
        {
            Apply(await _updateService.InstallAsync());
            return _snapshot.Phase == AppUpdatePhase.Installing;
        }
        finally
        {
            _requesting = false;
            NotifyAll();
        }
    }

    private void UpdateService_OnStateChanged(object? sender, AppUpdateSnapshot snapshot)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Apply(snapshot);
            return;
        }

        dispatcher.BeginInvoke(new Action(() => Apply(snapshot)));
    }

    private void Apply(AppUpdateSnapshot snapshot)
    {
        _snapshot = snapshot;
        NotifyAll();
    }

    private void NotifyAll()
    {
        foreach (var property in new[]
        {
            nameof(CurrentVersion),
            nameof(AvailableVersion),
            nameof(StatusLabel),
            nameof(Message),
            nameof(Error),
            nameof(HasError),
            nameof(ReleaseNotes),
            nameof(HasReleaseNotes),
            nameof(IsProgressVisible),
            nameof(ProgressPercent),
            nameof(ProgressText),
            nameof(InstallModeText),
            nameof(CanInstall),
            nameof(CanCheck),
            nameof(CanPrimaryAction),
            nameof(PrimaryActionLabel)
        })
            OnPropertyChanged(property);
    }

    private static string FormatBytes(long value)
    {
        if (value <= 0) return "0 MB";

        var megabytes = value / (1024d * 1024d);
        return megabytes >= 1024
            ? $"{megabytes / 1024d:0.00} GB"
            : $"{megabytes:0.0} MB";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
