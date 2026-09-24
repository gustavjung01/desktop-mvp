using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _attendanceAdjustmentSelectionObserverAttached;

    public bool IsAttendanceAdjustmentSelected =>
        SelectedWorkspaceIndex == WorkspaceSlots.AttendanceAdjustment;

    internal void InitializeAttendanceAdjustmentShell() =>
        EnsureAttendanceAdjustmentSelectionObserver();

    public Task NavigateAttendanceAdjustmentAsync()
    {
        EnsureAttendanceAdjustmentSelectionObserver();
        SetSelectedNavigation("workforce.adjustments");
        IsWorkforceOpen = true;

        if (!CanViewWorkforceAdjustments)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền sử dụng Điều chỉnh công.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.AttendanceAdjustment;
        return Task.CompletedTask;
    }

    private void EnsureAttendanceAdjustmentSelectionObserver()
    {
        if (_attendanceAdjustmentSelectionObserverAttached) return;

        PropertyChanged += AttendanceAdjustmentShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            if (!CanViewWorkforceAdjustments && IsAttendanceAdjustmentSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền sử dụng Điều chỉnh công.";
        };
        _attendanceAdjustmentSelectionObserverAttached = true;
    }

    private void AttendanceAdjustmentShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsAttendanceAdjustmentSelected));
    }
}
