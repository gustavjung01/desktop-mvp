using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _leaveSelectionObserverAttached;

    public bool CanOpenLeaveWorkspace => CanViewWorkforceLeave;
    public bool IsLeaveSelected => SelectedWorkspaceIndex == WorkspaceSlots.Leave;

    internal void InitializeLeaveShell() => EnsureLeaveSelectionObserver();

    public Task NavigateLeaveAsync()
    {
        EnsureLeaveSelectionObserver();
        SetSelectedNavigation("workforce.leave");
        IsWorkforceOpen = true;

        if (!CanOpenLeaveWorkspace)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Nghỉ và đơn nghỉ.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.Leave;
        return Task.CompletedTask;
    }

    private void EnsureLeaveSelectionObserver()
    {
        if (_leaveSelectionObserverAttached) return;

        PropertyChanged += LeaveShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanOpenLeaveWorkspace));
            if (!CanOpenLeaveWorkspace && IsLeaveSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Nghỉ và đơn nghỉ.";
        };
        _leaveSelectionObserverAttached = true;
    }

    private void LeaveShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsLeaveSelected));
    }
}
