using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class SalesViewModel : INotifyPropertyChanged
{
    private const string Read="core.sales-order.read";
    private const string Create="core.sales-order.create";
    private const string UpdateDraft="core.sales-order.update-draft";
    private const string Confirm="core.sales-order.confirm";
    private const string Amend="core.sales-order.amend";
    private const string Cancel="core.sales-order.cancel";
    private const string PriceOverride="core.sales-order.price.override";
    private const string DiscountOverride="core.sales-order.discount.override";
    private const string CreditOverride="core.sales-order.credit.override";
    private const string IssueInventory="core.delivery-order.issue-inventory";
    private const string CustomerPaymentCreate="core.customer-payment.create";
    private const string CustomerWrite="core.customer.write";
    private const string InventoryRead="core.inventory.read";

    private enum EditorMode { None,Create,Draft,Amendment,ManualEdit }

    private readonly ISalesOrderService _service;
    private readonly IPartnerService _partnerService;
    private readonly SalesSkuLocalCatalog _skuCatalog=new();
    private readonly ICanonicalIdempotencyKeyProvider _keys;
    private readonly IAccessStateService _access;
    private readonly IAuthenticationService _authentication;
    private readonly List<SalesOrderData> _orders=[];
    private readonly List<CustomerData> _customers=[];
    private readonly List<WarehouseData> _warehouses=[];
    private readonly List<CustomerAddressData> _addresses=[];
    private readonly List<SalesOrderSkuSearchOptionData> _skuOptions=[];
    private readonly Dictionary<string,string> _actionKeys=[];
    private readonly Dictionary<string,IReadOnlyList<ProductVariantData>> _productVariantCache=new(StringComparer.Ordinal);
    private readonly Dictionary<SalesDraftLineRow,int> _linePricingGeneration=[];
    private string _editorBaselineFingerprint=string.Empty;
    private string _editorPricingAt=string.Empty;
    private string? _preferredAddressId;

    private SalesOrderEntrySettingsData? _entrySettings;
    private SalesOrderData? _selectedOrder;
    private bool _isLoaded;
    private bool _isBusy;
    private string _message=string.Empty;
    private bool _messageIsError;
    private string _search=string.Empty;
    private string _laneFilter="all";
    private string _stageFilter="all";
    private string _sourceFilter="all";

    private EditorMode _editorMode;
    private string? _editingOrderId;
    private string? _editingVersion;
    private string? _saveKey;
    private string? _confirmKey;
    private string? _entrySettingsKey;
    private string? _quickCustomerKey;
    private string _draftCustomerMode="EXISTING";
    private string _draftCustomerId=string.Empty;
    private string _draftWalkInName=string.Empty;
    private string _draftWalkInPhone=string.Empty;
    private string _draftAddressId=string.Empty;
    private string _draftWarehouseId=string.Empty;
    private string _draftSalesChannelId=string.Empty;
    private string _draftPriceSelectionMode="STANDARD";
    private string _draftDeliveryChoice="TRIP";
    private string _draftCollectionPolicy="COLLECT_ON_DELIVERY";
    private string _draftRequestedDeliveryDate=string.Empty;
    private string _draftNote=string.Empty;
    private string _draftDocumentDiscountMode="NONE";
    private string _draftDocumentDiscountValue="0";
    private string _draftDocumentDiscountReason=string.Empty;
    private string _draftCreditOverrideReason=string.Empty;
    private string _customerSearch=string.Empty;
    private string _skuSearch=string.Empty;
    private bool _quickCustomerOpen;
    private string _quickCustomerCode=string.Empty;
    private string _quickCustomerName=string.Empty;
    private string _quickCustomerPhone=string.Empty;
    private bool _rememberWarehouseDefault;
    private bool _rememberDeliveryDefault;
    private SalesDraftLineRow? _selectedDraftLine;
    private string _amendmentReason=string.Empty;
    private string _cancellationReason=string.Empty;
    private string _paidAmount="0";
    private string _paymentMethod="CASH";
    private string _pricingMismatchMessage=string.Empty;
    private bool _isInventoryHistoryOpen;
    private string _inventoryHistorySku=string.Empty;
    private string _inventoryHistorySummary=string.Empty;

    public SalesViewModel(ISalesOrderService service,IPartnerService partnerService,ICanonicalIdempotencyKeyProvider keys,IAccessStateService access,IAuthenticationService authentication)
    {
        _service=service; _partnerService=partnerService; _keys=keys; _access=access; _authentication=authentication;
        _access.Changed += (_, _) => RunOnUiThread(RaisePermissions);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SalesOrderRow> OrderRows { get; }=[];
    public ObservableCollection<SalesOrderLineRow> DetailLineRows { get; }=[];
    public ObservableCollection<SalesOrderVersionRow> VersionRows { get; }=[];
    public ObservableCollection<SalesSkuSearchRow> SkuRows { get; }=[];
    public ObservableCollection<SalesDraftLineRow> DraftLines { get; }=[];
    public ObservableCollection<SalesInventoryHistoryRow> InventoryHistoryRows { get; }=[];
    public ObservableCollection<SalesLookupOption> CustomerOptions { get; }=[];
    public ObservableCollection<SalesLookupOption> AddressOptions { get; }=[];
    public ObservableCollection<SalesLookupOption> WarehouseOptions { get; }=[];
    public ObservableCollection<SalesLookupOption> ChannelOptions { get; }=[];

    public IReadOnlyList<SalesLookupOption> LaneOptions=>SalesPresentation.LaneOptions;
    public IReadOnlyList<SalesLookupOption> StageOptions=>SalesPresentation.StageOptions;
    public IReadOnlyList<SalesLookupOption> SourceOptions=>SalesPresentation.SourceOptions;
    public IReadOnlyList<SalesLookupOption> CustomerModeOptions=>SalesPresentation.CustomerModeOptions;
    public IReadOnlyList<SalesLookupOption> DeliveryChoiceOptions=>SalesPresentation.DeliveryChoiceOptions;
    public IReadOnlyList<SalesLookupOption> CollectionOptions=>SalesPresentation.CollectionOptions;
    public IReadOnlyList<SalesLookupOption> PriceSelectionOptions=>SalesPresentation.PriceSelectionOptions;
    public IReadOnlyList<SalesLookupOption> DiscountModeOptions=>SalesPresentation.DiscountModeOptions;
    public IReadOnlyList<SalesLookupOption> DocumentDiscountModeOptions=>SalesPresentation.DocumentDiscountModeOptions;
    public IReadOnlyList<SalesLookupOption> PaymentMethodOptions=>SalesPresentation.PaymentMethodOptions;

    public bool CanRead=>_access.HasPermission(Read);
    public bool CanCreate=>_access.HasPermission(Create);
    public bool CanUpdateDraft=>_access.HasPermission(UpdateDraft);
    public bool CanConfirmPermission=>_access.HasPermission(Confirm);
    public bool CanAmend=>_access.HasPermission(Amend);
    public bool CanCancelPermission=>_access.HasPermission(Cancel);
    public bool CanPriceOverride=>_access.HasPermission(PriceOverride);
    public bool CanDiscountOverride=>_access.HasPermission(DiscountOverride);
    public bool CanCreditOverride=>_access.HasPermission(CreditOverride);
    public bool CanIssueInventory=>_access.HasPermission(IssueInventory);
    public bool CanRecordPayment=>_access.HasPermission(CustomerPaymentCreate);
    public bool CanQuickCreateCustomer=>_access.HasPermission(CustomerWrite)&&IsEditorOpen&&IsExistingCustomer;
    public bool CanOpenInventoryHistory=>_access.HasPermission(InventoryRead)&&IsEditorOpen;

    public bool IsBusy { get=>_isBusy; private set { if(SetField(ref _isBusy,value)) OnPropertyChanged(nameof(IsNotBusy)); } }
    public bool IsNotBusy=>!IsBusy;
    public string Message { get=>_message; private set { if(SetField(ref _message,value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool MessageIsError { get=>_messageIsError; private set=>SetField(ref _messageIsError,value); }
    public bool HasMessage=>!string.IsNullOrWhiteSpace(Message);

    public string Search { get=>_search; set { if(SetField(ref _search,value)) RefreshRows(); } }
    public string LaneFilter { get=>_laneFilter; set { if(SetField(ref _laneFilter,value)) RefreshRows(); } }
    public string StageFilter { get=>_stageFilter; set { if(SetField(ref _stageFilter,value)) RefreshRows(); } }
    public string SourceFilter { get=>_sourceFilter; set { if(SetField(ref _sourceFilter,value)) RefreshRows(); } }

    public int TotalCount=>_orders.Count;
    public int ActiveCount=>_orders.Count(o=>SalesPresentation.Stage(o)=="active");
    public int PreparingCount=>_orders.Count(o=>SalesPresentation.Stage(o)=="preparing");
    public int WaitingDeliveryCount=>_orders.Count(o=>SalesPresentation.Stage(o)=="waiting_delivery");
    public int CompletedCount=>_orders.Count(o=>SalesPresentation.Stage(o)=="completed");

    public bool HasSelectedOrder=>_selectedOrder is not null;
    public string SelectedNumber=>SalesPresentation.Number(_selectedOrder?.Number);
    public string SelectedCustomer=>_selectedOrder is null ? "Chưa chọn đơn" : $"{_selectedOrder.CustomerCode} · {_selectedOrder.CustomerName}";
    public string SelectedWarehouse=>_selectedOrder is null ? "—" : $"{_selectedOrder.WarehouseCode} · {_selectedOrder.WarehouseName}";
    public string SelectedLane=>_selectedOrder is null ? "—" : SalesPresentation.LaneLabel(_selectedOrder);
    public string SelectedOrderStatus=>_selectedOrder is null ? "—" : SalesPresentation.OrderStatus(_selectedOrder.Status);
    public string SelectedFulfillmentStatus=>_selectedOrder is null ? "—" : SalesPresentation.FulfillmentStatus(_selectedOrder.FulfillmentStatus);
    public string SelectedDeliveryStatus=>_selectedOrder is null ? "—" : SalesPresentation.DeliveryStatus(_selectedOrder.DeliveryStatus,_selectedOrder.FulfillmentStatus);
    public string SelectedSettlementStatus=>_selectedOrder is null ? "—" : SalesPresentation.SettlementStatus(_selectedOrder.SettlementStatus);
    public string SelectedTotal=>SalesPresentation.Money(CurrentVersion?.Total);
    public string SelectedCollection=>_selectedOrder is null ? "—" : SalesPresentation.CollectionPolicy(_selectedOrder.CollectionPolicy);
    public string SelectedReceivable=>_selectedOrder is null ? "—" : SalesPresentation.Money(_selectedOrder.ReceivableRemainingAmount);
    public SalesOrderData? SelectedOrder=>_selectedOrder;
    public SalesOrderVersionData? SelectedVersion=>CurrentVersion;

    private SalesOrderVersionData? CurrentVersion=>SalesPresentation.ActiveVersion(_selectedOrder);
    private SalesOrderVersionData? PendingVersion=>SalesPresentation.PendingVersion(_selectedOrder);
    private bool IsManualDirect=>CurrentVersion?.DeliveryMode=="DELIVERY"&&CurrentVersion.DeliveryExecutionMode=="MANUAL";
    private bool IsPickupDirect=>CurrentVersion?.DeliveryMode=="PICKUP";
    private bool IsDirectOrder=>IsManualDirect||IsPickupDirect;
    private bool DirectStockReady=>_selectedOrder?.FulfillmentStatus is "issued" or "fulfilled";

    public bool CanEditSelectedDraft=>_selectedOrder?.Status=="draft"&&CanUpdateDraft;
    public bool CanConfirmSelected=>_selectedOrder?.Status=="draft"&&CanConfirmPermission;
    public bool CanCreateAmendment=>_selectedOrder?.Status=="confirmed"&&!IsManualDirect&&PendingVersion is null&&CanAmend;
    public bool CanEditAmendment=>_selectedOrder?.Status=="confirmed"&&PendingVersion is not null&&CanAmend;
    public bool CanConfirmAmendment=>CanEditAmendment;
    public bool CanEditManual=>_selectedOrder?.Status=="confirmed"&&IsManualDirect&&PendingVersion is null&&CanAmend&&!DirectStockReady;
    public bool CanIssueSelectedStock=>_selectedOrder?.Status=="confirmed"&&IsDirectOrder&&CanIssueInventory&&!DirectStockReady;
    public bool CanCancelSelected=>_selectedOrder is { Status:"draft" or "confirmed" }&&CanCancelPermission&&(!IsDirectOrder||!DirectStockReady);
    public bool CanCloseExecution=>_selectedOrder?.Status=="confirmed"&&!IsDirectOrder&&CanCancelPermission&&_selectedOrder.DeliveryStatus is "partially_delivered" or "failed";
    public bool CanCompleteDirect=>_selectedOrder?.Status=="confirmed"&&IsDirectOrder&&DirectStockReady&&CanConfirmPermission;
    public bool CanSettleDirect=>_selectedOrder?.Status=="closed"&&IsDirectOrder&&DirectStockReady&&CanRecordPayment&&_selectedOrder.SettlementStatus is "pending" or "partially_paid";
    public bool ShowDirectSettlement=>IsDirectOrder&&DirectStockReady&&_selectedOrder is { Status:"confirmed" or "closed" };
    public bool CanCopySelected=>CanCreate&&_selectedOrder is not null&&CurrentVersion is not null;
    public bool CanPrintSelected=>_selectedOrder is not null&&CurrentVersion is not null&&!string.IsNullOrWhiteSpace(_selectedOrder.Number)&&_selectedOrder.Status is "confirmed" or "closed" or "cancelled";

    public string AmendmentReason { get=>_amendmentReason; set=>SetField(ref _amendmentReason,value); }
    public string CancellationReason { get=>_cancellationReason; set=>SetField(ref _cancellationReason,value); }
    public string PaidAmount { get=>_paidAmount; set=>SetField(ref _paidAmount,value); }
    public string PaymentMethod { get=>_paymentMethod; set=>SetField(ref _paymentMethod,value); }

    public bool IsEditorOpen=>_editorMode!=EditorMode.None;
    public bool IsCreateEditor=>_editorMode==EditorMode.Create;
    public string EditorTitle=>_editorMode switch { EditorMode.Create=>"Tạo đơn bán hàng",EditorMode.Draft=>"Sửa đơn nháp",EditorMode.Amendment=>"Sửa bản điều chỉnh",EditorMode.ManualEdit=>"Sửa đơn Giao thủ công",_=>string.Empty };
    public bool CanEditorConfirm=>_editorMode switch { EditorMode.Create or EditorMode.Draft=>CanConfirmPermission,EditorMode.Amendment=>CanAmend,_=>false };
    public string PricingMismatchMessage { get=>_pricingMismatchMessage; private set { if(SetField(ref _pricingMismatchMessage,value))OnPropertyChanged(nameof(HasPricingMismatch)); } }
    public bool HasPricingMismatch=>!string.IsNullOrWhiteSpace(PricingMismatchMessage);
    public bool IsInventoryHistoryOpen { get=>_isInventoryHistoryOpen; private set=>SetField(ref _isInventoryHistoryOpen,value); }
    public string InventoryHistorySku { get=>_inventoryHistorySku; private set=>SetField(ref _inventoryHistorySku,value); }
    public string InventoryHistorySummary { get=>_inventoryHistorySummary; private set=>SetField(ref _inventoryHistorySummary,value); }

    public string DraftCustomerMode { get=>_draftCustomerMode; set { if(SetField(ref _draftCustomerMode,value)) { if(value=="WALK_IN"){ DraftCustomerId=string.Empty; DraftDeliveryChoice="PICKUP"; DraftPriceSelectionMode="STANDARD"; CloseQuickCustomer(); } RefreshCustomerOptions(); OnPropertyChanged(nameof(IsExistingCustomer)); OnPropertyChanged(nameof(IsWalkInCustomer)); OnPropertyChanged(nameof(CanQuickCreateCustomer)); OnPropertyChanged(nameof(ShowCustomerResults)); } } }
    public bool IsExistingCustomer=>DraftCustomerMode=="EXISTING";
    public bool IsWalkInCustomer=>DraftCustomerMode=="WALK_IN";
    public string DraftCustomerId { get=>_draftCustomerId; set { if(SetField(ref _draftCustomerId,value)) { _=ReloadAddressesAsync(value); OnPropertyChanged(nameof(HasSelectedCustomer)); OnPropertyChanged(nameof(SelectedCustomerSummary)); } } }
    public bool HasSelectedCustomer=>IsExistingCustomer&&!string.IsNullOrWhiteSpace(DraftCustomerId)&&_customers.Any(x=>x.Id==DraftCustomerId);
    public string SelectedCustomerSummary
    {
        get
        {
            var customer=_customers.FirstOrDefault(x=>x.Id==DraftCustomerId);
            return customer is null?"Chưa chọn khách hàng":$"{customer.Code} · {customer.Name}{(string.IsNullOrWhiteSpace(customer.Phone)?string.Empty:$" · {customer.Phone}")}";
        }
    }
    public string DraftWalkInName { get=>_draftWalkInName; set=>SetField(ref _draftWalkInName,value); }
    public string DraftWalkInPhone { get=>_draftWalkInPhone; set=>SetField(ref _draftWalkInPhone,value); }
    public string DraftAddressId { get=>_draftAddressId; set=>SetField(ref _draftAddressId,value); }
    public string DraftWarehouseId { get=>_draftWarehouseId; set=>SetField(ref _draftWarehouseId,value); }
    public string DraftSalesChannelId { get=>_draftSalesChannelId; set=>SetField(ref _draftSalesChannelId,value); }
    public string DraftPriceSelectionMode { get=>_draftPriceSelectionMode; set=>SetField(ref _draftPriceSelectionMode,value); }
    public string DraftDeliveryChoice { get=>_draftDeliveryChoice; set=>SetField(ref _draftDeliveryChoice,value); }
    public string DraftCollectionPolicy { get=>_draftCollectionPolicy; set=>SetField(ref _draftCollectionPolicy,value); }
    public string DraftRequestedDeliveryDate { get=>_draftRequestedDeliveryDate; set=>SetField(ref _draftRequestedDeliveryDate,value); }
    public string DraftNote { get=>_draftNote; set=>SetField(ref _draftNote,value); }
    public string DraftDocumentDiscountMode { get=>_draftDocumentDiscountMode; set { if(SetField(ref _draftDocumentDiscountMode,value)){RaiseDraftTotals();OnPropertyChanged(nameof(CanEditLineDiscount));} } }
    public string DraftDocumentDiscountValue { get=>_draftDocumentDiscountValue; set { if(SetField(ref _draftDocumentDiscountValue,value)){RaiseDraftTotals();OnPropertyChanged(nameof(CanEditLineDiscount));} } }
    public string DraftDocumentDiscountReason { get=>_draftDocumentDiscountReason; set=>SetField(ref _draftDocumentDiscountReason,value); }
    public string DraftCreditOverrideReason { get=>_draftCreditOverrideReason; set=>SetField(ref _draftCreditOverrideReason,value); }
    public string CustomerSearch { get=>_customerSearch; set { if(SetField(ref _customerSearch,value)){RefreshCustomerOptions();OnPropertyChanged(nameof(ShowCustomerResults));} } }
    public bool ShowCustomerResults=>IsEditorOpen&&IsExistingCustomer&&!string.IsNullOrWhiteSpace(CustomerSearch)&&CustomerOptions.Count>0;
    public string SkuSearch { get=>_skuSearch; set { if(SetField(ref _skuSearch,value)){PopulateLocalSkuCandidates();OnPropertyChanged(nameof(ShowSkuResults));} } }
    public bool ShowSkuResults=>IsEditorOpen&&!string.IsNullOrWhiteSpace(SkuSearch)&&SkuRows.Count>0;
    public bool CanEditLineDiscount=>CanDiscountOverride&&(DraftDocumentDiscountMode=="NONE"||!decimal.TryParse(DraftDocumentDiscountValue,NumberStyles.Number,CultureInfo.InvariantCulture,out var documentDiscount)||documentDiscount==0m);
    public bool QuickCustomerOpen { get=>_quickCustomerOpen; private set=>SetField(ref _quickCustomerOpen,value); }
    public string QuickCustomerCode { get=>_quickCustomerCode; set=>SetField(ref _quickCustomerCode,value); }
    public string QuickCustomerName { get=>_quickCustomerName; set=>SetField(ref _quickCustomerName,value); }
    public string QuickCustomerPhone { get=>_quickCustomerPhone; set=>SetField(ref _quickCustomerPhone,value); }
    public bool RememberWarehouseDefault { get=>_rememberWarehouseDefault; set=>SetField(ref _rememberWarehouseDefault,value); }
    public bool RememberDeliveryDefault { get=>_rememberDeliveryDefault; set=>SetField(ref _rememberDeliveryDefault,value); }
    public SalesDraftLineRow? SelectedDraftLine { get=>_selectedDraftLine; set=>SetField(ref _selectedDraftLine,value); }
    private SalesDraftEstimateResult DraftEstimate=>SalesPresentation.EstimateDraft(DraftLines.ToArray(),DraftDocumentDiscountMode,DraftDocumentDiscountValue);
    public string DraftGrossText=>SalesPresentation.Money(DraftEstimate.Gross.ToString(CultureInfo.InvariantCulture));
    public string DraftDiscountText=>SalesPresentation.Money(DraftEstimate.Discount.ToString(CultureInfo.InvariantCulture));
    public string DraftTaxText=>SalesPresentation.Money(DraftEstimate.Tax.ToString(CultureInfo.InvariantCulture));
    public string DraftTotalText=>SalesPresentation.Money(DraftEstimate.Total.ToString(CultureInfo.InvariantCulture));

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken=default)
    { if(!_isLoaded&&CanRead) await RefreshAsync(cancellationToken).ConfigureAwait(true); }

    public async Task RefreshAsync(CancellationToken cancellationToken=default)
    {
        if(!CanRead||IsBusy)return;
        IsBusy=true;
        try
        {
            var ordersTask=_service.ListAsync(cancellationToken);
            var settingsTask=_service.GetEntrySettingsAsync(cancellationToken);
            var customersTask=_service.ListCustomersAsync(cancellationToken);
            var warehousesTask=_service.ListWarehousesAsync(cancellationToken);
            var catalogTask=WarmSkuCatalogAsync(cancellationToken);
            await Task.WhenAll(ordersTask,settingsTask,customersTask,warehousesTask,catalogTask).ConfigureAwait(true);
            Replace(_orders,await ordersTask.ConfigureAwait(true));
            _entrySettings=await settingsTask.ConfigureAwait(true);
            Replace(_customers,(await customersTask.ConfigureAwait(true)).Where(c=>c.IsActive));
            Replace(_warehouses,(await warehousesTask.ConfigureAwait(true)).Where(w=>w.IsActive));
            BuildLookups(); RefreshRows(); _isLoaded=true; SetMessage("Đã cập nhật dữ liệu đơn bán hàng.",false);
        }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    public async Task SelectOrderAsync(string id,CancellationToken cancellationToken=default)
    {
        if(!CanRead||IsBusy)return;
        IsBusy=true;
        try { _selectedOrder=await _service.GetAsync(id,cancellationToken).ConfigureAwait(true); MergeOrder(_selectedOrder); RefreshSelected(); SetMessage(string.Empty,false); }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    public void OpenCreateEditor()
    {
        if(!CanCreate||_entrySettings is null)return;
        OpenEditor(EditorMode.Create,null,null,"sales-order-create");
        DraftCustomerMode="EXISTING"; CustomerSearch=string.Empty; DraftCustomerId=string.Empty; DraftWalkInName=string.Empty; DraftWalkInPhone=string.Empty; DraftAddressId=string.Empty;
        DraftWarehouseId=_entrySettings.SavedWarehouseId??_entrySettings.DefaultWarehouseId??_warehouses.FirstOrDefault()?.Id??string.Empty;
        DraftSalesChannelId=_entrySettings.DefaultSalesChannelId??_entrySettings.SalesChannels.FirstOrDefault()?.Id??string.Empty;
        DraftPriceSelectionMode="STANDARD"; ApplyDeliveryChoice(_entrySettings.SavedDeliveryChoice??_entrySettings.DefaultDeliveryChoice);
        DraftCollectionPolicy="COLLECT_ON_DELIVERY"; DraftRequestedDeliveryDate=string.Empty; DraftNote=string.Empty;
        DraftDocumentDiscountMode="NONE"; DraftDocumentDiscountValue="0"; DraftDocumentDiscountReason=string.Empty; DraftCreditOverrideReason=string.Empty;
        RememberWarehouseDefault=false; RememberDeliveryDefault=false; CloseQuickCustomer(); ClearDraftLines(); SkuRows.Clear(); _skuOptions.Clear(); SkuSearch=string.Empty; SelectedDraftLine=null;
        PricingMismatchMessage=string.Empty; CloseInventoryHistory(); RaiseDraftTotals(); CaptureEditorBaseline();
    }

    public void OpenCopyEditor()
    {
        if(!CanCopySelected||CurrentVersion is null)return;
        OpenEditor(EditorMode.Create,null,null,"sales-order-create");
        ApplyVersionToEditor(CurrentVersion,true);
    }

    public void OpenDraftEditor(){if(CanEditSelectedDraft&&CurrentVersion is not null)OpenEditorFromVersion(EditorMode.Draft,CurrentVersion,"sales-order-draft-update");}
    public void OpenAmendmentEditor(){if(CanEditAmendment&&PendingVersion is not null)OpenEditorFromVersion(EditorMode.Amendment,PendingVersion,"sales-order-amendment-update");}
    public void OpenManualEditor(){if(CanEditManual&&CurrentVersion is not null)OpenEditorFromVersion(EditorMode.ManualEdit,CurrentVersion,"sales-order-manual-edit");}

    public void CancelEditor()
    {
        _editorMode=EditorMode.None; _editingOrderId=null; _editingVersion=null; _saveKey=null; _confirmKey=null; _entrySettingsKey=null; _quickCustomerKey=null;
        _editorBaselineFingerprint=string.Empty; _editorPricingAt=string.Empty; PricingMismatchMessage=string.Empty; CloseInventoryHistory();
        ClearDraftLines(); SkuRows.Clear(); _skuOptions.Clear(); SelectedDraftLine=null; CloseQuickCustomer(); RaiseEditorState(); RaiseDraftTotals();
    }

    public bool HasUnsavedEditorChanges()=>IsEditorOpen&&!string.IsNullOrWhiteSpace(_editorBaselineFingerprint)&&EditorFingerprint()!=_editorBaselineFingerprint;

    public void SelectCustomer(string id)
    {
        if(!IsExistingCustomer||string.IsNullOrWhiteSpace(id))return;
        DraftCustomerId=id;
        CustomerSearch=string.Empty;
        OnPropertyChanged(nameof(ShowCustomerResults));
    }

    public async Task SearchCustomersAsync(CancellationToken cancellationToken=default)
    {
        if(!IsEditorOpen||!IsExistingCustomer)return;
        var term=CustomerSearch.Trim(); RefreshCustomerOptions(); if(term.Length<2)return;
        try
        {
            var rows=await _service.SearchCustomersAsync(term,cancellationToken).ConfigureAwait(true);
            foreach(var row in rows.Where(x=>x.IsActive))MergeCustomer(row,false);
            RefreshCustomerOptions();
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){ }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
    }

    public void OpenQuickCustomer()
    {
        if(!CanQuickCreateCustomer)return;
        QuickCustomerCode=AutoCustomerCode(); QuickCustomerName=string.Empty; QuickCustomerPhone=CustomerSearch.Trim();
        _quickCustomerKey=_keys.Create("sales-quick-customer"); QuickCustomerOpen=true;
    }

    public void CloseQuickCustomer()=>QuickCustomerOpen=false;

    public async Task CreateQuickCustomerAsync(CancellationToken cancellationToken=default)
    {
        if(!CanQuickCreateCustomer||IsBusy)return;
        if(string.IsNullOrWhiteSpace(QuickCustomerName)){SetMessage("Hãy nhập tên khách hàng.",true);return;}
        if(string.IsNullOrWhiteSpace(QuickCustomerCode)){SetMessage("Hãy nhập mã khách hàng.",true);return;}
        IsBusy=true;
        try
        {
            var phone=NormalizePhone(QuickCustomerPhone);
            if(phone.Length>0)
            {
                var matches=await _service.SearchCustomersAsync(phone,cancellationToken).ConfigureAwait(true);
                var duplicate=matches.FirstOrDefault(x=>NormalizePhone(x.Phone)==phone);
                if(duplicate is not null)
                {
                    MergeCustomer(duplicate,true); SelectCustomer(duplicate.Id);
                    CloseQuickCustomer(); SetMessage($"Số điện thoại đã thuộc khách {duplicate.Code} · {duplicate.Name}; đã chọn khách này thay vì tạo trùng.",false); return;
                }
            }
            var created=await _partnerService.CreateCustomerAsync(new CustomerCreateRequest(
                QuickCustomerCode.Trim().ToUpperInvariant(),QuickCustomerName.Trim(),null,null,string.IsNullOrWhiteSpace(QuickCustomerPhone)?null:QuickCustomerPhone.Trim(),
                null,null,0,"0","Tạo nhanh từ màn hình đơn bán hàng."),RequireQuickCustomerKey(),cancellationToken).ConfigureAwait(true);
            _quickCustomerKey=null; MergeCustomer(created,true); SelectCustomer(created.Id);
            CloseQuickCustomer(); SetMessage($"Đã tạo và chọn khách {created.Code} · {created.Name}.",false);
        }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    public async Task SearchSkuAsync(CancellationToken cancellationToken=default)
    {
        if(!IsEditorOpen)return;
        var term=SkuSearch.Trim();
        if(term.Length==0){SkuRows.Clear();_skuOptions.Clear();return;}
        if(string.IsNullOrWhiteSpace(DraftWarehouseId)||string.IsNullOrWhiteSpace(DraftSalesChannelId))return;
        try
        {
            var pricingAt=CurrentPricingAt();
            var customerId=IsExistingCustomer&&!string.IsNullOrWhiteSpace(DraftCustomerId)?DraftCustomerId:null;
            var local=_skuCatalog.Search(term,30);
            IReadOnlyList<SalesOrderSkuSearchOptionData> rows=local.Count>0?local.Select(CatalogSearchOption).ToArray():
                await _service.SearchSkuAsync(term,DraftWarehouseId,DraftSalesChannelId,customerId,DraftPriceSelectionMode,pricingAt,cancellationToken).ConfigureAwait(true);
            var previews=await _service.GetSkuPreviewsAsync(rows.Select(r=>r.Id).ToArray(),DraftWarehouseId,DraftSalesChannelId,customerId,DraftPriceSelectionMode,pricingAt,cancellationToken).ConfigureAwait(true);
            if(cancellationToken.IsCancellationRequested)return;
            SetSkuRows(rows,previews); if(SkuRows.Count==0)SetMessage("Không tìm thấy hàng phù hợp.",false);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){ }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
    }

    public Task<SalesDraftLineRow?> AddSkuAsync(string id,CancellationToken cancellationToken=default)
    {
        var option=_skuOptions.FirstOrDefault(x=>x.Id==id);
        if(option is null||!option.Eligibility.Selectable)return Task.FromResult<SalesDraftLineRow?>(null);
        var existing=DraftLines.FirstOrDefault(x=>x.VariantId==id);
        if(existing is not null)
        {
            SelectedDraftLine=existing; SkuSearch=string.Empty; SkuRows.Clear(); _skuOptions.Clear(); OnPropertyChanged(nameof(ShowSkuResults));
            SetMessage("Hàng này đã có trong đơn. Dùng Tách dòng nếu cần thêm dòng riêng.",false);
            return Task.FromResult<SalesDraftLineRow?>(existing);
        }
        var found=SkuRows.FirstOrDefault(x=>x.Id==id);
        var line=new SalesDraftLineRow(
            option.Id,option.Sku,option.ProductName,option.UnitName??option.UnitCode??"—",
            option.DefaultTaxMode,option.DefaultTaxRate,"1",found?.PriceMinor??"0",string.Empty,string.Empty,"PERCENT","0",
            option.ProductId,option.ConversionToBase??"1",option.AllowsFractional??false);
        AddDraftLine(line); SelectedDraftLine=line; SkuSearch=string.Empty; SkuRows.Clear(); _skuOptions.Clear(); OnPropertyChanged(nameof(ShowSkuResults));
        _=LoadLineVariantsAsync(line,cancellationToken);
        _=RepriceLineAsync(line,cancellationToken);
        SetMessage($"Đã thêm {option.Sku}; nhập số lượng để tiếp tục.",false);
        return Task.FromResult<SalesDraftLineRow?>(line);
    }

    public async Task LoadLineVariantsAsync(SalesDraftLineRow line,CancellationToken cancellationToken=default)
    {
        if(!DraftLines.Contains(line))return;
        var productId=line.ProductId;
        if(string.IsNullOrWhiteSpace(productId))
        {
            var local=_skuCatalog.Search(line.Sku,50).FirstOrDefault(x=>x.Id==line.VariantId)
                ??_skuCatalog.Search(line.Sku,50).FirstOrDefault(x=>x.Sku.Equals(line.Sku,StringComparison.OrdinalIgnoreCase));
            if(local is not null)
            {
                productId=local.ProductId;
                line.SetProductIdentity(local.ProductId,local.ConversionToBase,local.AllowsFractional);
            }
            else if(!string.IsNullOrWhiteSpace(DraftWarehouseId)&&!string.IsNullOrWhiteSpace(DraftSalesChannelId))
            {
                try
                {
                    var rows=await _service.SearchSkuAsync(line.Sku,DraftWarehouseId,DraftSalesChannelId,
                        IsExistingCustomer&&!string.IsNullOrWhiteSpace(DraftCustomerId)?DraftCustomerId:null,
                        DraftPriceSelectionMode,CurrentPricingAt(),cancellationToken).ConfigureAwait(true);
                    var current=rows.FirstOrDefault(x=>x.Id==line.VariantId)??rows.FirstOrDefault(x=>x.Sku.Equals(line.Sku,StringComparison.OrdinalIgnoreCase));
                    if(current is not null)
                    {
                        productId=current.ProductId;
                        line.SetProductIdentity(current.ProductId,current.ConversionToBase,current.AllowsFractional);
                    }
                }
                catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){return;}
            }
        }
        if(string.IsNullOrWhiteSpace(productId))return;
        try
        {
            if(!_productVariantCache.TryGetValue(productId,out var variants))
            {
                variants=(await _service.ListProductVariantsAsync(productId,cancellationToken).ConfigureAwait(true))
                    .Where(v=>v.ProductId==productId&&v.IsActive&&v.IsSellable&&v.VariantKind is "BASE" or "CARTON"
                        &&!string.IsNullOrWhiteSpace(v.UnitId)&&!string.IsNullOrWhiteSpace(v.UnitCode)&&!string.IsNullOrWhiteSpace(v.ConversionToBase))
                    .OrderBy(v=>v.VariantKind=="BASE"?0:1).ThenBy(v=>v.Sku,StringComparer.OrdinalIgnoreCase).ToArray();
                _productVariantCache[productId]=variants;
            }
            line.SetUnitOptions(variants);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){ }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
    }

    public async Task ChangeLineVariantAsync(SalesDraftLineRow line,string nextVariantId,CancellationToken cancellationToken=default)
    {
        if(!DraftLines.Contains(line)||string.IsNullOrWhiteSpace(nextVariantId)||nextVariantId==line.VariantId)return;
        await LoadLineVariantsAsync(line,cancellationToken).ConfigureAwait(true);
        if(string.IsNullOrWhiteSpace(line.ProductId)||!_productVariantCache.TryGetValue(line.ProductId,out var variants))return;
        var variant=variants.FirstOrDefault(v=>v.Id==nextVariantId);
        if(variant is null){SetMessage("ĐVT đã chọn không còn hợp lệ để bán.",true);return;}
        if(line.ApplyVariant(variant))await RepriceLineAsync(line,cancellationToken).ConfigureAwait(true);
        RaiseDraftTotals();
    }

    public async Task RepriceLineAsync(SalesDraftLineRow line,CancellationToken cancellationToken=default,string? pricingAt=null)
    {
        if(!DraftLines.Contains(line)||!TryPositive(line.Quantity)||string.IsNullOrWhiteSpace(DraftSalesChannelId))return;
        var generation=_linePricingGeneration.TryGetValue(line,out var current)?current+1:1;
        _linePricingGeneration[line]=generation;
        var effectiveAt=pricingAt??CurrentPricingAt();
        try
        {
            var customerId=IsExistingCustomer&&!string.IsNullOrWhiteSpace(DraftCustomerId)?DraftCustomerId:null;
            var resolution=await _service.ResolvePriceAsync(new SalesPricePreviewRequest(
                line.VariantId,Positive(line.Quantity),DraftSalesChannelId,DraftPriceSelectionMode,
                effectiveAt,customerId),cancellationToken).ConfigureAwait(true);
            if(!_linePricingGeneration.TryGetValue(line,out var latest)||latest!=generation||!DraftLines.Contains(line))return;
            line.ApplyPricingResolution(resolution);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){ }
        catch(CanonicalApiException api)
        {
            line.SetPricingError(api.Code,CanonicalErrorMessages.ToOfficeMessage(api));
            await HandleFailureAsync(api,cancellationToken).ConfigureAwait(true);
        }
        catch(Exception ex){line.SetPricingError("PRICE_PREVIEW_FAILED",ex.Message);await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        RaiseDraftTotals();
    }

    public async Task RepriceAllAsync(string? pricingAt=null,CancellationToken cancellationToken=default)
    {
        var effectiveAt=pricingAt??CurrentPricingAt();
        foreach(var line in DraftLines.ToArray())await RepriceLineAsync(line,cancellationToken,effectiveAt).ConfigureAwait(true);
        RaiseDraftTotals();
    }

    public async Task<SalesDraftLineRow?> SplitLineAsync(SalesDraftLineRow source,CancellationToken cancellationToken=default)
    {
        if(!CanPriceOverride){SetMessage("Cần quyền Sửa giá bán trên đơn để tách dòng.",true);return null;}
        if(!DraftLines.Contains(source))return null;
        var split=SalesPresentation.SplitDraftLine(source);
        var sourceIndex=DraftLines.IndexOf(source);
        split.PropertyChanged+=DraftLineOnPropertyChanged;
        DraftLines.Insert(Math.Max(0,sourceIndex),split);
        SelectedDraftLine=split; RaiseDraftTotals();
        await RepriceLineAsync(split,cancellationToken,CurrentPricingAt()).ConfigureAwait(true);
        SetMessage($"Đã tách dòng {source.Sku}.",false);
        return split;
    }

    public async Task AdjustQuantityAsync(SalesDraftLineRow line,int delta,CancellationToken cancellationToken=default)
    {
        if(!DraftLines.Contains(line))return;
        var current=decimal.TryParse(line.Quantity,NumberStyles.Number,CultureInfo.InvariantCulture,out var value)&&value>0?value:1m;
        var next=current+delta;
        if(next<=0)next=1m;
        line.Quantity=next.ToString("0.############################",CultureInfo.InvariantCulture);
        await RepriceLineAsync(line,cancellationToken).ConfigureAwait(true);
    }

    public void UseSystemPrice(SalesDraftLineRow line)
    {
        if(!DraftLines.Contains(line))return;
        line.UseSystemPrice(); RaiseDraftTotals();
    }

    public void RemoveDraftLine(string clientLineId)
    {
        var row=DraftLines.FirstOrDefault(x=>x.ClientLineId==clientLineId); if(row is null)return;
        row.PropertyChanged-=DraftLineOnPropertyChanged; DraftLines.Remove(row); _linePricingGeneration.Remove(row);
        if(ReferenceEquals(SelectedDraftLine,row))SelectedDraftLine=DraftLines.FirstOrDefault();
        RaiseDraftTotals(); OnPropertyChanged(nameof(CanEditLineDiscount));
    }

    public void TogglePriceDetail(SalesDraftLineRow line){if(DraftLines.Contains(line))line.TogglePriceDetail();}

    public async Task OpenInventoryHistoryAsync(SalesDraftLineRow line,CancellationToken cancellationToken=default)
    {
        if(!CanOpenInventoryHistory){SetMessage("Tài khoản chưa được cấp quyền xem lịch sử xuất nhập tồn.",true);return;}
        if(string.IsNullOrWhiteSpace(DraftWarehouseId)){SetMessage("Hãy chọn kho xuất trước khi xem lịch sử XNT.",true);return;}
        if(IsBusy)return;
        IsBusy=true;
        try
        {
            var balances=await _service.ListInventoryBalancesAsync(DraftWarehouseId,cancellationToken).ConfigureAwait(true);
            var balance=balances.FirstOrDefault(x=>x.BaseVariantId==line.VariantId)
                ??balances.FirstOrDefault(x=>x.BaseSku.Equals(line.Sku,StringComparison.OrdinalIgnoreCase)
                    ||string.Equals(x.PackageSku,line.Sku,StringComparison.OrdinalIgnoreCase));
            if(balance is null){SetMessage($"Chưa tìm thấy tồn kho của SKU {line.Sku} tại kho đang chọn.",true);return;}
            var rows=await _service.ListInventoryHistoryAsync(DraftWarehouseId,balance.BaseVariantId,100,0,cancellationToken).ConfigureAwait(true);
            var unit=balance.BaseUnitName??balance.BaseUnitSymbol??balance.BaseUnitCode??line.UnitCode;
            Reset(InventoryHistoryRows,rows.Select(row=>SalesPresentation.InventoryHistoryRow(row,unit)));
            InventoryHistorySku=line.Sku;
            InventoryHistorySummary=$"{balance.WarehouseCode} · {balance.WarehouseName} · {InventoryHistoryRows.Count} biến động gần nhất";
            IsInventoryHistoryOpen=true;
        }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    public void CloseInventoryHistory()
    {
        IsInventoryHistoryOpen=false; InventoryHistorySku=string.Empty; InventoryHistorySummary=string.Empty; InventoryHistoryRows.Clear();
    }

    public async Task SaveEditorAsync(bool confirmAfter,CancellationToken cancellationToken=default)
    {
        if(!IsEditorOpen||IsBusy)return;
        if(!ValidateEditor(out var error)){SetMessage(error,true);return;}
        IsBusy=true;
        try
        {
            PricingMismatchMessage=string.Empty;
            var request=await BuildDraftRequestAsync(cancellationToken).ConfigureAwait(true);
            var saved=_editorMode switch
            {
                EditorMode.Create=>await _service.CreateDraftAsync(request,RequireSaveKey(),cancellationToken).ConfigureAwait(true),
                EditorMode.Draft=>await _service.UpdateDraftAsync(_editingOrderId!,request,RequireSaveKey(),cancellationToken).ConfigureAwait(true),
                EditorMode.Amendment=>await _service.UpdateAmendmentAsync(_editingOrderId!,_editingVersion!,request,RequireSaveKey(),cancellationToken).ConfigureAwait(true),
                EditorMode.ManualEdit=>await _service.ManualEditAsync(_editingOrderId!,request,RequireSaveKey(),cancellationToken).ConfigureAwait(true),
                _=>throw new InvalidOperationException("Trình soạn đơn không còn hiệu lực.")
            };
            _saveKey=null;
            if(confirmAfter)
            {
                saved=_editorMode==EditorMode.Amendment
                    ? await _service.ConfirmAmendmentAsync(saved.Id,_editingVersion!,RequireConfirmKey(),cancellationToken).ConfigureAwait(true)
                    : await _service.ConfirmAsync(saved.Id,RequireConfirmKey(),cancellationToken).ConfigureAwait(true);
                _confirmKey=null;
            }
            var settingsWarning=await PersistEntryDefaultsAsync(cancellationToken).ConfigureAwait(true);
            MergeOrder(saved); _selectedOrder=saved; RefreshRows(); RefreshSelected();
            var message=confirmAfter?"Đã lưu, xác nhận và cấp số đơn bán hàng.":_editorMode==EditorMode.Create?"Đã tạo đơn bán hàng nháp.":"Đã lưu thay đổi đơn bán hàng.";
            CancelEditor(); SetMessage(settingsWarning is null?message:$"{message} Chưa lưu được lựa chọn mặc định: {settingsWarning}",settingsWarning is not null);
        }
        catch(CanonicalApiException api) when(api.Code=="SALES_PRICE_CHANGED")
        {
            PricingMismatchMessage="Giá hệ thống đã thay đổi. Công Ty đã tính lại; hãy xem từng dòng rồi lưu lại.";
            _editorPricingAt=DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture);
            ResetEditorMutationKeys();
            await RepriceAllAsync(_editorPricingAt,cancellationToken).ConfigureAwait(true);
            SetMessage(CanonicalErrorMessages.WithRequestId("Giá hệ thống đã thay đổi; cần kiểm tra lại trước khi lưu.",api.RequestId),true);
        }
        catch(Exception ex){await HandleFailureAsync(ex,cancellationToken).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    public Task ConfirmSelectedAsync(CancellationToken ct=default)=>
        CanConfirmSelected&&_selectedOrder is not null
            ? RunActionAsync("sales-confirm",$"{_selectedOrder.Id}:{_selectedOrder.Revision}",key=>_service.ConfirmAsync(_selectedOrder.Id,key,ct),"Đã xác nhận và cấp số đơn bán hàng.",ct)
            : Task.CompletedTask;

    public async Task CreateAmendmentAsync(CancellationToken ct=default)
    {
        if(!CanCreateAmendment||_selectedOrder is null)return;
        if(string.IsNullOrWhiteSpace(AmendmentReason)){SetMessage("Hãy nhập lý do điều chỉnh.",true);return;}
        var reason=AmendmentReason.Trim();
        await RunActionAsync("sales-amend",$"{_selectedOrder.Id}:{_selectedOrder.Revision}:{reason}",key=>_service.CreateAmendmentAsync(_selectedOrder.Id,reason,key,ct),"Đã tạo bản điều chỉnh nháp.",ct);
        AmendmentReason=string.Empty;
    }

    public Task ConfirmAmendmentSelectedAsync(CancellationToken ct=default)
    {
        if(!CanConfirmAmendment||_selectedOrder is null||PendingVersion is null)return Task.CompletedTask;
        var version=PendingVersion.VersionNumber;
        return RunActionAsync("sales-amend-confirm",$"{_selectedOrder.Id}:{version}:{PendingVersion.Revision}",key=>_service.ConfirmAmendmentAsync(_selectedOrder.Id,version,key,ct),"Đã xác nhận bản điều chỉnh.",ct);
    }

    public Task IssueStockAsync(CancellationToken ct=default)
    {
        if(!CanIssueSelectedStock||_selectedOrder is null||CurrentVersion is null)return Task.CompletedTask;
        var pickup=IsPickupDirect;
        var action=pickup?"pickup-stock-issue":"sales-manual-stock-issue";
        var message=pickup?"Đã xuất kho đơn Mua tại quầy.":"Đã xuất kho đơn Giao thủ công.";
        return RunActionAsync(action,$"{_selectedOrder.Id}:{CurrentVersion.Revision}",key=>_service.IssueStockAsync(_selectedOrder.Id,CurrentVersion.Revision,pickup?"PICKUP":null,key,ct),message,ct);
    }

    public async Task CancelSelectedAsync(CancellationToken ct=default)
    {
        if(!CanCancelSelected||_selectedOrder is null)return;
        if(string.IsNullOrWhiteSpace(CancellationReason)){SetMessage("Hãy nhập lý do hủy đơn.",true);return;}
        var reason=CancellationReason.Trim();
        await RunActionAsync("sales-cancel",$"{_selectedOrder.Id}:{_selectedOrder.Revision}:{reason}",key=>_service.CancelAsync(_selectedOrder.Id,reason,key,ct),"Đã hủy đơn bán hàng.",ct);
        CancellationReason=string.Empty;
    }

    public async Task CloseExecutionAsync(CancellationToken ct=default)
    {
        if(!CanCloseExecution||_selectedOrder is null)return;
        if(string.IsNullOrWhiteSpace(CancellationReason)){SetMessage("Hãy nhập lý do kết thúc phần chưa giao.",true);return;}
        var reason=CancellationReason.Trim();
        await RunActionAsync("sales-execution-close",$"{_selectedOrder.Id}:{_selectedOrder.Revision}:{reason}",key=>_service.CloseExecutionAsync(_selectedOrder.Id,reason,key,ct),"Đã kết thúc phần chưa giao.",ct);
        CancellationReason=string.Empty;
    }

    public Task CompleteDirectAsync(CancellationToken ct=default)
    {
        if(!CanCompleteDirect||_selectedOrder is null||CurrentVersion is null)return Task.CompletedTask;
        var pickup=IsPickupDirect;
        var action=pickup?"pickup-order-complete":"manual-order-complete";
        return RunActionAsync(action,$"{_selectedOrder.Id}:{CurrentVersion.Revision}",
            key=>pickup?_service.CompletePickupAsync(_selectedOrder.Id,CurrentVersion.Revision,key,ct):_service.CompleteManualAsync(_selectedOrder.Id,CurrentVersion.Revision,key,ct),
            "Đã hoàn thành đơn; doanh số và khoản phải thu đã được ghi nhận.",ct);
    }

    public async Task SettleDirectAsync(CancellationToken ct=default)
    {
        if(!CanSettleDirect||_selectedOrder is null)return;
        if(!decimal.TryParse(PaidAmount,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)||amount<0){SetMessage("Số tiền thực nộp phải là số không âm.",true);return;}
        var request=new SalesDirectSettlementRequest(_selectedOrder.Revision,amount.ToString(CultureInfo.InvariantCulture),amount==0?null:PaymentMethod);
        var fp=$"{_selectedOrder.Id}:{_selectedOrder.Revision}:{request.PaidAmount}:{request.PaymentMethod}";
        var pickup=IsPickupDirect;
        await RunActionAsync(pickup?"pickup-order-settlement":"manual-order-settlement",fp,
            key=>pickup?_service.SettlePickupAsync(_selectedOrder.Id,request,key,ct):_service.SettleManualAsync(_selectedOrder.Id,request,key,ct),
            amount==0?"Đã ghi nhận nợ toàn bộ.":"Đã ghi nhận tiền thu và cập nhật công nợ.",ct);
        PaidAmount="0";
    }

    public void FillRemainingAmount(){if(_selectedOrder is not null)PaidAmount=OfficeNumberFormatting.CompactInvariant(_selectedOrder.ReceivableRemainingAmount,"0");}

    private async Task RunActionAsync(string action,string fingerprint,Func<string,Task<SalesOrderData>> operation,string success,CancellationToken ct)
    {
        if(IsBusy)return; IsBusy=true;
        try
        {
            var slot=$"{action}:{fingerprint}";
            if(!_actionKeys.TryGetValue(slot,out var key)){key=_keys.Create(action);_actionKeys[slot]=key;}
            var saved=await operation(key).ConfigureAwait(true);
            _actionKeys.Remove(slot); MergeOrder(saved); _selectedOrder=saved; RefreshRows(); RefreshSelected(); SetMessage(success,false);
        }
        catch(Exception ex){await HandleFailureAsync(ex,ct).ConfigureAwait(true);}
        finally{IsBusy=false;}
    }

    private void OpenEditor(EditorMode mode,string? orderId,string? version,string scope)
    {
        _editorMode=mode; _editingOrderId=orderId; _editingVersion=version; _saveKey=_keys.Create(scope); _entrySettingsKey=null; _quickCustomerKey=null;
        _confirmKey=_keys.Create(mode==EditorMode.Amendment?"sales-amend-confirm":"sales-confirm"); _editorBaselineFingerprint=string.Empty;
        _editorPricingAt=DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture);
        PricingMismatchMessage=string.Empty; CloseInventoryHistory(); RaiseEditorState();
    }

    private void OpenEditorFromVersion(EditorMode mode,SalesOrderVersionData version,string scope)
    {
        if(_selectedOrder is null)return;
        OpenEditor(mode,_selectedOrder.Id,version.VersionNumber,scope);
        ApplyVersionToEditor(version,false);
    }

    private void ApplyVersionToEditor(SalesOrderVersionData version,bool forCopy)
    {
        _preferredAddressId=version.CustomerAddressId;
        DraftCustomerMode=version.CustomerMode; DraftCustomerId=version.CustomerMode=="EXISTING"?version.CustomerId:string.Empty;
        DraftWalkInName=version.WalkInDisplayName??string.Empty; DraftWalkInPhone=version.WalkInPhone??string.Empty; DraftAddressId=version.CustomerAddressId??string.Empty;
        DraftWarehouseId=version.WarehouseId; DraftSalesChannelId=version.SalesChannelId??string.Empty; DraftPriceSelectionMode=version.PriceSelectionMode;
        DraftDeliveryChoice=version.DeliveryMode=="PICKUP"?"PICKUP":version.DeliveryExecutionMode=="MANUAL"?"MANUAL":"TRIP";
        DraftCollectionPolicy=version.CollectionPolicy; DraftRequestedDeliveryDate=forCopy?string.Empty:version.RequestedDeliveryDate??string.Empty; DraftNote=version.Note??string.Empty;
        DraftDocumentDiscountMode=version.DocumentDiscountMode; DraftDocumentDiscountValue=version.DocumentDiscountValue; DraftDocumentDiscountReason=version.DocumentDiscountReason??string.Empty;
        DraftCreditOverrideReason=string.Empty; CustomerSearch=string.Empty; ClearDraftLines();
        foreach(var line in version.Lines.OrderByDescending(x=>x.LineNumber))AddDraftLine(SalesPresentation.DraftLineFromVersion(line,forCopy));
        SelectedDraftLine=DraftLines.FirstOrDefault(); SkuRows.Clear(); _skuOptions.Clear(); SkuSearch=string.Empty;
        PricingMismatchMessage=string.Empty; CloseInventoryHistory(); RaiseDraftTotals(); CaptureEditorBaseline();
    }

    private async Task<SalesOrderDraftRequest> BuildDraftRequestAsync(CancellationToken ct)
    {
        var pricingAt=CurrentPricingAt();
        var customerId=IsExistingCustomer?DraftCustomerId:null;
        var lines=new List<SalesOrderLineDraftRequest>();
        foreach(var line in DraftLines)
        {
            var resolution=await _service.ResolvePriceAsync(new SalesPricePreviewRequest(line.VariantId,Positive(line.Quantity),DraftSalesChannelId,DraftPriceSelectionMode,pricingAt,customerId),ct).ConfigureAwait(true);
            line.ApplyPricingResolution(resolution);
            lines.Add(new SalesOrderLineDraftRequest
            {
                VariantId=line.VariantId,Quantity=Positive(line.Quantity),TaxMode=line.TaxMode,TaxRate=line.TaxRate,
                ManualUnitPriceMinor=string.IsNullOrWhiteSpace(line.ManualUnitPriceMinor)?null:NonNegative(line.ManualUnitPriceMinor),
                ManualReason=string.IsNullOrWhiteSpace(line.ManualUnitPriceMinor)?null:line.ManualReason.Trim(),
                DiscountMode=line.DiscountMode,DiscountValue=string.IsNullOrWhiteSpace(line.DiscountValue)?"0":NonNegative(line.DiscountValue),
                ExpectedSystemUnitPriceMinor=resolution.SystemUnitPriceMinor,ExpectedPricingFingerprint=resolution.ResolutionFingerprint
            });
        }
        var deliveryMode=DraftDeliveryChoice=="PICKUP"?"PICKUP":"DELIVERY";
        var execution=deliveryMode=="DELIVERY"?(DraftDeliveryChoice=="MANUAL"?"MANUAL":"TRIP"):null;
        var edited=_editorMode==EditorMode.Create?null:_editorMode==EditorMode.Amendment?PendingVersion:CurrentVersion;
        return new SalesOrderDraftRequest
        {
            CustomerMode=DraftCustomerMode,CustomerId=IsExistingCustomer?DraftCustomerId:null,
            WalkInDisplayName=IsWalkInCustomer?DraftWalkInName.Trim():null,WalkInPhone=IsWalkInCustomer&&!string.IsNullOrWhiteSpace(DraftWalkInPhone)?DraftWalkInPhone.Trim():null,
            CustomerAddressId=IsExistingCustomer&&deliveryMode=="DELIVERY"&&!string.IsNullOrWhiteSpace(DraftAddressId)?DraftAddressId:null,
            WarehouseId=DraftWarehouseId,SalesChannelId=DraftSalesChannelId,PriceSelectionMode=DraftPriceSelectionMode,PricingAt=pricingAt,
            DeliveryMode=deliveryMode,DeliveryExecutionMode=execution,CollectionPolicy=DraftCollectionPolicy,
            RequestedDeliveryDate=string.IsNullOrWhiteSpace(DraftRequestedDeliveryDate)?null:DraftRequestedDeliveryDate.Trim(),
            Note=string.IsNullOrWhiteSpace(DraftNote)?null:DraftNote.Trim(),ExpectedRevision=edited?.Revision,
            CreditOverrideReason=CanCreditOverride&&!string.IsNullOrWhiteSpace(DraftCreditOverrideReason)?DraftCreditOverrideReason.Trim():null,
            DocumentDiscountMode=DraftDocumentDiscountMode,DocumentDiscountValue=DraftDocumentDiscountMode=="NONE"?"0":NonNegative(DraftDocumentDiscountValue),
            DocumentDiscountReason=DraftDocumentDiscountMode=="NONE"?null:DraftDocumentDiscountReason.Trim(),Lines=lines.ToArray()
        };
    }

    private bool ValidateEditor(out string message)
    {
        message=string.Empty;
        if(string.IsNullOrWhiteSpace(DraftSalesChannelId)){message="Hãy chọn giá áp dụng.";return false;}
        if(string.IsNullOrWhiteSpace(DraftWarehouseId)){message="Hãy chọn kho xuất.";return false;}
        if(IsExistingCustomer&&string.IsNullOrWhiteSpace(DraftCustomerId)){message="Hãy chọn khách hàng.";return false;}
        if(IsWalkInCustomer&&string.IsNullOrWhiteSpace(DraftWalkInName)){message="Hãy nhập tên khách vãng lai.";return false;}
        if(IsWalkInCustomer&&DraftDeliveryChoice!="PICKUP"){message="Khách vãng lai chỉ dùng Mua tại quầy.";return false;}
        if(IsWalkInCustomer&&DraftCollectionPolicy is "COLLECT_AFTER_DELIVERY" or "CREDIT_TERMS"){message="Khách vãng lai không được bán chịu hoặc giao trước thu sau.";return false;}
        if(DraftPriceSelectionMode=="LAST_PURCHASE"&&!IsExistingCustomer){message="Giá lần mua trước chỉ dùng cho khách hàng đã chọn.";return false;}
        if(DraftLines.Count==0){message="Đơn bán hàng phải có ít nhất một SKU.";return false;}
        foreach(var line in DraftLines)
        {
            if(!TryPositive(line.Quantity)){message=$"Số lượng SKU {line.Sku} chưa hợp lệ.";return false;}
            if(!string.IsNullOrWhiteSpace(line.ManualUnitPriceMinor)&&(!CanPriceOverride||!TryNonNegative(line.ManualUnitPriceMinor))){message=$"Giá sửa trực tiếp của {line.Sku} cần quyền và số tiền hợp lệ.";return false;}
            if(!string.IsNullOrWhiteSpace(line.DiscountValue)&&line.DiscountValue!="0"&&(!CanDiscountOverride||!TryNonNegative(line.DiscountValue))){message=$"Chiết khấu của {line.Sku} chưa hợp lệ hoặc chưa được cấp quyền.";return false;}
        }
        if(DraftDocumentDiscountMode!="NONE"&&(!CanDiscountOverride||!TryNonNegative(DraftDocumentDiscountValue)||string.IsNullOrWhiteSpace(DraftDocumentDiscountReason))){message="Chiết khấu toàn đơn cần quyền, giá trị hợp lệ và lý do.";return false;}
        var estimate=DraftEstimate;
        if(estimate.MixedScope){message="Chỉ dùng CK từng dòng hoặc chiết khấu toàn đơn trong cùng một đơn.";return false;}
        if(!estimate.Valid){message="Giá, chiết khấu hoặc thuế trên đơn chưa hợp lệ.";return false;}
        return true;
    }

    private async Task ReloadAddressesAsync(string customerId)
    {
        _addresses.Clear(); AddressOptions.Clear(); DraftAddressId=string.Empty;
        if(!IsEditorOpen||!IsExistingCustomer||!Guid.TryParse(customerId,out _))return;
        try
        {
            var rows=await _service.ListCustomerAddressesAsync(customerId).ConfigureAwait(true);
            foreach(var row in rows.Where(a=>a.IsActive).OrderByDescending(a=>a.IsDefault).ThenBy(a=>a.Label))
            { _addresses.Add(row); AddressOptions.Add(new SalesLookupOption(row.Id,$"{row.Label} · {row.AddressLine1}, {row.Ward}, {row.Province}")); }
            var preferred=_preferredAddressId; _preferredAddressId=null;
            DraftAddressId=!string.IsNullOrWhiteSpace(preferred)&&_addresses.Any(a=>a.Id==preferred)
                ?preferred
                :_addresses.FirstOrDefault(a=>a.IsDefault)?.Id??_addresses.FirstOrDefault()?.Id??string.Empty;
        }
        catch { _preferredAddressId=null; AddressOptions.Clear(); }
    }

    private void BuildLookups()
    {
        RefreshCustomerOptions();
        Reset(WarehouseOptions,_warehouses.OrderBy(w=>w.Code).Select(w=>new SalesLookupOption(w.Id,$"{w.Code} · {w.Name}")));
        Reset(ChannelOptions,(_entrySettings?.SalesChannels??[]).OrderBy(c=>c.Code).Select(c=>new SalesLookupOption(c.Id,$"{c.Code} · {c.Name}")));
    }

    private void RefreshCustomerOptions()
    {
        var term=SalesSkuLocalCatalog.NormalizeSearchText(CustomerSearch);
        var matches=_customers.Where(x=>x.IsActive)
            .Where(x=>term.Length==0||new[]{x.Code,x.Name,x.Phone}.Where(v=>!string.IsNullOrWhiteSpace(v)).Any(v=>SalesSkuLocalCatalog.NormalizeSearchText(v).Contains(term,StringComparison.Ordinal)))
            .OrderBy(x=>x.Name,StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"),true)).ThenBy(x=>x.Code).Take(term.Length==0?100:30).ToList();
        var selected=_customers.FirstOrDefault(x=>x.Id==DraftCustomerId);
        if(selected is not null&&matches.All(x=>x.Id!=selected.Id))matches.Insert(0,selected);
        Reset(CustomerOptions,matches.Select(x=>new SalesLookupOption(x.Id,$"{x.Code} · {x.Name}{(string.IsNullOrWhiteSpace(x.Phone)?string.Empty:$" · {x.Phone}")}")));
        OnPropertyChanged(nameof(ShowCustomerResults));
        OnPropertyChanged(nameof(HasSelectedCustomer));
        OnPropertyChanged(nameof(SelectedCustomerSummary));
    }

    private void MergeCustomer(CustomerData customer,bool putFirst)
    {
        _customers.RemoveAll(x=>x.Id==customer.Id); if(putFirst)_customers.Insert(0,customer);else _customers.Add(customer);
    }

    private async Task WarmSkuCatalogAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _skuCatalog.LoadAsync(cancellationToken).ConfigureAwait(false);
            var delta=await _service.GetLocalSkuCatalogAsync(_skuCatalog.Cursor,cancellationToken).ConfigureAwait(false);
            await _skuCatalog.ApplyAsync(delta,cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){ } catch { }
    }

    private SalesOrderSkuSearchOptionData CatalogSearchOption(SalesOrderSkuCatalogRowData row)=>new()
    {
        Id=row.Id,ProductId=row.ProductId,ProductCode=row.ProductCode,ProductName=row.ProductName,Sku=row.Sku,VariantName=row.VariantName,
        Barcode=row.Barcode,Barcodes=row.Barcodes,UnitId=row.UnitId,UnitCode=row.UnitCode,UnitName=row.UnitName,ConversionToBase=row.ConversionToBase,AllowsFractional=row.AllowsFractional,
        DefaultTaxMode=_entrySettings?.DefaultTaxMode??"EXCLUSIVE",DefaultTaxRate=_entrySettings?.DefaultTaxRate??"0",
        Eligibility=new SalesOrderSkuEligibilityData{Selectable=true,Code="OK",Message=string.Empty}
    };

    private void PopulateLocalSkuCandidates()
    {
        if(!IsEditorOpen)return; var term=SkuSearch.Trim();
        if(term.Length==0){SkuRows.Clear();_skuOptions.Clear();OnPropertyChanged(nameof(ShowSkuResults));return;}
        var local=_skuCatalog.Search(term,30);
        if(local.Count==0){SkuRows.Clear();_skuOptions.Clear();OnPropertyChanged(nameof(ShowSkuResults));return;}
        SetSkuRows(local.Select(CatalogSearchOption).ToArray(),[]);
    }

    private void SetSkuRows(IReadOnlyList<SalesOrderSkuSearchOptionData> rows,IReadOnlyList<SalesOrderSkuSearchPreviewData> previews)
    {
        var byId=previews.ToDictionary(x=>x.Id,StringComparer.Ordinal); _skuOptions.Clear(); SkuRows.Clear();
        foreach(var row in rows)
        {
            _skuOptions.Add(row); byId.TryGetValue(row.Id,out var preview);
            var price=preview is null?"Đang cập nhật…":preview.PricePreview.UnitPriceMinor is null?"Chưa có giá":SalesPresentation.Money(preview.PricePreview.UnitPriceMinor);
            var stockUnit=preview?.InventoryPreview.UnitName??preview?.InventoryPreview.UnitCode;
            var available=preview is null?"…":preview.InventoryPreview.Status=="NOT_MANAGED"?"Không quản lý":SalesPresentation.QuantityWithUnit(preview.InventoryPreview.AvailableQuantity,stockUnit);
            var held=preview is null?"…":preview.InventoryPreview.Status=="NOT_MANAGED"?"—":SalesPresentation.QuantityWithUnit(preview.InventoryPreview.HeldQuantity,stockUnit);
            var eligibility=row.Eligibility.Selectable?(preview?.EligibilityMessage??row.Eligibility.Message):row.Eligibility.Message;
            SkuRows.Add(new SalesSkuSearchRow(row.Id,row.Sku,$"{row.ProductName}{(string.IsNullOrWhiteSpace(row.VariantName)?string.Empty:$" · {row.VariantName}")}",row.UnitName??row.UnitCode??"—",price,available,held,eligibility,row.Eligibility.Selectable,preview?.PricePreview.UnitPriceMinor,row));
        }
        OnPropertyChanged(nameof(ShowSkuResults));
    }

    private void AddDraftLine(SalesDraftLineRow row){row.PropertyChanged+=DraftLineOnPropertyChanged;DraftLines.Insert(0,row);RaiseDraftTotals();OnPropertyChanged(nameof(CanEditLineDiscount));}
    private void ClearDraftLines(){foreach(var row in DraftLines)row.PropertyChanged-=DraftLineOnPropertyChanged;DraftLines.Clear();_linePricingGeneration.Clear();}
    private void DraftLineOnPropertyChanged(object? sender,PropertyChangedEventArgs e){RaiseDraftTotals();OnPropertyChanged(nameof(CanEditLineDiscount));}
    private void RaiseDraftTotals(){foreach(var name in new[]{nameof(DraftGrossText),nameof(DraftDiscountText),nameof(DraftTaxText),nameof(DraftTotalText)})OnPropertyChanged(name);}

    private async Task<string?> PersistEntryDefaultsAsync(CancellationToken cancellationToken)
    {
        if(_editorMode!=EditorMode.Create||(!RememberWarehouseDefault&&!RememberDeliveryDefault))return null;
        try
        {
            _entrySettingsKey??=_keys.Create("sales-entry-settings-update");
            _entrySettings=await _service.UpdateEntrySettingsAsync(new SalesOrderEntrySettingsUpdateRequest(
                RememberWarehouseDefault?DraftWarehouseId:_entrySettings?.SavedWarehouseId,
                RememberDeliveryDefault?DraftDeliveryChoice:_entrySettings?.SavedDeliveryChoice),_entrySettingsKey,cancellationToken).ConfigureAwait(true);
            _entrySettingsKey=null; return null;
        }
        catch(Exception ex){return ex is CanonicalApiException api?CanonicalErrorMessages.ToOfficeMessage(api):ex.Message;}
    }

    private string RequireQuickCustomerKey(){_quickCustomerKey??=_keys.Create("sales-quick-customer");return _keys.IsValid(_quickCustomerKey)?_quickCustomerKey:throw new InvalidOperationException("Thao tác tạo khách chưa có khóa chống xử lý trùng hợp lệ.");}
    private static string AutoCustomerCode()=>$"KH{Guid.NewGuid():N}"[..14].ToUpperInvariant();
    private static string NormalizePhone(string? value)=>new string((value??string.Empty).Where(ch=>char.IsDigit(ch)||ch=='+').ToArray());

    private void RefreshRows()
    {
        var term=Search.Trim();
        Reset(OrderRows,_orders.Where(o=>LaneFilter=="all"||SalesPresentation.Lane(o)==LaneFilter)
            .Where(o=>StageFilter=="all"||SalesPresentation.Stage(o)==StageFilter)
            .Where(o=>SourceFilter=="all"||SalesPresentation.SourceBucket(o)==SourceFilter)
            .Where(o=>string.IsNullOrWhiteSpace(term)||new[]{o.Number,o.CustomerCode,o.CustomerName,o.WarehouseCode,o.SalesChannelCode,o.SalesChannelName}.Any(v=>!string.IsNullOrWhiteSpace(v)&&v.Contains(term,StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(o=>o.CreatedAt)
            .Select((o,index)=>new SalesOrderRow((index+1).ToString(CultureInfo.InvariantCulture),o.Id,SalesPresentation.ListNumber(o.Number),$"{o.CustomerCode} · {o.CustomerName}",$"{o.WarehouseCode} · {o.WarehouseName}",SalesPresentation.LaneLabel(o),SalesPresentation.Lane(o),SalesPresentation.StageLabel(o),SalesPresentation.Stage(o),SalesPresentation.SourceLabel(o),SalesPresentation.Money(o.Total),SalesPresentation.SettlementStatus(o.SettlementStatus),SalesPresentation.DateTimeText(o.CreatedAt))));
        RaiseCounts();
    }

    private void RefreshSelected()
    {
        DetailLineRows.Clear(); VersionRows.Clear();
        var stockByLineId=(_selectedOrder?.Fulfillment?.Lines??[])
            .Where(x=>!string.IsNullOrWhiteSpace(x.SalesOrderLineId))
            .GroupBy(x=>x.SalesOrderLineId,StringComparer.Ordinal)
            .ToDictionary(g=>g.Key,g=>g.First(),StringComparer.Ordinal);
        if(CurrentVersion is { } current)
            foreach(var line in current.Lines.OrderBy(x=>x.LineNumber))
            {
                stockByLineId.TryGetValue(line.Id,out var stock);
                var stockUnit=stock?.BaseUnitName??stock?.BaseUnitCode;
                DetailLineRows.Add(new SalesOrderLineRow(
                    line.LineNumber.ToString(CultureInfo.InvariantCulture),line.Sku,line.ItemName,
                    SalesPresentation.QuantityWithUnit(line.Quantity,line.UnitName??line.UnitCode),
                    stock is null?"—":SalesPresentation.QuantityWithUnit(stock.WarehouseOnHandBaseQuantity,stockUnit),
                    stock is null?"—":SalesPresentation.QuantityWithUnit(stock.WarehouseHeldByOthersBaseQuantity,stockUnit),
                    stock is null?"—":SalesPresentation.QuantityWithUnit(stock.WarehouseAvailableBaseQuantity,stockUnit),
                    SalesPresentation.Money(line.UnitPrice),SalesPresentation.Money(line.DiscountAmount),SalesPresentation.Money(line.TaxAmount),SalesPresentation.Money(line.LineTotal)));
            }
        if(_selectedOrder is not null)
            foreach(var version in _selectedOrder.Versions.OrderByDescending(v=>int.TryParse(v.VersionNumber,out var n)?n:0))
                VersionRows.Add(new SalesOrderVersionRow(version.VersionNumber,SalesPresentation.VersionStatus(version.Status),version.AmendmentReason??"Phiên bản gốc",SalesPresentation.Money(version.Total),SalesPresentation.DateTimeText(version.CreatedAt)));
        foreach(var name in new[]{nameof(HasSelectedOrder),nameof(SelectedNumber),nameof(SelectedCustomer),nameof(SelectedWarehouse),nameof(SelectedLane),nameof(SelectedOrderStatus),nameof(SelectedFulfillmentStatus),nameof(SelectedDeliveryStatus),nameof(SelectedSettlementStatus),nameof(SelectedTotal),nameof(SelectedCollection),nameof(SelectedReceivable),nameof(SelectedOrder),nameof(SelectedVersion),nameof(CanEditSelectedDraft),nameof(CanConfirmSelected),nameof(CanCreateAmendment),nameof(CanEditAmendment),nameof(CanConfirmAmendment),nameof(CanEditManual),nameof(CanIssueSelectedStock),nameof(CanCancelSelected),nameof(CanCloseExecution),nameof(CanCompleteDirect),nameof(CanSettleDirect),nameof(ShowDirectSettlement),nameof(CanCopySelected),nameof(CanPrintSelected)})OnPropertyChanged(name);
    }

    private void MergeOrder(SalesOrderData order)
    {
        var i=_orders.FindIndex(x=>x.Id==order.Id);
        var total=!string.IsNullOrWhiteSpace(order.Total)?order.Total:SalesPresentation.ActiveVersion(order)?.Total;
        if(string.IsNullOrWhiteSpace(total)&&i>=0)total=_orders[i].Total;
        var merged=order with { Total=total };
        if(i>=0)_orders[i]=merged;else _orders.Add(merged);
        if(ReferenceEquals(_selectedOrder,order))_selectedOrder=merged;
    }
    private void RaiseCounts(){OnPropertyChanged(nameof(TotalCount));OnPropertyChanged(nameof(ActiveCount));OnPropertyChanged(nameof(PreparingCount));OnPropertyChanged(nameof(WaitingDeliveryCount));OnPropertyChanged(nameof(CompletedCount));}
    private void RaisePermissions(){foreach(var name in new[]{nameof(CanRead),nameof(CanCreate),nameof(CanUpdateDraft),nameof(CanConfirmPermission),nameof(CanAmend),nameof(CanCancelPermission),nameof(CanPriceOverride),nameof(CanDiscountOverride),nameof(CanCreditOverride),nameof(CanIssueInventory),nameof(CanRecordPayment),nameof(CanQuickCreateCustomer),nameof(CanEditLineDiscount),nameof(CanOpenInventoryHistory)})OnPropertyChanged(name);RefreshSelected();}

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private void RaiseEditorState(){foreach(var name in new[]{nameof(IsEditorOpen),nameof(IsCreateEditor),nameof(EditorTitle),nameof(CanEditorConfirm),nameof(IsExistingCustomer),nameof(IsWalkInCustomer),nameof(CanQuickCreateCustomer),nameof(ShowCustomerResults),nameof(ShowSkuResults),nameof(HasSelectedCustomer),nameof(SelectedCustomerSummary)})OnPropertyChanged(name);}
    private void ApplyDeliveryChoice(string? value)=>DraftDeliveryChoice=value is "MANUAL" or "PICKUP"?value:"TRIP";

    private string CurrentPricingAt()
    {
        if(string.IsNullOrWhiteSpace(_editorPricingAt))
            _editorPricingAt=DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture);
        return _editorPricingAt;
    }

    private void ResetEditorMutationKeys()
    {
        _saveKey=_keys.Create(_editorMode switch
        {
            EditorMode.Create=>"sales-order-create",EditorMode.Draft=>"sales-order-draft-update",
            EditorMode.Amendment=>"sales-order-amendment-update",EditorMode.ManualEdit=>"sales-order-manual-edit",_=>"sales-order-save"
        });
        _confirmKey=_keys.Create(_editorMode==EditorMode.Amendment?"sales-amend-confirm":"sales-confirm");
    }

    private void CaptureEditorBaseline()=>_editorBaselineFingerprint=EditorFingerprint();

    private string EditorFingerprint()
    {
        var builder=new StringBuilder();
        void Add(string? value)=>builder.Append(value??string.Empty).Append('\u001F');
        foreach(var value in new[]{DraftCustomerMode,DraftCustomerId,DraftWalkInName,DraftWalkInPhone,DraftAddressId,DraftWarehouseId,DraftSalesChannelId,
            DraftPriceSelectionMode,DraftDeliveryChoice,DraftCollectionPolicy,DraftRequestedDeliveryDate,DraftNote,DraftDocumentDiscountMode,
            DraftDocumentDiscountValue,DraftDocumentDiscountReason,DraftCreditOverrideReason})
            Add(value);
        foreach(var line in DraftLines)
        {
            Add(line.ClientLineId);Add(line.VariantId);Add(line.Quantity);Add(line.ManualUnitPriceMinor);Add(line.ManualReason);
            Add(line.DiscountMode);Add(line.DiscountValue);Add(line.SystemUnitPriceMinor);Add(line.PricingFingerprint);
        }
        return builder.ToString();
    }

    private string RequireSaveKey()=>!string.IsNullOrWhiteSpace(_saveKey)&&_keys.IsValid(_saveKey)?_saveKey:throw new InvalidOperationException("Thao tác lưu đơn chưa có khóa chống xử lý trùng hợp lệ.");
    private string RequireConfirmKey()=>!string.IsNullOrWhiteSpace(_confirmKey)&&_keys.IsValid(_confirmKey)?_confirmKey:throw new InvalidOperationException("Thao tác xác nhận đơn chưa có khóa chống xử lý trùng hợp lệ.");

    private async Task HandleFailureAsync(Exception ex,CancellationToken ct)
    {
        if(ex is CanonicalApiException api)
        {
            if(api.StatusCode==HttpStatusCode.Unauthorized){await _authentication.LogoutAsync(ct).ConfigureAwait(true);SetMessage(CanonicalErrorMessages.WithRequestId("Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.",api.RequestId),true);return;}
            var message=api.Code switch
            {
                "FORBIDDEN"=>"Tài khoản chưa được cấp quyền cho thao tác bán hàng này.",
                "CONFLICT" or "INVALID_STATUS_TRANSITION"=>"Trạng thái đơn đã thay đổi. Hãy cập nhật đơn rồi thao tác lại.",
                "DOCUMENT_NUMBER_SERIES_UNAVAILABLE"=>"Chưa cấp được số chứng từ. Vui lòng thử lại sau.",
                "WAREHOUSE_SCOPE_DENIED"=>"Kho này nằm ngoài phạm vi được cấp.",
                "SALES_ORDER_PRICE_PREVIEW_UNAVAILABLE"=>"Chưa tính được giá bán. Vui lòng thử lại.",
                _=>CanonicalErrorMessages.ToOfficeMessage(api)
            };
            SetMessage(CanonicalErrorMessages.WithRequestId(message,api.RequestId),true);return;
        }
        SetMessage(ex is HttpRequestException or TaskCanceledException?"Không kết nối được hệ thống Công Ty. Vui lòng kiểm tra kết nối rồi thử lại.":ex.Message,true);
    }

    private void SetMessage(string value,bool error){MessageIsError=error;Message=value;}
    private static bool TryPositive(string v)=>decimal.TryParse(v,NumberStyles.Number,CultureInfo.InvariantCulture,out var n)&&n>0;
    private static bool TryNonNegative(string v)=>decimal.TryParse(v,NumberStyles.Number,CultureInfo.InvariantCulture,out var n)&&n>=0;
    private static string Positive(string v)=>TryPositive(v)?decimal.Parse(v,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture):throw new InvalidOperationException("Số lượng phải lớn hơn 0.");
    private static string NonNegative(string v)=>TryNonNegative(v)?decimal.Parse(v,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture):throw new InvalidOperationException("Giá trị phải là số không âm.");
    private static void Replace<T>(List<T> target,IEnumerable<T> source){target.Clear();target.AddRange(source);}
    private static void Reset<T>(ObservableCollection<T> target,IEnumerable<T> source){target.Clear();foreach(var item in source)target.Add(item);}
    private bool SetField<T>(ref T field,T value,[CallerMemberName] string? name=null){if(EqualityComparer<T>.Default.Equals(field,value))return false;field=value;OnPropertyChanged(name);return true;}
    private void OnPropertyChanged([CallerMemberName] string? name=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}
