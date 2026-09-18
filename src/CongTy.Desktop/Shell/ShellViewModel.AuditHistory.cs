using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _auditHistorySelectionObserverAttached;

    public bool CanViewAuditHistory =>
        _access.HasPermission("core.reporting.audit-history.read");

    public bool IsAuditHistorySelected => SelectedWorkspaceIndex == 45;

    internal void InitializeAuditHistoryShell() => EnsureAuditHistorySelectionObserver();

    public Task NavigateAuditHistoryAsync()
    {
        EnsureAuditHistorySelectionObserver();
        SetSelectedNavigation("operations.audit-history");

        if (!CanViewAuditHistory)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lịch sử thay đổi.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 45;
        return Task.CompletedTask;
    }

    private void EnsureAuditHistorySelectionObserver()
    {
        if (_auditHistorySelectionObserverAttached) return;

        PropertyChanged += AuditHistoryShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewAuditHistory));
            if (!CanViewAuditHistory && IsAuditHistorySelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lịch sử thay đổi.";
        };
        _auditHistorySelectionObserverAttached = true;
    }

    private void AuditHistoryShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsAuditHistorySelected));
    }
}
