using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
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
using CongTy.Desktop.Themes;
using CongTy.Windows;

namespace CongTy.Desktop.Shell;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private enum ShellStage
    {
        Setup,
        Login,
        Workspace
    }

    private readonly IConnectionStateService _connection;
    private readonly IInstallationProfileService _installation;
    private readonly IAuthenticationService _authentication;
    private readonly IAccessStateService _access;
    private readonly DashboardViewModel _dashboard;
    private readonly InternalOrganizationViewModel _internalOrganization;
    private readonly PartnerViewModel _partners;
    private readonly ProductViewModel _products;
    private readonly PricingViewModel _pricing;
    private readonly DocumentNumberingViewModel _documentNumbering;
    private readonly SalesViewModel _sales;
    private readonly SalesReportingViewModel _salesReporting;
    private readonly GrossMarginReportingViewModel _grossMarginReporting;
    private readonly InventoryViewModel _inventory;
    private readonly FulfillmentViewModel _fulfillment;
    private readonly TransferViewModel _transfer;
    private readonly StocktakeViewModel _stocktake;
    private readonly InventoryAdjustmentViewModel _adjustment;
    private readonly ManualInboundViewModel _manualInbound;
    private readonly InventoryCostingViewModel _inventoryCosting;
    private readonly InventoryLookupViewModel _inventoryLookup;
    private readonly InventoryTrackingPolicyViewModel _inventoryTrackingPolicy;
    private readonly InventoryLotsViewModel _inventoryLots;
    private readonly OpeningBalanceViewModel _openingBalance;
    private readonly LogisticsReportingViewModel _logisticsReporting;
    private readonly DeliveryOrderViewModel _deliveryOrders;
    private readonly TripPlanningViewModel _tripPlanning;
    private readonly TripDispatchViewModel _tripDispatch;
    private readonly DeliveryAttemptViewModel _deliveryAttempts;
    private readonly TripReconciliationViewModel _tripReconciliation;
    private readonly CustomerReturnViewModel _customerReturns;
    private readonly AgingReportingViewModel _agingReporting;
    private readonly CodAccountingViewModel _codAccounting;
    private readonly PurchasingReportingViewModel _purchasingReporting;
    private readonly PurchaseOrderViewModel _purchaseOrders;
    private readonly PurchasePriceViewModel _purchasePrices;
    private readonly GoodsReceiptViewModel _goodsReceipts;
    private readonly SupplierReturnViewModel _supplierReturns;
    private readonly ILocalSettingsStore _settingsStore;
    private readonly DesktopSettingsState _settingsState;

    private ShellStage _stage;
    private string _connectionText;
    private bool _isDark;
    private bool _isBusy;
    private int _selectedWorkspaceIndex;
    private string _setupInstallationName;
    private string _setupCompanyName;
    private string _setupApiUrl;
    private string _setupMessage = string.Empty;
    private string _loginName = string.Empty;
    private string _ownerCode = string.Empty;
    private string _loginMessage = string.Empty;
    private bool _isOwnerCodeRequired;
    private string _workspaceMessage = string.Empty;
    private bool _isSidebarExpanded = true;
    private int _selectedSettingsIndex;
    private string _selectedNavigationKey = "dashboard";
    private bool _isCatalogOpen;
    private bool _isInventoryOpen;
    private bool _isLogisticsOpen;
    private bool _isSalesOpen;
    private bool _isPurchasingOpen;
    private bool _isAccountingOpen;
    private bool _isCompanySettingsOpen;
    private bool _isAccessOpen;

    public ShellViewModel(
        IConnectionStateService connection,
        IInstallationProfileService installation,
        IAuthenticationService authentication,
        IAccessStateService access,
        DashboardViewModel dashboard,
        InternalOrganizationViewModel internalOrganization,
        PartnerViewModel partners,
        ProductViewModel products,
        PricingViewModel pricing,
        DocumentNumberingViewModel documentNumbering,
        SalesViewModel sales,
        SalesReportingViewModel salesReporting,
        GrossMarginReportingViewModel grossMarginReporting,
        InventoryViewModel inventory,
        FulfillmentViewModel fulfillment,
        TransferViewModel transfer,
        StocktakeViewModel stocktake,
        InventoryAdjustmentViewModel adjustment,
        ManualInboundViewModel manualInbound,
        InventoryCostingViewModel inventoryCosting,
        InventoryLookupViewModel inventoryLookup,
        InventoryTrackingPolicyViewModel inventoryTrackingPolicy,
        InventoryLotsViewModel inventoryLots,
        OpeningBalanceViewModel openingBalance,
        LogisticsReportingViewModel logisticsReporting,
        DeliveryOrderViewModel deliveryOrders,
        TripPlanningViewModel tripPlanning,
        TripDispatchViewModel tripDispatch,
        DeliveryAttemptViewModel deliveryAttempts,
        TripReconciliationViewModel tripReconciliation,
        CustomerReturnViewModel customerReturns,
        AgingReportingViewModel agingReporting,
        CodAccountingViewModel codAccounting,
        PurchasingReportingViewModel purchasingReporting,
        PurchaseOrderViewModel purchaseOrders,
        PurchasePriceViewModel purchasePrices,
        GoodsReceiptViewModel goodsReceipts,
        SupplierReturnViewModel supplierReturns,
        ILocalSettingsStore settingsStore,
        DesktopSettingsState settingsState)
    {
        _connection = connection;
        _installation = installation;
        _authentication = authentication;
        _access = access;
        _dashboard = dashboard;
        _internalOrganization = internalOrganization;
        _partners = partners;
        _products = products;
        _pricing = pricing;
        _documentNumbering = documentNumbering;
        _sales = sales;
        _salesReporting = salesReporting;
        _grossMarginReporting = grossMarginReporting;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _transfer = transfer;
        _stocktake = stocktake;
        _adjustment = adjustment;
        _manualInbound = manualInbound;
        _inventoryCosting = inventoryCosting;
        _inventoryLookup = inventoryLookup;
        _inventoryTrackingPolicy = inventoryTrackingPolicy;
        _inventoryLots = inventoryLots;
        _openingBalance = openingBalance;
        _logisticsReporting = logisticsReporting;
        _deliveryOrders = deliveryOrders;
        _tripPlanning = tripPlanning;
        _tripDispatch = tripDispatch;
        _deliveryAttempts = deliveryAttempts;
        _tripReconciliation = tripReconciliation;
        _customerReturns = customerReturns;
        _agingReporting = agingReporting;
        _codAccounting = codAccounting;
        _purchasingReporting = purchasingReporting;
        _purchaseOrders = purchaseOrders;
        _purchasePrices = purchasePrices;
        _goodsReceipts = goodsReceipts;
        _supplierReturns = supplierReturns;
        _settingsStore = settingsStore;
        _settingsState = settingsState;

        var settings = settingsState.Current;
        _setupInstallationName = settings.InstallationName;
        _setupCompanyName = settings.CompanyDisplayName;
        _setupApiUrl = settings.ApiBaseUrl;
        _connectionText = connection.Current.Message;
        _isDark = ThemeManager.Normalize(settings.Theme) == "Dark";
        _stage = installation.IsConfigured ? ShellStage.Login : ShellStage.Setup;

        _connection.Changed += (_, snapshot) => ConnectionText = snapshot.Message;
        _settingsState.Changed += (_, _) => RaiseSettingsChanged();
        _dashboard.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 0 && (args.PropertyName == nameof(DashboardViewModel.Message) || args.PropertyName == nameof(DashboardViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
        };
        _internalOrganization.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 1 && (args.PropertyName == nameof(InternalOrganizationViewModel.Message) || args.PropertyName == nameof(InternalOrganizationViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
        };
        _partners.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 2 && (args.PropertyName == nameof(PartnerViewModel.Message) || args.PropertyName == nameof(PartnerViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(PartnerViewModel.CustomerWorkspaceTabIndex)
                or nameof(PartnerViewModel.IsCustomerProfileOpen)
                or nameof(PartnerViewModel.CanShowCustomerTopbarCreate)
                or nameof(PartnerViewModel.CustomerTopbarCreateText))
            {
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
                OnPropertyChanged(nameof(HeaderKicker));
                OnPropertyChanged(nameof(IsCustomerProfileOpen));
                OnPropertyChanged(nameof(IsCustomerCatalogWorkspace));
                OnPropertyChanged(nameof(CanShowCustomerTopbarCreate));
                OnPropertyChanged(nameof(CustomerTopbarCreateText));
            }
        };
        _products.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 11
                && (args.PropertyName == nameof(ProductViewModel.Message)
                    || args.PropertyName == nameof(ProductViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
        };
        _pricing.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 15
                && (args.PropertyName == nameof(PricingViewModel.Message)
                    || args.PropertyName == nameof(PricingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(PricingViewModel.CanRead)
                or nameof(PricingViewModel.CanWrite))
            {
                OnPropertyChanged(nameof(CanViewPricing));
            }
        };
        _sales.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 3 && (args.PropertyName == nameof(SalesViewModel.Message) || args.PropertyName == nameof(SalesViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
        };
        _salesReporting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 32
                && (args.PropertyName == nameof(SalesReportingViewModel.Message)
                    || args.PropertyName == nameof(SalesReportingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
            if (args.PropertyName is nameof(SalesReportingViewModel.CanRead)
                or nameof(SalesReportingViewModel.CanExport)
                or nameof(SalesReportingViewModel.CanOpenExport))
            {
                OnPropertyChanged(nameof(CanViewSalesReporting));
                OnPropertyChanged(nameof(CanExportSalesReporting));
            }
        };
        _grossMarginReporting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 33
                && (args.PropertyName == nameof(GrossMarginReportingViewModel.Message)
                    || args.PropertyName == nameof(GrossMarginReportingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
            if (args.PropertyName is nameof(GrossMarginReportingViewModel.CanRead)
                or nameof(GrossMarginReportingViewModel.CanExport)
                or nameof(GrossMarginReportingViewModel.CanOpenExport))
            {
                OnPropertyChanged(nameof(CanViewGrossMargin));
                OnPropertyChanged(nameof(CanExportGrossMargin));
            }
        };
        _inventory.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 4 && args.PropertyName == nameof(InventoryViewModel.Message))
            {
                RaiseActiveNotice();
            }
        };
        _inventory.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(InventoryViewModel.MainTabIndex) && IsInventorySelected)
            {
                SetSelectedNavigation(_inventory.MainTabIndex switch
                {
                    1 => "inventory.reporting",
                    2 => "inventory.lots",
                    _ => "inventory.balances"
                });
            }
        };
        _fulfillment.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 6
                && (args.PropertyName == nameof(FulfillmentViewModel.Message)
                    || args.PropertyName == nameof(FulfillmentViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
        };
        _transfer.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 7
                && (args.PropertyName == nameof(TransferViewModel.Message)
                    || args.PropertyName == nameof(TransferViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }
            if (args.PropertyName is nameof(TransferViewModel.CanCreate)
                or nameof(TransferViewModel.IsNotBusy))
            {
                OnPropertyChanged(nameof(CanCreateTransferTopbar));
                OnPropertyChanged(nameof(CanUseTransferTopbarActions));
            }
        };
        _stocktake.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 8
                && (args.PropertyName == nameof(StocktakeViewModel.Message)
                    || args.PropertyName == nameof(StocktakeViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(StocktakeViewModel.CanCreate)
                or nameof(StocktakeViewModel.IsNotBusy))
            {
                OnPropertyChanged(nameof(CanCreateStocktakeTopbar));
                OnPropertyChanged(nameof(CanUseStocktakeTopbarActions));
            }
        };
        _adjustment.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 9
                && (args.PropertyName == nameof(InventoryAdjustmentViewModel.Message)
                    || args.PropertyName == nameof(InventoryAdjustmentViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(InventoryAdjustmentViewModel.IsNotBusy)
                or nameof(InventoryAdjustmentViewModel.ActiveTab))
            {
                OnPropertyChanged(nameof(CanUseAdjustmentTopbarActions));
                OnPropertyChanged(nameof(IsAdjustmentDocumentsTab));
            }
        };
        _manualInbound.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 10
                && (args.PropertyName == nameof(ManualInboundViewModel.Message)
                    || args.PropertyName == nameof(ManualInboundViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(ManualInboundViewModel.CanOpen))
            {
                OnPropertyChanged(nameof(CanViewManualInbound));
            }
        };
        _inventoryCosting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 12
                && (args.PropertyName == nameof(InventoryCostingViewModel.Message)
                    || args.PropertyName == nameof(InventoryCostingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(InventoryCostingViewModel.CanOpen)
                or nameof(InventoryCostingViewModel.CanRunRebuild)
                or nameof(InventoryCostingViewModel.IsBusy))
            {
                OnPropertyChanged(nameof(CanViewInventoryCosting));
                OnPropertyChanged(nameof(CanRunCostingRebuild));
                OnPropertyChanged(nameof(CostingRebuildActionText));
            }
        };
        _inventoryLookup.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 13
                && (args.PropertyName == nameof(InventoryLookupViewModel.Message)
                    || args.PropertyName == nameof(InventoryLookupViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(InventoryLookupViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewInventoryBalances));
            }
        };
        _inventoryTrackingPolicy.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 14
                && (args.PropertyName == nameof(InventoryTrackingPolicyViewModel.Message)
                    || args.PropertyName == nameof(InventoryTrackingPolicyViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(InventoryTrackingPolicyViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewInventoryTrackingPolicy));
            }
        };
        _inventoryLots.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 16
                && (args.PropertyName == nameof(InventoryLotsViewModel.Message)
                    || args.PropertyName == nameof(InventoryLotsViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(InventoryLotsViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewInventoryLots));
            }
        };
        _openingBalance.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 18
                && (args.PropertyName == nameof(OpeningBalanceViewModel.Message)
                    || args.PropertyName == nameof(OpeningBalanceViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(OpeningBalanceViewModel.CanImport)
                or nameof(OpeningBalanceViewModel.CanReadHistory))
            {
                OnPropertyChanged(nameof(CanViewOpeningBalance));
            }
        };
        _logisticsReporting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 19
                && (args.PropertyName == nameof(LogisticsReportingViewModel.Message)
                    || args.PropertyName == nameof(LogisticsReportingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(LogisticsReportingViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewLogisticsReporting));
            }
        };
        _deliveryOrders.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 21
                && (args.PropertyName == nameof(DeliveryOrderViewModel.Message)
                    || args.PropertyName == nameof(DeliveryOrderViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(DeliveryOrderViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewDeliveryOrders));
            }
        };
        _tripPlanning.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 23
                && (args.PropertyName == nameof(TripPlanningViewModel.Message)
                    || args.PropertyName == nameof(TripPlanningViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(TripPlanningViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewTripPlanning));
            }
        };
        _tripDispatch.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 25
                && (args.PropertyName == nameof(TripDispatchViewModel.Message)
                    || args.PropertyName == nameof(TripDispatchViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(TripDispatchViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewTripDispatch));
            }
        };
        _deliveryAttempts.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 27
                && (args.PropertyName == nameof(DeliveryAttemptViewModel.Message)
                    || args.PropertyName == nameof(DeliveryAttemptViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(DeliveryAttemptViewModel.CanRead)
                or nameof(DeliveryAttemptViewModel.CanReadProofs))
            {
                OnPropertyChanged(nameof(CanViewDeliveryAttempts));
            }
        };
        _tripReconciliation.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 29
                && (args.PropertyName == nameof(TripReconciliationViewModel.Message)
                    || args.PropertyName == nameof(TripReconciliationViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(TripReconciliationViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewTripReconciliation));
            }
        };
        _customerReturns.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 30
                && (args.PropertyName == nameof(CustomerReturnViewModel.Message)
                    || args.PropertyName == nameof(CustomerReturnViewModel.MessageIsError)
                    || args.PropertyName == nameof(CustomerReturnViewModel.RequiresTripReconciliation)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(CustomerReturnViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewCustomerReturns));
            }
        };
        _agingReporting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 31
                && (args.PropertyName == nameof(AgingReportingViewModel.Message)
                    || args.PropertyName == nameof(AgingReportingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(AgingReportingViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewAging));
            }
        };
        _codAccounting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 34
                && (args.PropertyName == nameof(CodAccountingViewModel.Message)
                    || args.PropertyName == nameof(CodAccountingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(CodAccountingViewModel.CanReadReport))
            {
                OnPropertyChanged(nameof(CanViewCodAccounting));
            }
        };
        _purchasingReporting.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 20
                && (args.PropertyName == nameof(PurchasingReportingViewModel.Message)
                    || args.PropertyName == nameof(PurchasingReportingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(PurchasingReportingViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewPurchasingReporting));
            }
        };
        _purchaseOrders.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 22
                && (args.PropertyName == nameof(PurchaseOrderViewModel.Message)
                    || args.PropertyName == nameof(PurchaseOrderViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(PurchaseOrderViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewPurchaseOrders));
            }
        };
        _purchasePrices.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 24
                && (args.PropertyName == nameof(PurchasePriceViewModel.Message)
                    || args.PropertyName == nameof(PurchasePriceViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(PurchasePriceViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewPurchasePrices));
            }
        };
        _goodsReceipts.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 26
                && (args.PropertyName == nameof(GoodsReceiptViewModel.Message)
                    || args.PropertyName == nameof(GoodsReceiptViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(GoodsReceiptViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewGoodsReceipts));
            }
        };
        _supplierReturns.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 28
                && (args.PropertyName == nameof(SupplierReturnViewModel.Message)
                    || args.PropertyName == nameof(SupplierReturnViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName == nameof(SupplierReturnViewModel.CanRead))
            {
                OnPropertyChanged(nameof(CanViewSupplierReturns));
            }
        };
        _documentNumbering.PropertyChanged += (_, args) =>
        {
            if (SelectedWorkspaceIndex == 17
                && (args.PropertyName == nameof(DocumentNumberingViewModel.Message)
                    || args.PropertyName == nameof(DocumentNumberingViewModel.MessageIsError)))
            {
                RaiseActiveNotice();
            }

            if (args.PropertyName is nameof(DocumentNumberingViewModel.CanRead)
                or nameof(DocumentNumberingViewModel.CanWrite))
            {
                OnPropertyChanged(nameof(CanViewDocumentNumbering));
            }
        };
        _access.Changed += (_, snapshot) =>
        {
            RaiseAccessChanged();
            if (_stage == ShellStage.Workspace && !snapshot.IsAuthenticated)
            {
                LoginMessage = "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.";
                SelectedWorkspaceIndex = 0;
                SetStage(ShellStage.Login);
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSetup => _stage == ShellStage.Setup;
    public bool IsLogin => _stage == ShellStage.Login;
    public bool IsWorkspace => _stage == ShellStage.Workspace;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string CompanyName =>
        string.IsNullOrWhiteSpace(_settingsState.Current.CompanyDisplayName)
            ? "Công Ty"
            : _settingsState.Current.CompanyDisplayName;

    public string InstallationName => _settingsState.Current.InstallationName;

    public string ConnectionText
    {
        get => _connectionText;
        private set => SetField(ref _connectionText, value);
    }

    public string SetupInstallationName
    {
        get => _setupInstallationName;
        set => SetField(ref _setupInstallationName, value);
    }

    public string SetupCompanyName
    {
        get => _setupCompanyName;
        set => SetField(ref _setupCompanyName, value);
    }

    public string SetupApiUrl
    {
        get => _setupApiUrl;
        set => SetField(ref _setupApiUrl, value);
    }

    public string SetupMessage
    {
        get => _setupMessage;
        private set => SetField(ref _setupMessage, value);
    }

    public string LoginName
    {
        get => _loginName;
        set => SetField(ref _loginName, value);
    }

    public string OwnerCode
    {
        get => _ownerCode;
        set => SetField(ref _ownerCode, value);
    }

    public string LoginMessage
    {
        get => _loginMessage;
        private set => SetField(ref _loginMessage, value);
    }

    public bool IsOwnerCodeRequired
    {
        get => _isOwnerCodeRequired;
        private set => SetField(ref _isOwnerCodeRequired, value);
    }

    public string WorkspaceMessage
    {
        get => _workspaceMessage;
        private set
        {
            if (SetField(ref _workspaceMessage, value))
            {
                RaiseActiveNotice();
            }
        }
    }

    public string ActiveNotice =>
        !string.IsNullOrWhiteSpace(WorkspaceMessage)
            ? WorkspaceMessage
            : SelectedWorkspaceIndex switch
            {
                0 => _dashboard.Message,
                1 => _internalOrganization.Message,
                2 => _partners.Message,
                3 => _sales.Message,
                4 => _inventory.Message,
                6 => _fulfillment.Message,
                7 => _transfer.Message,
                8 => _stocktake.Message,
                9 => _adjustment.Message,
                10 => _manualInbound.Message,
                11 => _products.Message,
                12 => _inventoryCosting.Message,
                13 => _inventoryLookup.Message,
                14 => _inventoryTrackingPolicy.Message,
                15 => _pricing.Message,
                16 => _inventoryLots.Message,
                17 => _documentNumbering.Message,
                18 => _openingBalance.Message,
                19 => _logisticsReporting.Message,
                20 => _purchasingReporting.Message,
                21 => _deliveryOrders.Message,
                22 => _purchaseOrders.Message,
                23 => _tripPlanning.Message,
                24 => _purchasePrices.Message,
                25 => _tripDispatch.Message,
                26 => _goodsReceipts.Message,
                27 => _deliveryAttempts.Message,
                28 => _supplierReturns.Message,
                29 => _tripReconciliation.Message,
                30 => _customerReturns.Message,
                31 => _agingReporting.Message,
                32 => _salesReporting.Message,
                33 => _grossMarginReporting.Message,
                34 => _codAccounting.Message,
                _ => string.Empty
            };

    public bool ActiveNoticeIsError =>
        string.IsNullOrWhiteSpace(WorkspaceMessage)
        && (SelectedWorkspaceIndex switch
        {
            0 => _dashboard.MessageIsError,
            1 => _internalOrganization.MessageIsError,
            2 => _partners.MessageIsError,
            3 => _sales.MessageIsError,
            6 => _fulfillment.MessageIsError,
            7 => _transfer.MessageIsError,
            8 => _stocktake.MessageIsError,
            9 => _adjustment.MessageIsError,
            10 => _manualInbound.MessageIsError,
            11 => _products.MessageIsError,
            12 => _inventoryCosting.MessageIsError,
            13 => _inventoryLookup.MessageIsError,
            14 => _inventoryTrackingPolicy.MessageIsError,
            15 => _pricing.MessageIsError,
            16 => _inventoryLots.MessageIsError,
            17 => _documentNumbering.MessageIsError,
            18 => _openingBalance.MessageIsError,
            19 => _logisticsReporting.MessageIsError,
            20 => _purchasingReporting.MessageIsError,
            21 => _deliveryOrders.MessageIsError,
            22 => _purchaseOrders.MessageIsError,
            23 => _tripPlanning.MessageIsError,
            24 => _purchasePrices.MessageIsError,
            25 => _tripDispatch.MessageIsError,
            26 => _goodsReceipts.MessageIsError,
            27 => _deliveryAttempts.MessageIsError,
            28 => _supplierReturns.MessageIsError,
            29 => _tripReconciliation.MessageIsError,
            30 => _customerReturns.MessageIsError,
            31 => _agingReporting.MessageIsError,
            32 => _salesReporting.MessageIsError,
            33 => _grossMarginReporting.MessageIsError,
            34 => _codAccounting.MessageIsError,
            _ => false
        });

    public bool HasActiveNotice => !string.IsNullOrWhiteSpace(ActiveNotice);

    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        private set
        {
            if (SetField(ref _isSidebarExpanded, value))
            {
                OnPropertyChanged(nameof(SidebarWidth));
            }
        }
    }

    public GridLength SidebarWidth => new(IsSidebarExpanded ? 280 : 84);

    public int SelectedSettingsIndex
    {
        get => _selectedSettingsIndex;
        private set => SetField(ref _selectedSettingsIndex, Math.Clamp(value, 0, 2));
    }

    public bool IsDark
    {
        get => _isDark;
        private set
        {
            if (SetField(ref _isDark, value))
            {
                OnPropertyChanged(nameof(ThemeText));
                OnPropertyChanged(nameof(ThemeActionText));
            }
        }
    }

    public string ThemeText => IsDark ? "Tối" : "Sáng";

    public string ThemeActionText => IsDark
        ? "Chuyển sang giao diện sáng"
        : "Chuyển sang giao diện tối";

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        private set
        {
            if (_selectedWorkspaceIndex == value)
            {
                return;
            }

            _selectedWorkspaceIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsInternalOrganizationSelected));
            OnPropertyChanged(nameof(IsPartnersSelected));
            OnPropertyChanged(nameof(IsSalesSelected));
            OnPropertyChanged(nameof(IsInventorySelected));
            OnPropertyChanged(nameof(IsSettingsSelected));
            RaiseActiveNotice();
            OnPropertyChanged(nameof(IsCatalogGroupSelected));
            OnPropertyChanged(nameof(IsInventoryGroupSelected));
            OnPropertyChanged(nameof(IsLogisticsGroupSelected));
            OnPropertyChanged(nameof(IsSalesGroupSelected));
            OnPropertyChanged(nameof(IsPurchasingGroupSelected));
        }
    }

    public string PageTitle => _selectedNavigationKey switch
    {
        "catalog.overview" => "Tổ chức",
        "catalog.branches" => "Chi nhánh",
        "catalog.warehouses" => "Kho hàng",
        "catalog.locations" => "Vị trí kho",
        "catalog.customers" => _partners.IsCustomerProfileOpen ? "Chi tiết khách hàng" : "Khách hàng",
        "catalog.suppliers" => "Nhà cung cấp",
        "catalog.products" => "Danh mục sản phẩm",
        "catalog.pricing" => "Giá bán và khuyến mãi",
        "catalog.document-numbering" => "Số chứng từ",
        "sales.reporting" => "Báo cáo bán hàng",
        "sales.gross-margin" => "Lãi gộp",
        "sales.orders" => "Đơn bán hàng",
        "inventory.reporting" => "Báo cáo tồn kho",
        "inventory.fulfillment" => "Chuẩn bị hàng",
        "inventory.transfer" => "Chuyển kho",
        "inventory.stocktake" => "Kiểm kê kho",
        "inventory.adjustments" => "Điều chỉnh tồn",
        "inventory.manual-inbound" => "Nhập kho thủ công",
        "inventory.costing" => "Giá vốn tồn kho",
        "inventory.lots" => "Lô hàng",
        "inventory.balances" => "Tra cứu tồn kho",
        "inventory.tracking-policies" => "Chính sách quản lý lô",
        "inventory.opening-balances" => "Thiết lập tồn đầu kỳ",
        "logistics.reporting" => "Hiệu suất giao hàng / Logistics",
        "logistics.delivery-orders" => "Bàn giao giao nhận",
        "logistics.trips" => "Điều phối giao hàng",
        "logistics.dispatch" => "Bàn giao và cho xe xuất phát",
        "logistics.delivery-attempts" => "Theo dõi kết quả lần giao",
        "logistics.trip-reconciliation" => "Đối soát cuối chuyến",
        "logistics.customer-returns" => "Hàng khách trả",
        "purchasing.reporting" => "Báo cáo mua hàng",
        "purchasing.purchase-orders" => "Đơn mua hàng",
        "purchasing.purchase-prices" => "Bảng giá mua",
        "purchasing.goods-receipts" => "Phiếu nhận hàng",
        "purchasing.supplier-returns" => "Phiếu trả nhà cung cấp",
        "accounting.aging" => "Tuổi nợ phải thu / phải trả",
        "accounting.cod-reporting" => "COD & đối soát",
        "desktop.settings" => "Cài đặt ứng dụng",
        _ => "Tổng quan điều hành"
    };

    public string PageSubtitle => _selectedNavigationKey switch
    {
        "catalog.overview" => "Theo dõi cơ cấu chi nhánh, kho hàng và vị trí lưu trữ trong toàn hệ thống.",
        "catalog.branches" => "Quản lý danh mục chi nhánh và thông tin liên hệ phục vụ vận hành, hạch toán và báo cáo.",
        "catalog.warehouses" => "Quản lý kho, thiết lập nhanh và sơ đồ hàng hóa bên trong từng kho.",
        "catalog.locations" => "Danh mục vị trí lưu trữ theo kho và chi nhánh",
        "catalog.customers" => _partners.IsCustomerProfileOpen
            ? "Theo dõi thông tin, giao dịch và tình hình hiện tại của một khách hàng."
            : "Quản lý nhóm, hồ sơ khách hàng, điều khoản thanh toán, hạn mức và địa chỉ giao dịch.",
        "catalog.suppliers" => "Danh mục và hồ sơ nhà cung cấp",
        "catalog.products" => "Quản lý sản phẩm, SKU, loại sản phẩm, nhãn hàng, đơn vị tính, mã vạch và cập nhật theo tệp.",
        "catalog.pricing" => "Quản lý kênh bán, bảng giá, giá theo SKU, khuyến mãi, điều kiện áp dụng và kiểm tra giá cuối cùng.",
        "catalog.document-numbering" => "Thiết lập cách đánh số tự động theo từng loại chứng từ",
        "sales.reporting" => "Theo dõi doanh thu, đơn đã chốt, khách mua và sản lượng theo đúng kỳ và phạm vi kho được cấp.",
        "sales.gross-margin" => "Đối chiếu doanh thu thuần đã ghi nhận với dữ liệu giá vốn theo đúng chứng từ kho; hàng khách trả đã nhận được đảo cả doanh thu và giá vốn hàng bán.",
        "sales.orders" => "Đơn nhiều nguồn, trạng thái xử lý, chuẩn bị hàng, giao hàng và thanh toán",
        "inventory.reporting" => "Tổng quan, tồn hiện tại, luân chuyển, chậm luân chuyển, lô và các điểm cần kiểm tra",
        "inventory.fulfillment" => "Phân bổ số lượng phù hợp cho từng đơn; phần chưa phân bổ vẫn để dành cho quyết định tiếp theo.",
        "inventory.transfer" => "Tạo, duyệt, xuất và nhận hàng giữa các kho; chênh lệch được lưu bằng chứng từ riêng, không sửa số lượng xuất gốc.",
        "inventory.stocktake" => "Đếm thực tế, gửi duyệt và chỉ cập nhật tồn kho sau khi kết quả đã được duyệt.",
        "inventory.adjustments" => "Quản lý phiếu điều chỉnh, lập điều chỉnh thủ công hoặc nhập hàng loạt trong cùng một nơi. Tồn kho chỉ thay đổi ở bước Cập nhật tồn kho.",
        "inventory.manual-inbound" => "Nhập hàng thực tế có kiểm tra SKU, vị trí, lô, hạn dùng và giá vốn trước khi cập nhật tồn kho.",
        "inventory.costing" => "Bình quân gia quyền di động theo kho và SKU tồn kho cơ sở; kỳ đã khóa không bị dựng lại âm thầm.",
        "inventory.lots" => "Theo dõi mã lô, ngày sản xuất, hạn sử dụng và thông tin liên quan của từng SKU.",
        "inventory.balances" => "Tra cứu số lượng tồn và lịch sử biến động theo SKU và kho",
        "inventory.tracking-policies" => "Thiết lập quản lý lô và hạn sử dụng theo SKU tồn chuẩn; có thể tìm bằng bất kỳ SKU nào của cùng sản phẩm.",
        "inventory.opening-balances" => "Chọn kho, nhập SKU và số lượng; hệ thống tự áp dụng chính sách lô/hạn dùng đã cấu hình theo SKU.",
        "logistics.reporting" => "Theo dõi chuyến, điểm giao, giao đủ đúng hạn, giao một phần, thất bại, hẹn lại và khối lượng vận hành theo tài xế, phương tiện.",
        "logistics.delivery-orders" => "Lập phiếu từ phần đã đóng gói; giao tận nơi có thể đi chuyến hoặc xác nhận giao thủ công theo đúng phiếu giao hàng.",
        "logistics.trips" => "Lập chuyến và gán nhiều phiếu giao theo tuyến; chưa xuất kho hay ghi kết quả giao.",
        "logistics.dispatch" => "Xác nhận hàng rời kho và ghi xuất kho cho toàn chuyến; chưa ghi kết quả giao hay bằng chứng giao hàng.",
        "logistics.delivery-attempts" => "Đọc kết quả và bằng chứng tùy chọn tài xế đã ghi; không ghi thay tài xế và không tự nhập hàng về kho.",
        "logistics.trip-reconciliation" => "Đi theo 4 bước: chọn chuyến, kiểm tra chênh lệch, nhận hàng trả về rồi mới đóng chuyến.",
        "logistics.customer-returns" => "Lập phiếu từ đúng dòng đã xuất; chỉ xác nhận thực nhận mới tăng tồn kho.",
        "purchasing.reporting" => "Theo dõi giá trị đơn mua đã có hiệu lực, tiến độ duyệt và nhận hàng, cùng các nhà cung cấp và SKU nổi bật.",
        "purchasing.purchase-orders" => "Tạo, gửi duyệt và phê duyệt nhu cầu mua từ nhà cung cấp trước khi nhận hàng.",
        "purchasing.purchase-prices" => "Thiết lập giá mua theo nhà cung cấp, SKU, đơn vị, số lượng và thời gian hiệu lực. Giá bán được quản lý riêng.",
        "purchasing.goods-receipts" => "Nhập hàng từ đơn mua hàng, ghi sổ tồn kho và đảo phiếu khi cần.",
        "purchasing.supplier-returns" => "Lập phiếu trả từ hàng đã nhận, duyệt, ghi sổ xuất kho và đảo chứng từ khi cần.",
        "accounting.aging" => "Theo dõi số dư công nợ hiện tại trong phạm vi kho được cấp. Phải thu phân tuổi theo ngày chứng từ; phải trả theo ngày đến hạn trên chứng từ.",
        "accounting.cod-reporting" => "Theo dõi tiền khách đã trả, tiền tài xế đang giữ, bàn giao và kế toán tiếp nhận từ cùng một nguồn dữ liệu COD chính thức.",
        "desktop.settings" => "Tài khoản, kết nối và giao diện ứng dụng máy tính",
        _ => "Thông tin tổng hợp phục vụ điều hành"
    };

    public string HeaderKicker => _selectedNavigationKey switch
    {
        "catalog.overview" => "BÁO CÁO QUẢN TRỊ",
        "catalog.branches" => "DANH MỤC TỔ CHỨC VÀ KHO",
        "catalog.warehouses" => "DANH MỤC QUẢN LÝ",
        "catalog.customers" => _partners.IsCustomerProfileOpen ? "KHÁCH HÀNG" : "QUẢN LÝ KHÁCH HÀNG",
        "catalog.products" => "QUẢN LÝ HÀNG HÓA",
        "catalog.pricing" => "GIÁ BÁN VÀ KHUYẾN MÃI",
        "catalog.document-numbering" => "QUẢN LÝ CHỨNG TỪ",
        "sales.reporting" => "BÁN HÀNG · BÁO CÁO",
        "sales.gross-margin" => "BÁN HÀNG",
        "inventory.fulfillment" => "KHO VÀ HOÀN TẤT ĐƠN",
        "inventory.transfer" => "TỒN KHO & LÔ HÀNG",
        "inventory.stocktake" => "TỒN KHO & LÔ HÀNG",
        "inventory.adjustments" => "TỒN KHO & LÔ HÀNG",
        "inventory.manual-inbound" => "TỒN KHO & LÔ HÀNG",
        "inventory.costing" => "TỒN KHO & LÔ HÀNG",
        "inventory.balances" => "TỒN KHO & LÔ HÀNG",
        "inventory.tracking-policies" => "TỒN KHO & LÔ HÀNG",
        "inventory.lots" => "TỒN KHO, LÔ VÀ NHẬP ĐẦU KỲ",
        "inventory.opening-balances" => "TỒN KHO",
        "logistics.reporting" => "GIAO NHẬN & ĐIỀU PHỐI",
        "logistics.delivery-orders" => "KHO VÀ GIAO NHẬN",
        "logistics.trips" => "GIAO NHẬN",
        "logistics.dispatch" => "GIAO NHẬN",
        "logistics.delivery-attempts" => "ĐIỀU PHỐI GIAO HÀNG",
        "logistics.trip-reconciliation" => "ĐIỀU PHỐI GIAO HÀNG",
        "logistics.customer-returns" => "KHO VÀ BÁN HÀNG",
        "purchasing.reporting" => "MUA HÀNG",
        "purchasing.purchase-orders" => "MUA HÀNG",
        "purchasing.purchase-prices" => "MUA HÀNG",
        "purchasing.goods-receipts" => "MUA HÀNG",
        "purchasing.supplier-returns" => "MUA HÀNG",
        "accounting.aging" => "KẾ TOÁN & CÔNG NỢ",
        "accounting.cod-reporting" => "KẾ TOÁN & CÔNG NỢ",
        _ => "HỆ THỐNG CÔNG TY"
    };

    public bool IsHomeSelected => _selectedNavigationKey == "dashboard";
    public bool IsInternalOrganizationSelected => SelectedWorkspaceIndex == 1;
    public bool IsPartnersSelected => SelectedWorkspaceIndex == 2;
    public bool IsSalesSelected => SelectedWorkspaceIndex == 3;
    public bool IsInventorySelected => SelectedWorkspaceIndex == 4;
    public bool IsSettingsSelected => SelectedWorkspaceIndex == 5;

    public bool IsCatalogGroupSelected => _selectedNavigationKey.StartsWith("catalog.", StringComparison.Ordinal);
    public bool IsInventoryGroupSelected => _selectedNavigationKey.StartsWith("inventory.", StringComparison.Ordinal);
    public bool IsLogisticsGroupSelected => _selectedNavigationKey.StartsWith("logistics.", StringComparison.Ordinal);
    public bool IsSalesGroupSelected => _selectedNavigationKey.StartsWith("sales.", StringComparison.Ordinal);
    public bool IsPurchasingGroupSelected => _selectedNavigationKey.StartsWith("purchasing.", StringComparison.Ordinal);
    public bool IsCatalogOverviewSelected => _selectedNavigationKey == "catalog.overview";
    public bool IsCatalogBranchesSelected => _selectedNavigationKey == "catalog.branches";
    public bool IsCatalogWarehousesSelected => _selectedNavigationKey == "catalog.warehouses";
    public bool IsCatalogLocationsSelected => _selectedNavigationKey == "catalog.locations";
    public bool IsCatalogCustomersSelected => _selectedNavigationKey == "catalog.customers";
    public bool IsCustomerProfileOpen => IsCatalogCustomersSelected && _partners.IsCustomerProfileOpen;
    public bool IsCustomerCatalogWorkspace => IsCatalogCustomersSelected && !_partners.IsCustomerProfileOpen;
    public bool CanShowCustomerTopbarCreate => IsCustomerCatalogWorkspace && _partners.CanShowCustomerTopbarCreate;
    public string CustomerTopbarCreateText => _partners.CustomerTopbarCreateText;
    public bool IsCatalogSuppliersSelected => _selectedNavigationKey == "catalog.suppliers";
    public bool IsCatalogProductsSelected => _selectedNavigationKey == "catalog.products";
    public bool IsCatalogPricingSelected => _selectedNavigationKey == "catalog.pricing";
    public bool IsCatalogDocumentNumberingSelected => _selectedNavigationKey == "catalog.document-numbering";
    public bool IsSalesReportingSelected => _selectedNavigationKey == "sales.reporting";
    public bool IsGrossMarginSelected => _selectedNavigationKey == "sales.gross-margin";
    public bool IsSalesOrdersSelected => _selectedNavigationKey == "sales.orders";
    public bool IsInventoryReportingSelected => _selectedNavigationKey == "inventory.reporting";
    public bool IsInventoryFulfillmentSelected => _selectedNavigationKey == "inventory.fulfillment";
    public bool IsInventoryTransferSelected => _selectedNavigationKey == "inventory.transfer";
    public bool IsInventoryStocktakeSelected => _selectedNavigationKey == "inventory.stocktake";
    public bool IsInventoryAdjustmentSelected => _selectedNavigationKey == "inventory.adjustments";
    public bool IsManualInboundSelected => _selectedNavigationKey == "inventory.manual-inbound";
    public bool IsInventoryCostingSelected => _selectedNavigationKey == "inventory.costing";
    public bool IsInventoryBalancesSelected => _selectedNavigationKey == "inventory.balances";
    public bool IsInventoryTrackingPolicySelected => _selectedNavigationKey == "inventory.tracking-policies";
    public bool IsInventoryLotsSelected => _selectedNavigationKey == "inventory.lots";
    public bool IsInventoryOpeningBalancesSelected => _selectedNavigationKey == "inventory.opening-balances";
    public bool IsLogisticsReportingSelected => _selectedNavigationKey == "logistics.reporting";
    public bool IsDeliveryOrdersSelected => _selectedNavigationKey == "logistics.delivery-orders";
    public bool IsTripPlanningSelected => _selectedNavigationKey == "logistics.trips";
    public bool IsTripDispatchSelected => _selectedNavigationKey == "logistics.dispatch";
    public bool IsDeliveryAttemptsSelected => _selectedNavigationKey == "logistics.delivery-attempts";
    public bool IsTripReconciliationSelected => _selectedNavigationKey == "logistics.trip-reconciliation";
    public bool IsCustomerReturnsSelected => _selectedNavigationKey == "logistics.customer-returns";
    public bool IsPurchasingReportingSelected => _selectedNavigationKey == "purchasing.reporting";
    public bool IsPurchaseOrdersSelected => _selectedNavigationKey == "purchasing.purchase-orders";
    public bool IsPurchasePricesSelected => _selectedNavigationKey == "purchasing.purchase-prices";
    public bool IsGoodsReceiptsSelected => _selectedNavigationKey == "purchasing.goods-receipts";
    public bool IsSupplierReturnsSelected => _selectedNavigationKey == "purchasing.supplier-returns";
    public bool IsAgingSelected => _selectedNavigationKey == "accounting.aging";
    public bool IsCodAccountingSelected => _selectedNavigationKey == "accounting.cod-reporting";

    public bool IsCatalogOpen { get => _isCatalogOpen; private set { if (SetField(ref _isCatalogOpen, value)) OnPropertyChanged(nameof(CatalogChevron)); } }
    public bool IsInventoryOpen { get => _isInventoryOpen; private set { if (SetField(ref _isInventoryOpen, value)) OnPropertyChanged(nameof(InventoryChevron)); } }
    public bool IsLogisticsOpen { get => _isLogisticsOpen; private set { if (SetField(ref _isLogisticsOpen, value)) OnPropertyChanged(nameof(LogisticsChevron)); } }
    public bool IsSalesOpen { get => _isSalesOpen; private set { if (SetField(ref _isSalesOpen, value)) OnPropertyChanged(nameof(SalesChevron)); } }
    public bool IsPurchasingOpen { get => _isPurchasingOpen; private set { if (SetField(ref _isPurchasingOpen, value)) OnPropertyChanged(nameof(PurchasingChevron)); } }
    public bool IsAccountingOpen { get => _isAccountingOpen; private set { if (SetField(ref _isAccountingOpen, value)) OnPropertyChanged(nameof(AccountingChevron)); } }
    public bool IsCompanySettingsOpen { get => _isCompanySettingsOpen; private set { if (SetField(ref _isCompanySettingsOpen, value)) OnPropertyChanged(nameof(CompanySettingsChevron)); } }
    public bool IsAccessOpen { get => _isAccessOpen; private set { if (SetField(ref _isAccessOpen, value)) OnPropertyChanged(nameof(AccessChevron)); } }

    public string CatalogChevron => IsCatalogOpen ? "⌄" : "›";
    public string InventoryChevron => IsInventoryOpen ? "⌄" : "›";
    public string LogisticsChevron => IsLogisticsOpen ? "⌄" : "›";
    public string SalesChevron => IsSalesOpen ? "⌄" : "›";
    public string PurchasingChevron => IsPurchasingOpen ? "⌄" : "›";
    public string AccountingChevron => IsAccountingOpen ? "⌄" : "›";
    public string CompanySettingsChevron => IsCompanySettingsOpen ? "⌄" : "›";
    public string AccessChevron => IsAccessOpen ? "⌄" : "›";

    public string UserDisplayName =>
        _access.Current.EmployeeFullName
        ?? _access.Current.LoginName
        ?? "Người dùng Công Ty";

    public string LoginNameText =>
        string.IsNullOrWhiteSpace(_access.Current.LoginName)
            ? "Chưa có thông tin"
            : _access.Current.LoginName;

    public string AccountTypeText => _access.Current.OwnerKind switch
    {
        "PERMANENT" => "Chủ hệ thống",
        "TEMPORARY" => "Quản trị triển khai",
        _ => "Tài khoản nhân viên"
    };

    public string PermissionSummary =>
        _access.Current.IsAuthenticated
            ? $"{_access.Current.Permissions.Count} quyền thao tác được hệ thống Công Ty cấp."
            : "Chưa có thông tin quyền truy cập.";

    public string ScopeSummary =>
        _access.Current.IsAuthenticated
            ? $"Chi nhánh được cấp: {_access.Current.Scopes.BranchIds.Length}  •  Kho được cấp: {_access.Current.Scopes.WarehouseIds.Length}"
            : "Chưa có thông tin phạm vi.";

    public string SessionSummary =>
        _access.Current.IsAuthenticated
            ? "Phiên đăng nhập đang có hiệu lực và được hệ thống Công Ty quản lý."
            : "Chưa đăng nhập.";

    public bool CanViewInternalOrganization =>
        _access.CanNavigate("internal-organization");

    public bool CanViewPartners =>
        _access.CanNavigate("partners");

    public bool CanViewProducts =>
        _products.CanRead;

    public bool CanViewPricing =>
        _pricing.CanRead;

    public bool CanViewDocumentNumbering =>
        _documentNumbering.CanRead;

    public bool CanViewSales =>
        _access.CanNavigate("sales");

    public bool CanViewSalesReporting =>
        _salesReporting.CanRead;

    public bool CanExportSalesReporting =>
        _salesReporting.CanOpenExport;

    public bool CanViewGrossMargin =>
        _grossMarginReporting.CanRead;

    public bool CanExportGrossMargin =>
        _grossMarginReporting.CanOpenExport;

    public bool CanViewInventory =>
        _access.CanNavigate("inventory") || _stocktake.CanRead || _adjustment.CanRead || _manualInbound.CanOpen || _inventoryCosting.CanOpen || _inventoryLookup.CanRead || _inventoryTrackingPolicy.CanRead || _inventoryLots.CanRead || _openingBalance.CanImport;

    public bool CanViewFulfillment =>
        _access.CanNavigate("fulfillment");

    public bool CanViewInventoryTransfer =>
        _access.CanNavigate("inventory-transfer");

    public bool CanCreateTransferTopbar =>
        IsInventoryTransferSelected && _transfer.CanCreate;

    public bool CanUseTransferTopbarActions =>
        IsInventoryTransferSelected && _transfer.IsNotBusy;

    public bool CanViewInventoryStocktake =>
        _stocktake.CanRead;

    public bool CanCreateStocktakeTopbar =>
        IsInventoryStocktakeSelected && _stocktake.CanCreate;

    public bool CanUseStocktakeTopbarActions =>
        IsInventoryStocktakeSelected && _stocktake.IsNotBusy;

    public bool CanViewInventoryAdjustment =>
        _adjustment.CanRead;

    public bool IsAdjustmentDocumentsTab =>
        IsInventoryAdjustmentSelected && _adjustment.IsDocumentsTab;

    public bool CanUseAdjustmentTopbarActions =>
        IsAdjustmentDocumentsTab && _adjustment.IsNotBusy;

    public bool CanViewManualInbound =>
        _manualInbound.CanOpen;

    public bool CanViewInventoryCosting =>
        _inventoryCosting.CanOpen;

    public bool CanViewInventoryBalances =>
        _inventoryLookup.CanRead;

    public bool CanViewInventoryTrackingPolicy =>
        _inventoryTrackingPolicy.CanRead;

    public bool CanViewInventoryLots =>
        _inventoryLots.CanRead;

    public bool CanViewOpeningBalance =>
        _openingBalance.CanImport;

    public bool CanViewLogisticsReporting =>
        _logisticsReporting.CanRead;

    public bool CanViewDeliveryOrders =>
        _deliveryOrders.CanRead;

    public bool CanViewTripPlanning =>
        _tripPlanning.CanRead;

    public bool CanViewTripDispatch =>
        _tripDispatch.CanRead;

    public bool CanViewDeliveryAttempts =>
        _deliveryAttempts.CanRead;

    public bool CanViewTripReconciliation =>
        _tripReconciliation.CanRead;

    public bool CanViewCustomerReturns =>
        _customerReturns.CanRead;

    public bool CanViewPurchasingReporting =>
        _purchasingReporting.CanRead;

    public bool CanViewPurchaseOrders =>
        _purchaseOrders.CanRead;

    public bool CanViewPurchasePrices =>
        _purchasePrices.CanRead;

    public bool CanViewGoodsReceipts =>
        _goodsReceipts.CanRead;

    public bool CanViewSupplierReturns =>
        _supplierReturns.CanRead;

    public bool CanViewAging =>
        _agingReporting.CanRead;

    public bool CanViewCodAccounting =>
        _codAccounting.CanReadReport;

    public bool CanRunCostingRebuild =>
        IsInventoryCostingSelected && _inventoryCosting.CanRunRebuild;

    public string CostingRebuildActionText =>
        _inventoryCosting.IsBusy ? "Đang xử lý…" : "Dựng lại giá vốn";

    public async Task InitializeAsync()
    {
        if (!_installation.IsConfigured)
        {
            SetStage(ShellStage.Setup);
            return;
        }

        await _connection.RefreshAsync().ConfigureAwait(true);
        var restored = await _authentication.RestoreAsync().ConfigureAwait(true);

        if (restored.Kind == SessionRestoreKind.Succeeded)
        {
            EnterWorkspace();
            return;
        }

        LoginMessage = CanonicalErrorMessages.WithRequestId(restored.Message, restored.RequestId);
        SetStage(ShellStage.Login);
    }

    public async Task SaveInstallationAsync()
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            SetupMessage = "Đang kiểm tra địa chỉ hệ thống...";
            var result = await _installation.ValidateAndSaveAsync(
                SetupInstallationName,
                SetupCompanyName,
                SetupApiUrl).ConfigureAwait(true);

            SetupMessage = CanonicalErrorMessages.WithRequestId(result.Message, result.RequestId);
            if (!result.Success)
            {
                return;
            }

            SetupApiUrl = result.NormalizedApiUrl ?? SetupApiUrl;
            await _connection.RefreshAsync().ConfigureAwait(true);
            LoginMessage = "Cấu hình kết nối đã sẵn sàng. Vui lòng đăng nhập.";
            IsOwnerCodeRequired = false;
            OwnerCode = string.Empty;
            SetStage(ShellStage.Login);
        }
        finally
        {
            EndBusy();
        }
    }

    public async Task SubmitLoginAsync(string password)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            LoginMessage = "Đang xác nhận tài khoản...";
            var result = await _authentication.LoginAsync(
                LoginName,
                password,
                IsOwnerCodeRequired ? OwnerCode : null).ConfigureAwait(true);

            LoginMessage = CanonicalErrorMessages.WithRequestId(result.Message, result.RequestId);

            if (result.Kind == LoginAttemptKind.ChallengeRequired)
            {
                IsOwnerCodeRequired = true;
                return;
            }

            if (result.Kind != LoginAttemptKind.Succeeded)
            {
                return;
            }

            IsOwnerCodeRequired = false;
            OwnerCode = string.Empty;
            EnterWorkspace();
        }
        finally
        {
            EndBusy();
        }
    }

    public async Task RetryStoredSessionAsync()
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            var result = await _authentication.RestoreAsync().ConfigureAwait(true);
            LoginMessage = CanonicalErrorMessages.WithRequestId(result.Message, result.RequestId);
            if (result.Kind == SessionRestoreKind.Succeeded)
            {
                EnterWorkspace();
            }
        }
        finally
        {
            EndBusy();
        }
    }

    public async Task LogoutAsync()
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            var result = await _authentication.LogoutAsync().ConfigureAwait(true);
            LoginMessage = CanonicalErrorMessages.WithRequestId(result.Message, result.RequestId);
            IsOwnerCodeRequired = false;
            OwnerCode = string.Empty;
            SelectedWorkspaceIndex = 0;
            SetStage(ShellStage.Login);
        }
        finally
        {
            EndBusy();
        }
    }

    public void OpenInstallationSetup()
    {
        var settings = _settingsState.Current;
        SetupInstallationName = settings.InstallationName;
        SetupCompanyName = settings.CompanyDisplayName;
        SetupApiUrl = settings.ApiBaseUrl;
        SetupMessage = string.Empty;
        SetStage(ShellStage.Setup);
    }

    public void NavigateHome()
    {
        SetSelectedNavigation("dashboard");
        if (Navigate("home", 0))
        {
            _ = _dashboard.EnsureLoadedAsync();
        }
    }

    public void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        if (!IsSidebarExpanded) CloseNavigationGroups();
    }

    public void ToggleNavigationGroup(string group)
    {
        if (!IsSidebarExpanded) IsSidebarExpanded = true;
        switch (group)
        {
            case "catalog": IsCatalogOpen = !IsCatalogOpen; break;
            case "inventory": IsInventoryOpen = !IsInventoryOpen; break;
            case "logistics": IsLogisticsOpen = !IsLogisticsOpen; break;
            case "sales": IsSalesOpen = !IsSalesOpen; break;
            case "purchasing": IsPurchasingOpen = !IsPurchasingOpen; break;
            case "accounting": IsAccountingOpen = !IsAccountingOpen; break;
            case "company-settings": IsCompanySettingsOpen = !IsCompanySettingsOpen; break;
            case "access": IsAccessOpen = !IsAccessOpen; break;
        }
    }

    public void NavigateSettings()
    {
        SetSelectedNavigation("desktop.settings");
        NavigateSettingsTab(0);
    }

    public void NavigateAccess() => NavigateSettings();

    public async Task NavigateSystemStatusAsync()
    {
        SetSelectedNavigation("desktop.settings");
        if (NavigateSettingsTab(1))
        {
            await RefreshConnectionAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigateInternalOrganizationAsync(string navigationKey = "catalog.overview")
    {
        if (string.Equals(navigationKey, "catalog.locations", StringComparison.Ordinal))
        {
            navigationKey = "catalog.warehouses";
        }

        SetSelectedNavigation(navigationKey);
        IsCatalogOpen = true;
        if (Navigate("internal-organization", 1))
        {
            await _internalOrganization.EnsureLoadedAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigatePartnersAsync(string navigationKey = "catalog.customers")
    {
        SetSelectedNavigation(navigationKey);
        IsCatalogOpen = true;
        if (Navigate("partners", 2))
        {
            await _partners.EnsureLoadedAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigateProductsAsync()
    {
        SetSelectedNavigation("catalog.products");
        IsCatalogOpen = true;

        if (!_products.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Danh mục sản phẩm.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 11;
        await _products.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigatePricingAsync()
    {
        SetSelectedNavigation("catalog.pricing");
        IsCatalogOpen = true;

        if (!_pricing.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Giá bán và khuyến mãi.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 15;
        await _pricing.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateDocumentNumberingAsync()
    {
        SetSelectedNavigation("catalog.document-numbering");
        IsCatalogOpen = true;

        if (!_documentNumbering.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Số chứng từ.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 17;
        await _documentNumbering.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateSalesAsync()
    {
        SetSelectedNavigation("sales.orders");
        IsSalesOpen = true;
        if (Navigate("sales", 3))
        {
            await _sales.EnsureLoadedAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigateSalesReportingAsync()
    {
        SetSelectedNavigation("sales.reporting");
        IsSalesOpen = true;
        if (!_salesReporting.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Báo cáo bán hàng.";
            return;
        }
        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 32;
        await _salesReporting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateGrossMarginAsync()
    {
        SetSelectedNavigation("sales.gross-margin");
        IsSalesOpen = true;
        if (!_grossMarginReporting.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lãi gộp.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 33;
        await _grossMarginReporting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryAsync(int mainTabIndex = 0, string navigationKey = "inventory.balances")
    {
        SetSelectedNavigation(navigationKey);
        IsInventoryOpen = true;
        if (Navigate("inventory", 4))
        {
            await _inventory.EnsureLoadedAsync().ConfigureAwait(true);
            _inventory.MainTabIndex = mainTabIndex;
        }
    }

    public async Task NavigateFulfillmentAsync()
    {
        SetSelectedNavigation("inventory.fulfillment");
        IsInventoryOpen = true;
        if (Navigate("fulfillment", 6))
        {
            await _fulfillment.EnsureLoadedAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigateInventoryTransferAsync()
    {
        SetSelectedNavigation("inventory.transfer");
        IsInventoryOpen = true;
        if (Navigate("inventory-transfer", 7))
        {
            await _transfer.EnsureLoadedAsync().ConfigureAwait(true);
        }
    }

    public async Task NavigateInventoryStocktakeAsync()
    {
        SetSelectedNavigation("inventory.stocktake");
        IsInventoryOpen = true;

        if (!_stocktake.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Kiểm kê kho.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 8;
        await _stocktake.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryAdjustmentAsync()
    {
        SetSelectedNavigation("inventory.adjustments");
        IsInventoryOpen = true;

        if (!_adjustment.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Điều chỉnh tồn.";
            return;
        }

        WorkspaceMessage = string.Empty;
        _adjustment.SetTab("documents");
        SelectedWorkspaceIndex = 9;
        await _adjustment.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateManualInboundAsync()
    {
        SetSelectedNavigation("inventory.manual-inbound");
        IsInventoryOpen = true;

        if (!_manualInbound.CanOpen)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền dùng Nhập kho thủ công.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 10;
        await _manualInbound.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryCostingAsync()
    {
        SetSelectedNavigation("inventory.costing");
        IsInventoryOpen = true;

        if (!_inventoryCosting.CanOpen)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Giá vốn tồn kho.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 12;
        await _inventoryCosting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryLookupAsync()
    {
        SetSelectedNavigation("inventory.balances");
        IsInventoryOpen = true;

        if (!_inventoryLookup.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tra cứu tồn kho.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 13;
        await _inventoryLookup.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryTrackingPolicyAsync()
    {
        SetSelectedNavigation("inventory.tracking-policies");
        IsInventoryOpen = true;

        if (!_inventoryTrackingPolicy.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Chính sách lô.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 14;
        await _inventoryTrackingPolicy.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateInventoryLotsAsync()
    {
        SetSelectedNavigation("inventory.lots");
        IsInventoryOpen = true;

        if (!_inventoryLots.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lô hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 16;
        await _inventoryLots.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateOpeningBalanceAsync()
    {
        SetSelectedNavigation("inventory.opening-balances");
        IsInventoryOpen = true;

        if (!_openingBalance.CanImport)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền Thiết lập tồn đầu kỳ.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 18;
        await _openingBalance.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateLogisticsReportingAsync()
    {
        SetSelectedNavigation("logistics.reporting");
        IsLogisticsOpen = true;

        if (!_logisticsReporting.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Hiệu suất giao hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 19;
        await _logisticsReporting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateDeliveryOrdersAsync()
    {
        SetSelectedNavigation("logistics.delivery-orders");
        IsLogisticsOpen = true;

        if (!_deliveryOrders.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Phiếu giao hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 21;
        await _deliveryOrders.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateTripPlanningAsync()
    {
        SetSelectedNavigation("logistics.trips");
        IsLogisticsOpen = true;

        if (!_tripPlanning.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Lập và xếp chuyến.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 23;
        await _tripPlanning.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateTripDispatchAsync()
    {
        SetSelectedNavigation("logistics.dispatch");
        IsLogisticsOpen = true;

        if (!_tripDispatch.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Bàn giao và xuất phát.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 25;
        await _tripDispatch.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateDeliveryAttemptsAsync()
    {
        SetSelectedNavigation("logistics.delivery-attempts");
        IsLogisticsOpen = true;

        if (!_deliveryAttempts.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Kết quả lần giao.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 27;
        await _deliveryAttempts.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateTripReconciliationAsync()
    {
        SetSelectedNavigation("logistics.trip-reconciliation");
        IsLogisticsOpen = true;

        if (!_tripReconciliation.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Đối soát cuối chuyến.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 29;
        await _tripReconciliation.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateCustomerReturnsAsync()
    {
        SetSelectedNavigation("logistics.customer-returns");
        IsLogisticsOpen = true;

        if (!_customerReturns.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Hàng khách trả.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 30;
        await _customerReturns.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateAgingAsync()
    {
        SetSelectedNavigation("accounting.aging");
        IsAccountingOpen = true;

        if (!_agingReporting.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Tuổi nợ.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 31;
        await _agingReporting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateCodAccountingAsync()
    {
        SetSelectedNavigation("accounting.cod-reporting");
        IsAccountingOpen = true;

        if (!_codAccounting.CanReadReport)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem COD và đối soát.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 34;
        await _codAccounting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigatePurchasingReportingAsync()
    {
        SetSelectedNavigation("purchasing.reporting");
        IsPurchasingOpen = true;

        if (!_purchasingReporting.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Báo cáo mua hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 20;
        await _purchasingReporting.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigatePurchaseOrdersAsync()
    {
        SetSelectedNavigation("purchasing.purchase-orders");
        IsPurchasingOpen = true;

        if (!_purchaseOrders.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Đơn mua hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 22;
        await _purchaseOrders.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigatePurchaseOrdersSearchAsync(string searchText)
    {
        if (!_purchaseOrders.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Đơn mua hàng.";
            return;
        }

        await NavigatePurchaseOrdersAsync().ConfigureAwait(true);
        _purchaseOrders.SearchText = searchText?.Trim() ?? string.Empty;
    }

    public async Task NavigatePurchasePricesAsync()
    {
        SetSelectedNavigation("purchasing.purchase-prices");
        IsPurchasingOpen = true;

        if (!_purchasePrices.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Bảng giá mua.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 24;
        await _purchasePrices.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateGoodsReceiptsAsync()
    {
        SetSelectedNavigation("purchasing.goods-receipts");
        IsPurchasingOpen = true;

        if (!_goodsReceipts.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Phiếu nhận hàng.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 26;
        await _goodsReceipts.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task NavigateSupplierReturnsAsync()
    {
        SetSelectedNavigation("purchasing.supplier-returns");
        IsPurchasingOpen = true;

        if (!_supplierReturns.CanRead)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Phiếu trả nhà cung cấp.";
            return;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 28;
        await _supplierReturns.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public Task RefreshConnectionAsync() => _connection.RefreshAsync();

    public async Task ToggleThemeAsync()
    {
        IsDark = !IsDark;
        var theme = IsDark ? "Dark" : "Light";
        ThemeManager.Apply(theme);

        var updated = _settingsState.Current with { Theme = theme };
        await _settingsStore.SaveAsync(updated).ConfigureAwait(true);
        _settingsState.Update(updated);
    }

    private void SetSelectedNavigation(string key)
    {
        if (string.Equals(_selectedNavigationKey, key, StringComparison.Ordinal)) return;
        _selectedNavigationKey = key;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
        OnPropertyChanged(nameof(HeaderKicker));
        OnPropertyChanged(nameof(IsHomeSelected));
        OnPropertyChanged(nameof(IsCatalogGroupSelected));
        OnPropertyChanged(nameof(IsInventoryGroupSelected));
        OnPropertyChanged(nameof(IsLogisticsGroupSelected));
        OnPropertyChanged(nameof(IsSalesGroupSelected));
        OnPropertyChanged(nameof(IsPurchasingGroupSelected));
        OnPropertyChanged(nameof(IsCatalogOverviewSelected));
        OnPropertyChanged(nameof(IsCatalogBranchesSelected));
        OnPropertyChanged(nameof(IsCatalogWarehousesSelected));
        OnPropertyChanged(nameof(IsCatalogLocationsSelected));
        OnPropertyChanged(nameof(IsCatalogCustomersSelected));
        OnPropertyChanged(nameof(IsCustomerProfileOpen));
        OnPropertyChanged(nameof(IsCustomerCatalogWorkspace));
        OnPropertyChanged(nameof(CanShowCustomerTopbarCreate));
        OnPropertyChanged(nameof(CustomerTopbarCreateText));
        OnPropertyChanged(nameof(IsCatalogSuppliersSelected));
        OnPropertyChanged(nameof(IsCatalogProductsSelected));
        OnPropertyChanged(nameof(IsCatalogPricingSelected));
        OnPropertyChanged(nameof(IsCatalogDocumentNumberingSelected));
        OnPropertyChanged(nameof(IsSalesReportingSelected));
        OnPropertyChanged(nameof(IsGrossMarginSelected));
        OnPropertyChanged(nameof(IsSalesOrdersSelected));
        OnPropertyChanged(nameof(IsInventoryReportingSelected));
        OnPropertyChanged(nameof(IsInventoryFulfillmentSelected));
        OnPropertyChanged(nameof(IsInventoryTransferSelected));
        OnPropertyChanged(nameof(CanCreateTransferTopbar));
        OnPropertyChanged(nameof(CanUseTransferTopbarActions));
        OnPropertyChanged(nameof(IsInventoryStocktakeSelected));
        OnPropertyChanged(nameof(CanCreateStocktakeTopbar));
        OnPropertyChanged(nameof(CanUseStocktakeTopbarActions));
        OnPropertyChanged(nameof(IsInventoryAdjustmentSelected));
        OnPropertyChanged(nameof(IsAdjustmentDocumentsTab));
        OnPropertyChanged(nameof(CanUseAdjustmentTopbarActions));
        OnPropertyChanged(nameof(IsManualInboundSelected));
        OnPropertyChanged(nameof(IsInventoryCostingSelected));
        OnPropertyChanged(nameof(CanRunCostingRebuild));
        OnPropertyChanged(nameof(CostingRebuildActionText));
        OnPropertyChanged(nameof(IsInventoryBalancesSelected));
        OnPropertyChanged(nameof(IsInventoryTrackingPolicySelected));
        OnPropertyChanged(nameof(IsInventoryLotsSelected));
        OnPropertyChanged(nameof(IsInventoryOpeningBalancesSelected));
        OnPropertyChanged(nameof(IsLogisticsReportingSelected));
        OnPropertyChanged(nameof(IsDeliveryOrdersSelected));
        OnPropertyChanged(nameof(IsTripPlanningSelected));
        OnPropertyChanged(nameof(IsTripDispatchSelected));
        OnPropertyChanged(nameof(IsDeliveryAttemptsSelected));
        OnPropertyChanged(nameof(IsTripReconciliationSelected));
        OnPropertyChanged(nameof(IsCustomerReturnsSelected));
        OnPropertyChanged(nameof(IsPurchasingReportingSelected));
        OnPropertyChanged(nameof(IsPurchaseOrdersSelected));
        OnPropertyChanged(nameof(IsPurchasePricesSelected));
        OnPropertyChanged(nameof(IsGoodsReceiptsSelected));
        OnPropertyChanged(nameof(IsSupplierReturnsSelected));
        OnPropertyChanged(nameof(IsAgingSelected));
        OnPropertyChanged(nameof(IsCodAccountingSelected));
    }

    private void CloseNavigationGroups()
    {
        IsCatalogOpen = false;
        IsInventoryOpen = false;
        IsLogisticsOpen = false;
        IsSalesOpen = false;
        IsPurchasingOpen = false;
        IsAccountingOpen = false;
        IsCompanySettingsOpen = false;
        IsAccessOpen = false;
    }

    private bool NavigateSettingsTab(int tabIndex)
    {
        if (!Navigate("settings", 5))
        {
            return false;
        }

        SelectedSettingsIndex = tabIndex;
        return true;
    }

    private bool Navigate(string key, int index)
    {
        if (!_access.CanNavigate(key))
        {
            WorkspaceMessage = "Tài khoản chưa được phép mở mục này.";
            return false;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = index;
        return true;
    }

    private void EnterWorkspace()
    {
        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 0;
        SetStage(ShellStage.Workspace);
        RaiseAccessChanged();
        _ = _dashboard.EnsureLoadedAsync();
    }

    private void SetStage(ShellStage stage)
    {
        if (_stage == stage)
        {
            return;
        }

        _stage = stage;
        OnPropertyChanged(nameof(IsSetup));
        OnPropertyChanged(nameof(IsLogin));
        OnPropertyChanged(nameof(IsWorkspace));
    }

    private bool TryBeginBusy()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        return true;
    }

    private void EndBusy() => IsBusy = false;

    private void RaiseActiveNotice()
    {
        OnPropertyChanged(nameof(ActiveNotice));
        OnPropertyChanged(nameof(ActiveNoticeIsError));
        OnPropertyChanged(nameof(HasActiveNotice));
    }

    private void RaiseSettingsChanged()
    {
        OnPropertyChanged(nameof(CompanyName));
        OnPropertyChanged(nameof(InstallationName));
    }

    private void RaiseAccessChanged()
    {
        OnPropertyChanged(nameof(UserDisplayName));
        OnPropertyChanged(nameof(LoginNameText));
        OnPropertyChanged(nameof(AccountTypeText));
        OnPropertyChanged(nameof(PermissionSummary));
        OnPropertyChanged(nameof(ScopeSummary));
        OnPropertyChanged(nameof(SessionSummary));
        OnPropertyChanged(nameof(CanViewInternalOrganization));
        OnPropertyChanged(nameof(CanViewPartners));
        OnPropertyChanged(nameof(CanViewProducts));
        OnPropertyChanged(nameof(CanViewPricing));
        OnPropertyChanged(nameof(CanViewDocumentNumbering));
        OnPropertyChanged(nameof(CanViewSales));
        OnPropertyChanged(nameof(CanViewSalesReporting));
        OnPropertyChanged(nameof(CanExportSalesReporting));
        OnPropertyChanged(nameof(CanViewGrossMargin));
        OnPropertyChanged(nameof(CanExportGrossMargin));
        OnPropertyChanged(nameof(CanViewInventory));
        OnPropertyChanged(nameof(CanViewFulfillment));
        OnPropertyChanged(nameof(CanViewInventoryTransfer));
        OnPropertyChanged(nameof(CanCreateTransferTopbar));
        OnPropertyChanged(nameof(CanUseTransferTopbarActions));
        OnPropertyChanged(nameof(CanViewInventoryStocktake));
        OnPropertyChanged(nameof(CanCreateStocktakeTopbar));
        OnPropertyChanged(nameof(CanUseStocktakeTopbarActions));
        OnPropertyChanged(nameof(CanViewInventoryAdjustment));
        OnPropertyChanged(nameof(IsAdjustmentDocumentsTab));
        OnPropertyChanged(nameof(CanUseAdjustmentTopbarActions));
        OnPropertyChanged(nameof(CanViewManualInbound));
        OnPropertyChanged(nameof(CanViewInventoryCosting));
        OnPropertyChanged(nameof(CanViewInventoryBalances));
        OnPropertyChanged(nameof(CanViewInventoryTrackingPolicy));
        OnPropertyChanged(nameof(CanViewInventoryLots));
        OnPropertyChanged(nameof(CanViewOpeningBalance));
        OnPropertyChanged(nameof(CanViewLogisticsReporting));
        OnPropertyChanged(nameof(CanViewDeliveryOrders));
        OnPropertyChanged(nameof(CanViewTripPlanning));
        OnPropertyChanged(nameof(CanViewTripDispatch));
        OnPropertyChanged(nameof(CanViewDeliveryAttempts));
        OnPropertyChanged(nameof(CanViewTripReconciliation));
        OnPropertyChanged(nameof(CanViewCustomerReturns));
        OnPropertyChanged(nameof(CanViewPurchasingReporting));
        OnPropertyChanged(nameof(CanViewPurchaseOrders));
        OnPropertyChanged(nameof(CanViewPurchasePrices));
        OnPropertyChanged(nameof(CanViewGoodsReceipts));
        OnPropertyChanged(nameof(CanViewSupplierReturns));
        OnPropertyChanged(nameof(CanViewAging));
        OnPropertyChanged(nameof(CanViewCodAccounting));
        OnPropertyChanged(nameof(CanRunCostingRebuild));
        OnPropertyChanged(nameof(CostingRebuildActionText));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
