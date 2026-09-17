using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string PayableReadPermission = "core.payable.read";
    private bool _payableSelectionObserverAttached;

    public bool CanViewPayables => _access.HasPermission(PayableReadPermission);
    public bool IsPayablesSelected => SelectedWorkspaceIndex == 42;

    internal void InitializePayablesShell() => EnsurePayableSelectionObserver();

    public Task NavigatePayablesAsync()
    {
        EnsurePayableSelectionObserver();
        SetSelectedNavigation("accounting.payables");
        IsAccountingOpen = true;

        if (!CanViewPayables)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Công nợ phải trả.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 42;
        return Task.CompletedTask;
    }

    private void EnsurePayableSelectionObserver()
    {
        if (_payableSelectionObserverAttached) return;
        PropertyChanged += PayableShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewPayables));
            if (!CanViewPayables && IsPayablesSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Công nợ phải trả.";
        };
        _payableSelectionObserverAttached = true;
    }

    private void PayableShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsPayablesSelected));
    }
}
