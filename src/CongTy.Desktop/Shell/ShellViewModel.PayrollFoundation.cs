using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _payrollFoundationSelectionObserverAttached;

    public bool CanOpenPayrollFoundationWorkspace => CanViewWorkforcePayroll;
    public bool IsPayrollFoundationSelected => SelectedWorkspaceIndex == WorkspaceSlots.PayrollFoundation;

    internal void InitializePayrollFoundationShell() => EnsurePayrollFoundationSelectionObserver();

    public Task NavigatePayrollFoundationAsync()
    {
        EnsurePayrollFoundationSelectionObserver();
        SetSelectedNavigation("workforce.payroll");
        IsWorkforceOpen = true;

        if (!CanOpenPayrollFoundationWorkspace)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tính lương.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.PayrollFoundation;
        return Task.CompletedTask;
    }

    private void EnsurePayrollFoundationSelectionObserver()
    {
        if (_payrollFoundationSelectionObserverAttached) return;

        PropertyChanged += PayrollFoundationShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanOpenPayrollFoundationWorkspace));
            if (!CanOpenPayrollFoundationWorkspace && IsPayrollFoundationSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tính lương.";
        };
        _payrollFoundationSelectionObserverAttached = true;
    }

    private void PayrollFoundationShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsPayrollFoundationSelected));
    }
}
