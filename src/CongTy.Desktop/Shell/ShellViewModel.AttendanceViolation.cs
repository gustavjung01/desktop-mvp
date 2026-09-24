using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _attendanceViolationSelectionObserverAttached;

    public bool IsAttendanceViolationSelected =>
        SelectedWorkspaceIndex == WorkspaceSlots.AttendanceViolation;

    internal void InitializeAttendanceViolationShell() =>
        EnsureAttendanceViolationSelectionObserver();

    public Task NavigateAttendanceViolationAsync()
    {
        EnsureAttendanceViolationSelectionObserver();
        SetSelectedNavigation("workforce.violations");
        IsWorkforceOpen = true;

        if (!CanViewWorkforceViolations)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Xử lý vi phạm chấm công.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.AttendanceViolation;
        return Task.CompletedTask;
    }

    private void EnsureAttendanceViolationSelectionObserver()
    {
        if (_attendanceViolationSelectionObserverAttached) return;

        PropertyChanged += AttendanceViolationShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            if (!CanViewWorkforceViolations && IsAttendanceViolationSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Xử lý vi phạm chấm công.";
        };
        _attendanceViolationSelectionObserverAttached = true;
    }

    private void AttendanceViolationShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsAttendanceViolationSelected));
    }
}
