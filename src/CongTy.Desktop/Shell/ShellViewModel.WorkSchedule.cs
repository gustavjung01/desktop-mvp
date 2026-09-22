using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string WorkScheduleReadPermission = "core.work-schedule.read";
    private bool _workScheduleSelectionObserverAttached;

    public bool CanReadWorkSchedules => _access.HasPermission(WorkScheduleReadPermission);
    public bool IsWorkScheduleSelected => SelectedWorkspaceIndex == 53;

    internal void InitializeWorkScheduleShell() => EnsureWorkScheduleSelectionObserver();

    public Task NavigateWorkSchedulesAsync()
    {
        EnsureWorkScheduleSelectionObserver();
        SetSelectedNavigation("workforce.schedules");
        IsWorkforceOpen = true;

        if (!CanReadWorkSchedules)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Ca / lịch làm việc.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 53;
        return Task.CompletedTask;
    }

    private void EnsureWorkScheduleSelectionObserver()
    {
        if (_workScheduleSelectionObserverAttached) return;

        PropertyChanged += WorkScheduleShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanReadWorkSchedules));
            if (!CanReadWorkSchedules && IsWorkScheduleSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Ca / lịch làm việc.";
        };
        _workScheduleSelectionObserverAttached = true;
    }

    private void WorkScheduleShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsWorkScheduleSelected));
    }
}
