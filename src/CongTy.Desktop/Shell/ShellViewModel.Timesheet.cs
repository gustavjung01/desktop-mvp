using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _timesheetSelectionObserverAttached;

    public bool IsTimesheetSelected => SelectedWorkspaceIndex == WorkspaceSlots.Timesheet;

    internal void InitializeTimesheetShell() => EnsureTimesheetSelectionObserver();

    public Task NavigateTimesheetAsync()
    {
        EnsureTimesheetSelectionObserver();
        SetSelectedNavigation("workforce.timesheet");
        IsWorkforceOpen = true;

        if (!CanViewWorkforceTimesheet)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Bảng công.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.Timesheet;
        return Task.CompletedTask;
    }

    private void EnsureTimesheetSelectionObserver()
    {
        if (_timesheetSelectionObserverAttached) return;

        PropertyChanged += TimesheetShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            if (!CanViewWorkforceTimesheet && IsTimesheetSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Bảng công.";
        };
        _timesheetSelectionObserverAttached = true;
    }

    private void TimesheetShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsTimesheetSelected));
    }
}
