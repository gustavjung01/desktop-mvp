using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _attendanceSelectionObserverAttached;

    public bool IsAttendanceSelected => SelectedWorkspaceIndex == WorkspaceSlots.Attendance;

    internal void InitializeAttendanceShell() => EnsureAttendanceSelectionObserver();

    public Task NavigateAttendanceAsync()
    {
        EnsureAttendanceSelectionObserver();
        SetSelectedNavigation("workforce.attendance");
        IsWorkforceOpen = true;

        if (!CanViewWorkforceAttendance)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền sử dụng Chấm công.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.Attendance;
        return Task.CompletedTask;
    }

    private void EnsureAttendanceSelectionObserver()
    {
        if (_attendanceSelectionObserverAttached) return;

        PropertyChanged += AttendanceShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            if (!CanViewWorkforceAttendance && IsAttendanceSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền sử dụng Chấm công.";
        };
        _attendanceSelectionObserverAttached = true;
    }

    private void AttendanceShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsAttendanceSelected));
    }
}
