using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _importExportHistorySelectionObserverAttached;

    public bool CanViewImportExportHistory =>
        _access.HasPermission("core.reporting.audit-history.read");

    public bool IsImportExportHistorySelected => SelectedWorkspaceIndex == 46;

    internal void InitializeImportExportHistoryShell() => EnsureImportExportHistorySelectionObserver();

    public Task NavigateImportExportHistoryAsync()
    {
        EnsureImportExportHistorySelectionObserver();
        SetSelectedNavigation("operations.import-export-history");

        if (!CanViewImportExportHistory)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lịch sử nhập/xuất.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 46;
        return Task.CompletedTask;
    }

    private void EnsureImportExportHistorySelectionObserver()
    {
        if (_importExportHistorySelectionObserverAttached) return;

        PropertyChanged += ImportExportHistoryShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewImportExportHistory));
            if (!CanViewImportExportHistory && IsImportExportHistorySelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lịch sử nhập/xuất.";
        };
        _importExportHistorySelectionObserverAttached = true;
    }

    private void ImportExportHistoryShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsImportExportHistorySelected));
    }
}
