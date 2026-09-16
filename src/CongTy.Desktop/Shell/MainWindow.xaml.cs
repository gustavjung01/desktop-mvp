using System.IO;
using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Accounting;
using CongTy.Desktop.Dashboard;
using CongTy.Desktop.DocumentNumbering;
using CongTy.Desktop.Inventory;
using CongTy.Desktop.Logistics;
using CongTy.Desktop.Organization;
using CongTy.Desktop.Partners;
using CongTy.Desktop.Products;
using CongTy.Desktop.Pricing;
using CongTy.Desktop.Purchasing;
using CongTy.Desktop.Sales;

namespace CongTy.Desktop.Shell;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private readonly InternalOrganizationView _internalOrganizationView;
    private readonly PartnerView _partnerView;
    private readonly ProductView _productView;
    private readonly TransferView _transferView;
    private readonly StocktakeView _stocktakeView;
    private readonly InventoryAdjustmentView _adjustmentView;
    private readonly ManualInboundView _manualInboundView;
    private readonly InventoryCostingView _inventoryCostingView;
    private readonly InventoryLookupView _inventoryLookupView;
    private readonly InventoryTrackingPolicyView _inventoryTrackingPolicyView;
    private readonly InventoryLotsView _inventoryLotsView;
    private readonly OpeningBalanceView _openingBalanceView;
    private readonly LogisticsReportingView _logisticsReportingView;
    private readonly DeliveryOrderView _deliveryOrderView;
    private readonly TripPlanningView _tripPlanningView;
    private readonly TripDispatchView _tripDispatchView;
    private readonly DeliveryAttemptView _deliveryAttemptView;
    private readonly TripReconciliationView _tripReconciliationView;
    private readonly CustomerReturnView _customerReturnView;
    private readonly SupplierReturnView _supplierReturnView;
    private readonly SalesReportingView _salesReportingView;

    public MainWindow(
        ShellViewModel viewModel,
        DashboardView dashboardView,
        InternalOrganizationView internalOrganizationView,
        PartnerView partnerView,
        ProductView productView,
        PricingView pricingView,
        DocumentNumberingView documentNumberingView,
        SalesView salesView,
        SalesReportingView salesReportingView,
        InventoryView inventoryView,
        FulfillmentView fulfillmentView,
        TransferView transferView,
        StocktakeView stocktakeView,
        InventoryAdjustmentView adjustmentView,
        ManualInboundView manualInboundView,
        InventoryCostingView inventoryCostingView,
        InventoryLookupView inventoryLookupView,
        InventoryTrackingPolicyView inventoryTrackingPolicyView,
        InventoryLotsView inventoryLotsView,
        OpeningBalanceView openingBalanceView,
        LogisticsReportingView logisticsReportingView,
        DeliveryOrderView deliveryOrderView,
        TripPlanningView tripPlanningView,
        TripDispatchView tripDispatchView,
        DeliveryAttemptView deliveryAttemptView,
        TripReconciliationView tripReconciliationView,
        CustomerReturnView customerReturnView,
        AgingReportingView agingReportingView,
        PurchasingReportingView purchasingReportingView,
        PurchaseOrderView purchaseOrderView,
        PurchasePriceView purchasePriceView,
        GoodsReceiptView goodsReceiptView,
        SupplierReturnView supplierReturnView)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _internalOrganizationView = internalOrganizationView;
        _partnerView = partnerView;
        _productView = productView;
        _transferView = transferView;
        _stocktakeView = stocktakeView;
        _adjustmentView = adjustmentView;
        _manualInboundView = manualInboundView;
        _inventoryCostingView = inventoryCostingView;
        _inventoryLookupView = inventoryLookupView;
        _inventoryTrackingPolicyView = inventoryTrackingPolicyView;
        _inventoryLotsView = inventoryLotsView;
        _openingBalanceView = openingBalanceView;
        _logisticsReportingView = logisticsReportingView;
        _deliveryOrderView = deliveryOrderView;
        _tripPlanningView = tripPlanningView;
        _tripDispatchView = tripDispatchView;
        _deliveryAttemptView = deliveryAttemptView;
        _tripReconciliationView = tripReconciliationView;
        _customerReturnView = customerReturnView;
        _supplierReturnView = supplierReturnView;
        _salesReportingView = salesReportingView;
        DataContext = viewModel;
        HomeHost.Content = dashboardView;
        dashboardView.NavigationRequested += DashboardView_OnNavigationRequested;
        InternalOrganizationHost.Content = internalOrganizationView;
        internalOrganizationView.NavigationRequested += InternalOrganizationView_OnNavigationRequested;
        PartnerHost.Content = partnerView;
        ProductHost.Content = productView;
        PricingHost.Content = pricingView;
        DocumentNumberingHost.Content = documentNumberingView;
        SalesHost.Content = salesView;
        SalesReportingHost.Content = salesReportingView;
        InventoryHost.Content = inventoryView;
        FulfillmentHost.Content = fulfillmentView;
        TransferHost.Content = transferView;
        StocktakeHost.Content = stocktakeView;
        AdjustmentHost.Content = adjustmentView;
        ManualInboundHost.Content = manualInboundView;
        InventoryCostingHost.Content = inventoryCostingView;
        InventoryLookupHost.Content = inventoryLookupView;
        InventoryTrackingPolicyHost.Content = inventoryTrackingPolicyView;
        InventoryLotsHost.Content = inventoryLotsView;
        OpeningBalanceHost.Content = openingBalanceView;
        openingBalanceView.BackToLookupRequested += OpeningBalanceView_OnBackToLookupRequested;
        LogisticsReportingHost.Content = logisticsReportingView;
        DeliveryOrderHost.Content = deliveryOrderView;
        TripPlanningHost.Content = tripPlanningView;
        TripDispatchHost.Content = tripDispatchView;
        DeliveryAttemptHost.Content = deliveryAttemptView;
        TripReconciliationHost.Content = tripReconciliationView;
        tripReconciliationView.DeliveryAttemptsRequested += TripReconciliationView_OnDeliveryAttemptsRequested;
        CustomerReturnHost.Content = customerReturnView;
        customerReturnView.TripReconciliationRequested += CustomerReturnView_OnTripReconciliationRequested;
        AgingReportingHost.Content = agingReportingView;
        PurchasingReportingHost.Content = purchasingReportingView;
        PurchaseOrderHost.Content = purchaseOrderView;
        PurchasePriceHost.Content = purchasePriceView;
        GoodsReceiptHost.Content = goodsReceiptView;
        goodsReceiptView.SupplierReturnRequested += GoodsReceiptView_OnSupplierReturnRequested;
        SupplierReturnHost.Content = supplierReturnView;
        Loaded += MainWindow_OnLoaded;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_OnLoaded;

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            var diagnosticPath = WriteStartupDiagnostic(exception);
            MessageBox.Show(
                $"Ứng dụng gặp lỗi khi khởi tạo phiên làm việc nhưng sẽ không tự đóng.\n\nTệp chẩn đoán:\n{diagnosticPath}",
                "Không khởi tạo được ứng dụng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string WriteStartupDiagnostic(Exception exception)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CongTy",
            "Desktop");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "startup-error.txt");
        File.WriteAllText(
            path,
            $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}");
        return path;
    }

    private async void SetupSave_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveInstallationAsync();
    }

    private async void Login_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.SubmitLoginAsync(LoginPassword.Password);
        if (_viewModel.IsWorkspace)
        {
            LoginPassword.Clear();
        }
    }

    private async void RetrySession_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.RetryStoredSessionAsync();
        if (_viewModel.IsWorkspace)
        {
            LoginPassword.Clear();
        }
    }

    private void OpenSetup_OnClick(object sender, RoutedEventArgs e)
    {
        LoginPassword.Clear();
        _viewModel.OpenInstallationSetup();
    }

    private void Home_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NavigateHome();
    }

    private async void DashboardView_OnNavigationRequested(string key)
    {
        switch (key)
        {
            case "catalog.customers":
                _partnerView.SelectNavigationTarget("customers");
                await _viewModel.NavigatePartnersAsync("catalog.customers");
                break;
            case "sales.orders":
                await _viewModel.NavigateSalesAsync();
                break;
            case "inventory.reporting":
                await _viewModel.NavigateInventoryAsync(1, "inventory.reporting");
                break;
            case "inventory.balances":
                await _viewModel.NavigateInventoryLookupAsync();
                break;
            case "inventory.tracking-policies":
                await _viewModel.NavigateInventoryTrackingPolicyAsync();
                break;
            case "inventory.opening-balances":
                await _viewModel.NavigateOpeningBalanceAsync();
                break;
            case "inventory.lots":
                await _viewModel.NavigateInventoryLotsAsync();
                break;
            case "logistics.reporting":
                await _viewModel.NavigateLogisticsReportingAsync();
                break;
            case "logistics.trips":
                await _viewModel.NavigateTripPlanningAsync();
                break;
            case "logistics.delivery-orders":
                await _viewModel.NavigateDeliveryOrdersAsync();
                break;
        }
    }

    private void ToggleSidebar_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleSidebar();
    }

    private void NavGroup_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: string group })
        {
            _viewModel.ToggleNavigationGroup(group);
        }
    }

    private async void CatalogOverview_OnClick(object sender, RoutedEventArgs e)
    {
        _internalOrganizationView.SelectNavigationTarget("overview");
        await _viewModel.NavigateInternalOrganizationAsync("catalog.overview");
    }

    private async void CatalogOverviewRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _internalOrganizationView.RefreshAsync();

    private async void InternalOrganizationView_OnNavigationRequested(string target)
    {
        switch (target)
        {
            case "branches":
                _internalOrganizationView.SelectNavigationTarget("branches");
                await _viewModel.NavigateInternalOrganizationAsync("catalog.branches");
                break;
            case "warehouses":
                _internalOrganizationView.SelectNavigationTarget("warehouses");
                await _viewModel.NavigateInternalOrganizationAsync("catalog.warehouses");
                break;
            case "locations":
                _internalOrganizationView.SelectNavigationTarget("locations");
                await _viewModel.NavigateInternalOrganizationAsync("catalog.warehouses");
                break;
        }
    }

    private async void CatalogBranchesRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _internalOrganizationView.RefreshAsync();

    private void CatalogBranchesAdd_OnClick(object sender, RoutedEventArgs e) =>
        _internalOrganizationView.OpenCreateBranch();

    private async void CatalogBranches_OnClick(object sender, RoutedEventArgs e)
    {
        _internalOrganizationView.SelectNavigationTarget("branches");
        await _viewModel.NavigateInternalOrganizationAsync("catalog.branches");
    }

    private async void CatalogWarehousesRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _internalOrganizationView.RefreshAsync();

    private async void CatalogWarehouses_OnClick(object sender, RoutedEventArgs e)
    {
        _internalOrganizationView.SelectNavigationTarget("warehouses");
        await _viewModel.NavigateInternalOrganizationAsync("catalog.warehouses");
    }

    private async void CatalogCustomersRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _partnerView.RefreshAsync();

    private void CatalogCustomersCreate_OnClick(object sender, RoutedEventArgs e) =>
        _partnerView.OpenCustomerTopbarCreate();

    private void CatalogCustomersList_OnClick(object sender, RoutedEventArgs e) =>
        _partnerView.ReturnToCustomerList();

    private void CatalogCustomersEdit_OnClick(object sender, RoutedEventArgs e) =>
        _partnerView.EditSelectedCustomer();

    private async void CatalogCustomers_OnClick(object sender, RoutedEventArgs e)
    {
        _partnerView.SelectNavigationTarget("customers");
        await _viewModel.NavigatePartnersAsync("catalog.customers");
    }

    private async void CatalogSuppliers_OnClick(object sender, RoutedEventArgs e)
    {
        _partnerView.SelectNavigationTarget("suppliers");
        await _viewModel.NavigatePartnersAsync("catalog.suppliers");
    }

    private async void CatalogProducts_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateProductsAsync();

    private async void CatalogPricing_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigatePricingAsync();

    private async void CatalogDocumentNumbering_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateDocumentNumberingAsync();

    private async void InventoryReporting_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryAsync(1, "inventory.reporting");

    private async void InventoryFulfillment_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateFulfillmentAsync();

    private async void InventoryTransfer_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryTransferAsync();

    private async void InventoryTransferRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _transferView.RefreshAsync();

    private void InventoryTransferCreate_OnClick(object sender, RoutedEventArgs e) =>
        _transferView.OpenCreate();

    private async void InventoryStocktake_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryStocktakeAsync();

    private async void InventoryStocktakeRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _stocktakeView.RefreshAsync();

    private void InventoryStocktakeCreate_OnClick(object sender, RoutedEventArgs e) =>
        _stocktakeView.OpenCreate();

    private async void InventoryAdjustment_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryAdjustmentAsync();

    private async void InventoryAdjustmentRefresh_OnClick(object sender, RoutedEventArgs e) =>
        await _adjustmentView.RefreshAsync();

    private async void ManualInbound_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateManualInboundAsync();

    private async void InventoryCosting_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryCostingAsync();

    private async void InventoryCostingRebuild_OnClick(object sender, RoutedEventArgs e) =>
        await _inventoryCostingView.RebuildAsync();

    private async void InventoryBalances_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryLookupAsync();

    private async void InventoryTrackingPolicy_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryTrackingPolicyAsync();

    private async void InventoryLots_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateInventoryLotsAsync();

    private async void InventoryOpeningBalances_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateOpeningBalanceAsync();

    private async void OpeningBalanceView_OnBackToLookupRequested() =>
        await _viewModel.NavigateInventoryLookupAsync();

    private async void LogisticsReporting_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateLogisticsReportingAsync();

    private async void DeliveryOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateDeliveryOrdersAsync();

    private async void TripPlanning_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateTripPlanningAsync();

    private async void TripDispatch_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateTripDispatchAsync();

    private async void TripDispatchBack_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateTripPlanningAsync();

    private async void DeliveryAttempts_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateDeliveryAttemptsAsync();

    private async void DeliveryAttemptsBack_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateTripDispatchAsync();

    private async void TripReconciliation_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateTripReconciliationAsync();

    private async void TripReconciliationBack_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateDeliveryAttemptsAsync();

    private async void TripReconciliationView_OnDeliveryAttemptsRequested() =>
        await _viewModel.NavigateDeliveryAttemptsAsync();

    private async void CustomerReturns_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateCustomerReturnsAsync();

    private async void CustomerReturnsBack_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateDeliveryOrdersAsync();

    private async void CustomerReturnView_OnTripReconciliationRequested() =>
        await _viewModel.NavigateTripReconciliationAsync();

    private async void AccountingAging_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateAgingAsync();

    private async void PurchasingReporting_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigatePurchasingReportingAsync();

    private async void PurchaseOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigatePurchaseOrdersAsync();

    private async void PurchasePrices_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigatePurchasePricesAsync();

    private async void GoodsReceipts_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateGoodsReceiptsAsync();

    private async void GoodsReceiptView_OnSupplierReturnRequested(string receiptId)
    {
        await _viewModel.NavigateSupplierReturnsAsync();
        if (_viewModel.CanViewSupplierReturns)
        {
            await _supplierReturnView.OpenCreateFromReceiptAsync(receiptId);
        }
    }

    private async void SupplierReturns_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateSupplierReturnsAsync();

    private void Settings_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NavigateSettings();
    }

    private void Access_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NavigateAccess();
    }

    private async void InternalOrganization_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateInternalOrganizationAsync();
    }

    private async void Partners_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigatePartnersAsync();
    }

    private async void SalesReporting_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateSalesReportingAsync();

    private void SalesReportingExport_OnClick(object sender, RoutedEventArgs e) =>
        _salesReportingView.OpenExportDialog();

    private async void SalesReportingOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NavigateSalesAsync();

    private async void Sales_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateSalesAsync();
    }

    private async void Inventory_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateInventoryAsync();
    }

    private async void SystemStatus_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateSystemStatusAsync();
    }

    private async void RefreshConnection_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshConnectionAsync();
    }

    private async void ToggleTheme_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.ToggleThemeAsync();
    }

    private async void Logout_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LogoutAsync();
        LoginPassword.Clear();
    }
}
