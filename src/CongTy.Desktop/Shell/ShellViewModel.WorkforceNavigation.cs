namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _isWorkforceOpen;
    private bool _workforceNavigationObserverAttached;

    public bool IsWorkforceOpen
    {
        get => _isWorkforceOpen;
        private set
        {
            if (SetField(ref _isWorkforceOpen, value))
                OnPropertyChanged(nameof(WorkforceChevron));
        }
    }

    public string WorkforceChevron => IsWorkforceOpen ? "⌄" : "›";

    public bool CanViewWorkforceAttendance =>
        _access.HasPermission("core.attendance.self.read")
        || _access.HasPermission("core.attendance.self.record")
        || _access.HasPermission("core.attendance.read");

    public bool CanViewWorkforceTimesheet =>
        _access.HasPermission("core.attendance.self.read")
        || _access.HasPermission("core.attendance.read")
        || _access.HasPermission("core.attendance.reconcile")
        || _access.HasPermission("core.attendance.lock");

    public bool CanViewWorkforceOvertime =>
        _access.HasPermission("core.overtime.self-request")
        || _access.HasPermission("core.overtime.read")
        || _access.HasPermission("core.overtime.approve")
        || _access.HasPermission("core.overtime.confirm")
        || _access.HasPermission("core.attendance.self.read")
        || _access.HasPermission("core.attendance.read")
        || _access.HasPermission("core.attendance.reconcile")
        || _access.HasPermission("core.attendance.lock");

    public bool CanViewWorkforcePayroll =>
        _access.HasPermission("core.payroll.read")
        || _access.HasPermission("core.payroll.manage")
        || _access.HasPermission("core.payroll.close")
        || _access.HasPermission("core.payroll.adjust")
        || _access.HasPermission("core.payroll.export");

    public bool CanViewWorkforceLeave =>
        _access.HasPermission("core.leave.self.read")
        || _access.HasPermission("core.leave.self.request")
        || _access.HasPermission("core.leave.read")
        || _access.HasPermission("core.leave.approve")
        || _access.HasPermission("core.leave-type.manage");

    public bool CanViewWorkforceViolations =>
        _access.HasPermission("core.attendance-violation.self-explain")
        || _access.HasPermission("core.attendance-violation.resolve");

    public bool CanViewWorkforceAdjustments =>
        _access.HasPermission("core.attendance.self-adjust-request")
        || _access.HasPermission("core.attendance.adjust");

    public bool CanViewWorkforceSchedules =>
        _access.HasPermission("core.work-schedule.read")
        || _access.HasPermission("core.work-schedule.manage");

    public bool CanViewWorkforcePolicies =>
        _access.HasPermission("core.work-policy.read");

    public bool CanViewWorkforce =>
        CanViewWorkforceAttendance
        || CanViewWorkforceTimesheet
        || CanViewWorkforceOvertime
        || CanViewWorkforcePayroll
        || CanViewWorkforceLeave
        || CanViewWorkforceViolations
        || CanViewWorkforceAdjustments
        || CanViewEmployeeDirectory
        || CanViewWorkforceSchedules
        || CanViewWorkforcePolicies;

    internal void InitializeWorkforceNavigationShell()
    {
        if (_workforceNavigationObserverAttached) return;
        _access.Changed += (_, _) => RaiseWorkforceNavigationAccess();
        _workforceNavigationObserverAttached = true;
        RaiseWorkforceNavigationAccess();
    }

    private void RaiseWorkforceNavigationAccess()
    {
        OnPropertyChanged(nameof(CanViewWorkforceAttendance));
        OnPropertyChanged(nameof(CanViewWorkforceTimesheet));
        OnPropertyChanged(nameof(CanViewWorkforceOvertime));
        OnPropertyChanged(nameof(CanViewWorkforcePayroll));
        OnPropertyChanged(nameof(CanViewWorkforceLeave));
        OnPropertyChanged(nameof(CanViewWorkforceViolations));
        OnPropertyChanged(nameof(CanViewWorkforceAdjustments));
        OnPropertyChanged(nameof(CanViewWorkforceSchedules));
        OnPropertyChanged(nameof(CanViewWorkforcePolicies));
        OnPropertyChanged(nameof(CanViewWorkforce));
    }
}
