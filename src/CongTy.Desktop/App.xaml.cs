using System.IO;
using System.Net.Http;
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
using CongTy.Desktop.Shell;
using CongTy.Desktop.Themes;
using CongTy.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CongTy.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        var startupSmoke = e.Args.Any(argument =>
            string.Equals(argument, "--startup-smoke", StringComparison.Ordinal));

        try
        {
            base.OnStartup(e);
    
            var settingsStore = new JsonLocalSettingsStore();
            var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
            var settingsState = new DesktopSettingsState(settings);
            var endpointProvider = new CompanyEndpointProvider();
            if (!string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            {
                endpointProvider.TrySet(settings.ApiBaseUrl, out _, out _);
            }
    
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddDebug();
            });
    
            services.AddSingleton(settingsStore);
            services.AddSingleton<ILocalSettingsStore>(settingsStore);
            services.AddSingleton(settingsState);
            services.AddSingleton<ICompanyEndpointProvider>(endpointProvider);
            services.AddSingleton<ISecureCredentialStore, WindowsCredentialStore>();
            services.AddSingleton<ISessionTokenStore, WindowsSessionTokenStore>();
            services.AddSingleton<IRequestIdProvider, RequestIdProvider>();
            services.AddSingleton<ILogRedactor, LogRedactor>();
            services.AddSingleton<IAccessStateService, AccessStateService>();
            services.AddTransient<RequestIdHandler>();
            services.AddTransient<SafeHttpLoggingHandler>();
    
            services.AddHttpClient<CompanyApiClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                })
                .AddHttpMessageHandler<RequestIdHandler>()
                .AddHttpMessageHandler<SafeHttpLoggingHandler>();
    
            services.AddSingleton<ICustomerMediaTransferClient>(_ =>
                new CustomerMediaTransferClient(new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(60)
                }));
    
            services.AddSingleton<IInstallationProfileService, InstallationProfileService>();
            services.AddSingleton<AuthenticationService>();
            services.AddSingleton<IAuthenticationService>(provider => provider.GetRequiredService<AuthenticationService>());
            services.AddSingleton<IAuthenticatedSessionAccessor>(provider => provider.GetRequiredService<AuthenticationService>());
            services.AddSingleton<IConnectionStateService, ConnectionStateService>();
            services.AddSingleton<ICanonicalIdempotencyKeyProvider, CanonicalIdempotencyKeyProvider>();
            services.AddSingleton<IDocumentPrintTemplateService, DocumentPrintTemplateService>();
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<DashboardView>();
            services.AddSingleton<IInternalOrganizationService, InternalOrganizationService>();
            services.AddSingleton<InternalOrganizationViewModel>();
            services.AddSingleton<InternalOrganizationView>();
            services.AddSingleton<IPartnerService, PartnerService>();
            services.AddSingleton<PartnerViewModel>();
            services.AddSingleton<PartnerView>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IDataExchangeService, DataExchangeService>();
            services.AddSingleton<ProductViewModel>();
            services.AddSingleton<ProductView>();
            services.AddSingleton<IPricingService, PricingService>();
            services.AddSingleton<PricingViewModel>();
            services.AddSingleton<PricingView>();
            services.AddSingleton<IDocumentNumberingService, DocumentNumberingService>();
            services.AddSingleton<DocumentNumberingViewModel>();
            services.AddSingleton<DocumentNumberingView>();
            services.AddSingleton<ISalesOrderService, SalesOrderService>();
            services.AddSingleton<SalesViewModel>();
            services.AddSingleton<SalesView>();
            services.AddSingleton<ISalesReportingService, SalesReportingService>();
            services.AddSingleton<ISalesReportingViewStateStore, SalesReportingViewStateStore>();
            services.AddSingleton<SalesReportingViewModel>();
            services.AddSingleton<SalesReportingView>();
            services.AddSingleton<IGrossMarginReportingService, GrossMarginReportingService>();
            services.AddSingleton<GrossMarginReportingViewModel>();
            services.AddSingleton<GrossMarginReportingView>();
            services.AddSingleton<IManagementProposalService, ManagementProposalService>();
            services.AddSingleton<ManagementProposalViewModel>();
            services.AddSingleton<ManagementProposalView>();
            services.AddSingleton<ICustomerOnboardingService, CustomerOnboardingService>();
            services.AddSingleton<CustomerOnboardingViewModel>();
            services.AddSingleton<CustomerOnboardingView>();
            services.AddSingleton<IInventoryService, InventoryService>();
            services.AddSingleton<InventoryViewModel>();
            services.AddSingleton<InventoryView>();
            services.AddSingleton<IFulfillmentService, FulfillmentService>();
            services.AddSingleton<FulfillmentViewModel>();
            services.AddSingleton<FulfillmentView>();
            services.AddSingleton<IInventoryTransferService, InventoryTransferService>();
            services.AddSingleton<TransferViewModel>();
            services.AddSingleton<TransferView>();
            services.AddSingleton<IInventoryStocktakeService, InventoryStocktakeService>();
            services.AddSingleton<StocktakeViewModel>();
            services.AddSingleton<StocktakeView>();
            services.AddSingleton<IInventoryAdjustmentService, InventoryAdjustmentService>();
            services.AddSingleton<InventoryAdjustmentViewModel>();
            services.AddSingleton<InventoryAdjustmentView>();
            services.AddSingleton<IManualInboundService, ManualInboundService>();
            services.AddSingleton<ManualInboundViewModel>();
            services.AddSingleton<ManualInboundView>();
            services.AddSingleton<IInventoryCostingService, InventoryCostingService>();
            services.AddSingleton<InventoryCostingViewModel>();
            services.AddSingleton<InventoryCostingView>();
            services.AddSingleton<InventoryLookupViewModel>();
            services.AddSingleton<InventoryLookupView>();
            services.AddSingleton<IInventoryTrackingPolicyService, InventoryTrackingPolicyService>();
            services.AddSingleton<InventoryTrackingPolicyViewModel>();
            services.AddSingleton<InventoryTrackingPolicyView>();
            services.AddSingleton<InventoryLotsViewModel>();
            services.AddSingleton<InventoryLotsView>();
            services.AddSingleton<IOpeningBalanceService, OpeningBalanceService>();
            services.AddSingleton<OpeningBalanceViewModel>();
            services.AddSingleton<OpeningBalanceView>();
            services.AddSingleton<ILogisticsReportingService, LogisticsReportingService>();
            services.AddSingleton<LogisticsReportingViewModel>();
            services.AddSingleton<LogisticsReportingView>();
            services.AddSingleton<IDeliveryOrderService, DeliveryOrderService>();
            services.AddSingleton<DeliveryOrderViewModel>();
            services.AddSingleton<DeliveryOrderView>();
            services.AddSingleton<ITripPlanningService, TripPlanningService>();
            services.AddSingleton<TripPlanningViewModel>();
            services.AddSingleton<TripPlanningView>();
            services.AddSingleton<ITripDispatchService, TripDispatchService>();
            services.AddSingleton<TripDispatchViewModel>();
            services.AddSingleton<TripDispatchView>();
            services.AddSingleton<IDeliveryAttemptService, DeliveryAttemptService>();
            services.AddSingleton<DeliveryAttemptViewModel>();
            services.AddSingleton<DeliveryAttemptView>();
            services.AddSingleton<ITripReconciliationService, TripReconciliationService>();
            services.AddSingleton<TripReconciliationViewModel>();
            services.AddSingleton<TripReconciliationView>();
            services.AddSingleton<ICustomerReturnService, CustomerReturnService>();
            services.AddSingleton<CustomerReturnViewModel>();
            services.AddSingleton<CustomerReturnView>();
            services.AddSingleton<IAgingReportingService, AgingReportingService>();
            services.AddSingleton<AgingReportingViewModel>();
            services.AddSingleton<AgingReportingView>();
            services.AddSingleton<ICodAccountingService, CodAccountingService>();
            services.AddSingleton<CodAccountingViewModel>();
            services.AddSingleton<CodAccountingView>();
            services.AddSingleton<IPurchasingReportingService, PurchasingReportingService>();
            services.AddSingleton<PurchasingReportingViewModel>();
            services.AddSingleton<PurchasingReportingView>();
            services.AddSingleton<IPurchaseOrderService, PurchaseOrderService>();
            services.AddSingleton<PurchaseOrderViewModel>();
            services.AddSingleton<PurchaseOrderView>();
            services.AddSingleton<ISupplierPurchasePriceService, SupplierPurchasePriceService>();
            services.AddSingleton<PurchasePriceViewModel>();
            services.AddSingleton<PurchasePriceView>();
            services.AddSingleton<IGoodsReceiptService, GoodsReceiptService>();
            services.AddSingleton<GoodsReceiptViewModel>();
            services.AddSingleton<GoodsReceiptView>();
            services.AddSingleton<ISupplierReturnService, SupplierReturnService>();
            services.AddSingleton<SupplierReturnViewModel>();
            services.AddSingleton<SupplierReturnView>();
            services.AddSingleton<QuickActionWindowService>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<MainWindow>();
    
            _services = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    
            ThemeManager.Apply(settings.Theme);
    
            var mainWindow = _services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            ThemeManager.ApplyScale(settings.DisplayScale);
    
            if (e.Args.Any(argument => string.Equals(argument, "--startup-smoke", StringComparison.Ordinal)))
            {
                mainWindow.ShowActivated = false;
                mainWindow.ShowInTaskbar = false;
                mainWindow.Show();
                mainWindow.UpdateLayout();
                mainWindow.Dispatcher.Invoke(
                    static () => { },
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                mainWindow.Hide();
                Shutdown(0);
                return;
            }
    
            mainWindow.Show();
        
        }
        catch (Exception exception) when (startupSmoke)
        {
            var diagnosticPath = Path.Combine(AppContext.BaseDirectory, "startup-smoke-error.txt");
            File.WriteAllText(diagnosticPath, exception.ToString());
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
