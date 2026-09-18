using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private bool _dataExchangeSelectionObserverAttached;

    public bool CanViewDataExchange =>
        (_access.HasPermission("core.product.read") && _access.HasPermission("core.inventory-tracking-policy.read"))
        || (_access.HasPermission("core.product.write") && _access.HasPermission("core.inventory-tracking-policy.manage"))
        || _access.HasPermission("core.price.read")
        || _access.HasPermission("core.price.write")
        || _access.HasPermission("core.stocktake.read")
        || (_access.HasPermission("core.stocktake.create") && _access.HasPermission("core.stocktake.count"))
        || _access.HasPermission("core.inventory.read");

    public bool IsDataExchangeSelected => SelectedWorkspaceIndex == 44;

    internal void InitializeDataExchangeShell() => EnsureDataExchangeSelectionObserver();

    public Task NavigateDataExchangeAsync()
    {
        EnsureDataExchangeSelectionObserver();
        SetSelectedNavigation("operations.data-exchange");

        if (!CanViewDataExchange)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền dùng Nhập/xuất dữ liệu.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 44;
        return Task.CompletedTask;
    }

    private void EnsureDataExchangeSelectionObserver()
    {
        if (_dataExchangeSelectionObserverAttached) return;

        PropertyChanged += DataExchangeShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewDataExchange));
            if (!CanViewDataExchange && IsDataExchangeSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền dùng Nhập/xuất dữ liệu.";
        };
        _dataExchangeSelectionObserverAttached = true;
    }

    private void DataExchangeShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsDataExchangeSelected));
    }
}
