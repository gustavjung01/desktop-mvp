namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string SalesSettlementReadPermission = "core.receivable.read";

    public bool CanViewSalesSettlement =>
        _access.HasPermission(SalesSettlementReadPermission);

    public bool IsSalesSettlementSelected =>
        _selectedNavigationKey == "accounting.reconciliation";

    public Task NavigateSalesSettlementAsync()
    {
        SetSelectedNavigation("accounting.reconciliation");
        IsAccountingOpen = true;

        if (!CanViewSalesSettlement)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Đối soát bán hàng & COD.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = WorkspaceSlots.SalesSettlement;
        return Task.CompletedTask;
    }
}
