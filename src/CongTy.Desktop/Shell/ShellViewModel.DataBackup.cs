using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _dataBackupSelectionObserverAttached;

    public bool CanViewDataBackup => _access.Current.IsAuthenticated;
    public bool IsDataBackupSelected => SelectedWorkspaceIndex is 47 or 48 or 49;
    public bool IsDataBackupWorkspaceSelected => SelectedWorkspaceIndex == 47;
    public bool IsPrintTemplatesSelected => SelectedWorkspaceIndex == 48;
    public bool IsAppearanceSelected => SelectedWorkspaceIndex == 49;

    internal void InitializeDataBackupShell() => EnsureDataBackupSelectionObserver();

    public Task NavigateDataBackupAsync() => NavigateSettingsWorkspaceAsync(
        "settings.data-backup",
        47,
        "Dữ liệu & sao lưu");

    public Task NavigatePrintTemplatesAsync() => NavigateSettingsWorkspaceAsync(
        "settings.print-templates",
        48,
        "Mẫu in");

    public Task NavigateAppearanceAsync() => NavigateSettingsWorkspaceAsync(
        "settings.appearance",
        49,
        "Giao diện");

    private Task NavigateSettingsWorkspaceAsync(string navigationKey, int workspaceIndex, string label)
    {
        EnsureDataBackupSelectionObserver();
        SetSelectedNavigation(navigationKey);

        if (!CanViewDataBackup)
        {
            WorkspaceMessage = $"Cần đăng nhập để mở {label}.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = workspaceIndex;
        return Task.CompletedTask;
    }

    private void EnsureDataBackupSelectionObserver()
    {
        if (_dataBackupSelectionObserverAttached) return;

        PropertyChanged += DataBackupShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewDataBackup));
            if (!CanViewDataBackup && IsDataBackupSelected)
                WorkspaceMessage = "Cần đăng nhập để mở Cài đặt Công Ty.";
        };
        _dataBackupSelectionObserverAttached = true;
    }

    private void DataBackupShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SelectedWorkspaceIndex)) return;
        OnPropertyChanged(nameof(IsDataBackupSelected));
        OnPropertyChanged(nameof(IsDataBackupWorkspaceSelected));
        OnPropertyChanged(nameof(IsPrintTemplatesSelected));
        OnPropertyChanged(nameof(IsAppearanceSelected));
    }
}
