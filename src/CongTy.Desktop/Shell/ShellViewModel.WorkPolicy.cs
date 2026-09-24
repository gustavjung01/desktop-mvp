using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string WorkPolicyReadPermission = "core.work-policy.read";
    private bool _workPolicySelectionObserverAttached;

    public bool CanReadWorkPolicies => _access.HasPermission(WorkPolicyReadPermission);
    public bool IsWorkPolicySelected => SelectedWorkspaceIndex == WorkspaceSlots.WorkPolicy;

    internal void InitializeWorkPolicyShell() => EnsureWorkPolicySelectionObserver();

    public Task NavigateWorkPoliciesAsync()
    {
        EnsureWorkPolicySelectionObserver();
        SetSelectedNavigation("workforce.policies");
        IsWorkforceOpen = true;

        if (!CanReadWorkPolicies)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Chính sách làm việc.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.WorkPolicy;
        return Task.CompletedTask;
    }

    private void EnsureWorkPolicySelectionObserver()
    {
        if (_workPolicySelectionObserverAttached) return;

        PropertyChanged += WorkPolicyShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanReadWorkPolicies));
            if (!CanReadWorkPolicies && IsWorkPolicySelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Chính sách làm việc.";
        };
        _workPolicySelectionObserverAttached = true;
    }

    private void WorkPolicyShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsWorkPolicySelected));
    }
}
