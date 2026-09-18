using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _dataBackupSelectionObserverAttached;

    public bool CanViewDataBackup => _access.Current.IsAuthenticated;
    public bool IsDataBackupSelected => SelectedWorkspaceIndex == 47;

    internal void InitializeDataBackupShell() => EnsureDataBackupSelectionObserver();

    public Task NavigateDataBackupAsync()
    {
        EnsureDataBackupSelectionObserver();
        SetSelectedNavigation("settings.data-backup");

        if (!CanViewDataBackup)
        {
            WorkspaceMessage = "Cần đăng nhập để mở Dữ liệu & sao lưu.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 47;
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
                WorkspaceMessage = "Cần đăng nhập để mở Dữ liệu & sao lưu.";
        };
        _dataBackupSelectionObserverAttached = true;
    }

    private void DataBackupShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsDataBackupSelected));
    }
}
