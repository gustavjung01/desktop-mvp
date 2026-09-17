using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string ReceivablesReadPermission = "core.receivable.read";
    private bool _receivablesSelectionObserverAttached;

    public bool CanViewReceivables => _access.HasPermission(ReceivablesReadPermission);
    public bool IsReceivablesSelected => SelectedWorkspaceIndex == 39;

    internal void InitializeReceivablesShell() => EnsureReceivablesSelectionObserver();

    public Task NavigateReceivablesAsync()
    {
        EnsureReceivablesSelectionObserver();
        SetSelectedNavigation("accounting.receivables");
        IsAccountingOpen = true;

        if (!CanViewReceivables)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Công nợ phải thu.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 39;
        return Task.CompletedTask;
    }

    private void EnsureReceivablesSelectionObserver()
    {
        if (_receivablesSelectionObserverAttached) return;

        PropertyChanged += ReceivablesShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewReceivables));
            if (!CanViewReceivables && IsReceivablesSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Công nợ phải thu.";
        };
        _receivablesSelectionObserverAttached = true;
    }

    private void ReceivablesShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsReceivablesSelected));
    }
}
