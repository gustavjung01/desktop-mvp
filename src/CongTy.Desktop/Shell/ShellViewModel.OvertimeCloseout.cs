using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _overtimeCloseoutSelectionObserverAttached;

    public bool CanOpenOvertimeCloseoutWorkspace => CanViewWorkforceOvertime;
    public bool IsOvertimeCloseoutSelected => SelectedWorkspaceIndex == 55;

    internal void InitializeOvertimeCloseoutShell() => EnsureOvertimeCloseoutSelectionObserver();

    public Task NavigateOvertimeCloseoutAsync()
    {
        EnsureOvertimeCloseoutSelectionObserver();
        SetSelectedNavigation("workforce.overtime");
        IsWorkforceOpen = true;

        if (!CanOpenOvertimeCloseoutWorkspace)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tăng ca & chốt công.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 55;
        return Task.CompletedTask;
    }

    private void EnsureOvertimeCloseoutSelectionObserver()
    {
        if (_overtimeCloseoutSelectionObserverAttached) return;

        PropertyChanged += OvertimeCloseoutShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanOpenOvertimeCloseoutWorkspace));
            if (!CanOpenOvertimeCloseoutWorkspace && IsOvertimeCloseoutSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tăng ca & chốt công.";
        };
        _overtimeCloseoutSelectionObserverAttached = true;
    }

    private void OvertimeCloseoutShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsOvertimeCloseoutSelected));
    }
}
