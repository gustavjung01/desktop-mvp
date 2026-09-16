using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel : INotifyPropertyChanged
{
    private enum EditorKind { None, Channel, List, Item }
    private const string ReadPermission = "core.price.read";
    private const string WritePermission = "core.price.write";
    private const string OverridePermission = "core.sales-order.price-override";
    private const string BaseOnly = "__BASE__";
    private const string AllLists = "__ALL__";

    private readonly IPricingService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<SalesChannelData> _channels = [];
    private readonly List<PriceListData> _lists = [];
    private readonly List<ProductData> _products = [];
    private readonly List<ProductUnitData> _units = [];
    private readonly List<CustomerGroupData> _groups = [];
    private readonly List<CustomerData> _customers = [];
    private readonly Dictionary<string, IReadOnlyList<PriceListItemData>> _itemsByList = new(StringComparer.Ordinal);
    private readonly List<(ProductData Product, ProductVariantData Variant)> _overviewVariants = [];

    private bool _loaded;
    private bool _overviewLoaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private int _tabIndex;
    private EditorKind _editorKind;
    private SalesChannelData? _editingChannel;
    private PriceListData? _editingList;
    private PriceListItemData? _editingItem;
    private string _selectedPriceListId = string.Empty;
    private string _overviewMode = BaseOnly;
    private string _overviewSearch = string.Empty;

    private string _channelCode = string.Empty;
    private string _channelName = string.Empty;
    private string _channelDescription = string.Empty;
    private bool _channelActive = true;

    private string _listCode = string.Empty;
    private string _listName = string.Empty;
    private string _listType = "BASE";
    private string _listPriority = "100";
    private string _listStacking = "EXCLUSIVE";
    private bool _listStopProcessing;
    private string _listChannelId = string.Empty;
    private string _listCustomerGroupId = string.Empty;
    private string _listCustomerId = string.Empty;
    private string _listEffectiveFrom = string.Empty;
    private string _listEffectiveTo = string.Empty;
    private string _listDescription = string.Empty;
    private bool _listActive = true;

    private string _itemProductId = string.Empty;
    private string _itemVariantId = string.Empty;
    private string _itemAdjustmentType = "FIXED_PRICE";
    private string _itemAmount = string.Empty;
    private string _itemPercent = string.Empty;
    private string _itemMinQuantity = "0";
    private string _itemMaxQuantity = string.Empty;
    private string _itemEffectiveFrom = string.Empty;
    private string _itemEffectiveTo = string.Empty;
    private string _itemExternalRuleCode = string.Empty;
    private string _itemNote = string.Empty;
    private bool _itemActive = true;

    private string _resolverProductId = string.Empty;
    private string _resolverVariantId = string.Empty;
    private string _resolverQuantity = "1";
    private string _resolverChannelId = string.Empty;
    private string _resolverCustomerGroupId = string.Empty;
    private string _resolverCustomerId = string.Empty;
    private string _resolverManualPrice = string.Empty;
    private string _resolverManualReason = string.Empty;
    private PricingResolutionData? _resolution;

    public PricingViewModel(IPricingService service, IAccessStateService access, ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        foreach (var option in PricingPresentation.ListTypes) ListTypeOptions.Add(option);
        foreach (var option in PricingPresentation.StackingModes) StackingOptions.Add(option);
        foreach (var option in PricingPresentation.AdjustmentTypes) AdjustmentOptions.Add(option);
        _access.Changed += (_, _) =>
        {
            if (!CanRead)
            {
                _loaded = false;
                _overviewLoaded = false;
                ClearData();
            }
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(CanWrite));
            OnPropertyChanged(nameof(CanOverridePrice));
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? OverviewColumnsChanged;

    public ObservableCollection<PricingChannelRow> Channels { get; } = [];
    public ObservableCollection<PricingListRow> PriceLists { get; } = [];
    public ObservableCollection<PricingItemRow> PriceItems { get; } = [];
    public ObservableCollection<PricingLookup> ProductOptions { get; } = [];
    public ObservableCollection<PricingLookup> ItemVariantOptions { get; } = [];
    public ObservableCollection<PricingLookup> ResolverVariantOptions { get; } = [];
    public ObservableCollection<PricingLookup> ChannelOptions { get; } = [];
    public ObservableCollection<PricingLookup> CustomerGroupOptions { get; } = [];
    public ObservableCollection<PricingLookup> CustomerOptions { get; } = [];
    public ObservableCollection<PricingOption> ListTypeOptions { get; } = [];
    public ObservableCollection<PricingOption> StackingOptions { get; } = [];
    public ObservableCollection<PricingOption> AdjustmentOptions { get; } = [];
    public ObservableCollection<OverviewPriceListOption> OverviewModes { get; } = [];
    public ObservableCollection<PricingOverviewRow> OverviewRows { get; } = [];
    public ObservableCollection<PricingResolutionStepRow> ResolutionSteps { get; } = [];

    public IReadOnlyList<PriceListData> VisibleOverviewPriceLists => _overviewMode switch
    {
        AllLists => _lists.Where(row => row.ListType != "BASE").OrderByDescending(row => row.Priority).ThenBy(row => row.Code).ToArray(),
        BaseOnly => [],
        _ => _lists.Where(row => row.ListType != "BASE" && row.Id == _overviewMode).ToArray()
    };

    public bool CanRead => _access.HasPermission(ReadPermission);
    public bool CanWrite => _access.HasPermission(WritePermission);
    public bool CanOverridePrice => _access.HasPermission(OverridePermission);
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { OnPropertyChanged(nameof(IsNotBusy)); RaiseActionState(); } } }
    public bool IsNotBusy => !IsBusy;
    public string Message { get => _message; private set => SetField(ref _message, value); }
    public bool MessageIsError { get => _messageIsError; private set => SetField(ref _messageIsError, value); }
    public int TabIndex { get => _tabIndex; set => SetField(ref _tabIndex, Math.Clamp(value, 0, 4)); }

    public bool IsEditorOpen => _editorKind != EditorKind.None;
    public bool IsChannelEditor => _editorKind == EditorKind.Channel;
    public bool IsListEditor => _editorKind == EditorKind.List;
    public bool IsItemEditor => _editorKind == EditorKind.Item;
    public string EditorTitle => _editorKind switch
    {
        EditorKind.Channel => _editingChannel is null ? "Tạo kênh bán" : $"Sửa kênh {_editingChannel.Code}",
        EditorKind.List => _editingList is null ? "Tạo bảng giá hoặc chương trình" : $"Sửa {_editingList.Code}",
        EditorKind.Item => _editingItem is null ? "Thêm giá sản phẩm" : $"Sửa giá {_editingItem.Sku}",
        _ => string.Empty
    };
    public string EditorDescription => _editorKind switch
    {
        EditorKind.Channel => "Kênh bán dùng để áp dụng bảng giá phù hợp cho từng hình thức bán hàng.",
        EditorKind.List => "Thiết lập phạm vi áp dụng, thứ tự ưu tiên và thời gian hiệu lực.",
        EditorKind.Item => SelectedPriceList is null ? "Chọn bảng giá trước khi thêm giá sản phẩm." : $"Bảng giá: {SelectedPriceList.Code} — {SelectedPriceList.Name}",
        _ => string.Empty
    };
    public string EditorSaveText => _editorKind switch
    {
        EditorKind.Channel => _editingChannel is null ? "Tạo kênh" : "Cập nhật kênh",
        EditorKind.List => _editingList is null ? "Tạo bảng giá" : "Cập nhật bảng giá",
        EditorKind.Item => _editingItem is null ? "Thêm giá" : "Cập nhật giá",
        _ => "Lưu"
    };
    public bool CanSaveEditor => CanWrite && IsNotBusy && _editorKind switch
    {
        EditorKind.Channel => !string.IsNullOrWhiteSpace(ChannelCode) && !string.IsNullOrWhiteSpace(ChannelName),
        EditorKind.List => !string.IsNullOrWhiteSpace(ListCode) && !string.IsNullOrWhiteSpace(ListName) && int.TryParse(ListPriority, out _),
        EditorKind.Item => !string.IsNullOrWhiteSpace(ItemVariantId),
        _ => false
    };

    public string SelectedPriceListId { get => _selectedPriceListId; private set { if (SetField(ref _selectedPriceListId, value)) { OnPropertyChanged(nameof(SelectedPriceList)); OnPropertyChanged(nameof(CanCreatePriceItem)); OnPropertyChanged(nameof(HasSelectedPriceList)); OnPropertyChanged(nameof(NoSelectedPriceList)); } } }
    public PriceListData? SelectedPriceList => _lists.FirstOrDefault(row => row.Id == SelectedPriceListId);
    public bool CanCreatePriceItem => CanWrite && IsNotBusy && SelectedPriceList is not null;
    public bool HasChannels => Channels.Count > 0;
    public bool NoChannels => !HasChannels;
    public bool HasPriceLists => PriceLists.Count > 0;
    public bool NoPriceLists => !HasPriceLists;
    public bool HasPriceItems => PriceItems.Count > 0;
    public bool NoPriceItems => !HasPriceItems;
    public bool HasOverviewRows => OverviewRows.Count > 0;
    public bool NoOverviewRows => !HasOverviewRows;
    public bool HasSelectedPriceList => SelectedPriceList is not null;
    public bool NoSelectedPriceList => !HasSelectedPriceList;

    public string OverviewMode { get => _overviewMode; set { if (SetField(ref _overviewMode, value)) { RebuildOverviewRows(); OverviewColumnsChanged?.Invoke(this, EventArgs.Empty); OnPropertyChanged(nameof(VisibleOverviewPriceLists)); } } }
    public string OverviewSearch { get => _overviewSearch; set { if (SetField(ref _overviewSearch, value)) RebuildOverviewRows(); } }
    public int OverviewSkuCount => _overviewVariants.Count;
    public int OverviewListCount => _lists.Count(row => row.ListType != "BASE");
    public int OverviewRuleCount => _itemsByList.Values.Sum(rows => rows.Count);

    public string ChannelCode { get => _channelCode; set { if (SetField(ref _channelCode, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ChannelName { get => _channelName; set { if (SetField(ref _channelName, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ChannelDescription { get => _channelDescription; set => SetField(ref _channelDescription, value); }
    public bool ChannelActive { get => _channelActive; set => SetField(ref _channelActive, value); }
    public bool ChannelCodeEnabled => _editingChannel is null;

    public string ListCode { get => _listCode; set { if (SetField(ref _listCode, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ListName { get => _listName; set { if (SetField(ref _listName, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ListType { get => _listType; set { if (SetField(ref _listType, value)) { ListPriority = PricingPresentation.DefaultPriority(value).ToString(CultureInfo.InvariantCulture); if (value == "BASE") ListChannelId = string.Empty; if (value is "BASE" or "CHANNEL") ListCustomerGroupId = string.Empty; if (value is "BASE" or "CHANNEL" or "CUSTOMER_GROUP") ListCustomerId = string.Empty; RaiseListScopeState(); } } }
    public string ListPriority { get => _listPriority; set { if (SetField(ref _listPriority, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ListStacking { get => _listStacking; set => SetField(ref _listStacking, value); }
    public bool ListStopProcessing { get => _listStopProcessing; set => SetField(ref _listStopProcessing, value); }
    public string ListChannelId { get => _listChannelId; set => SetField(ref _listChannelId, value); }
    public string ListCustomerGroupId { get => _listCustomerGroupId; set => SetField(ref _listCustomerGroupId, value); }
    public string ListCustomerId { get => _listCustomerId; set => SetField(ref _listCustomerId, value); }
    public string ListEffectiveFrom { get => _listEffectiveFrom; set => SetField(ref _listEffectiveFrom, value); }
    public string ListEffectiveTo { get => _listEffectiveTo; set => SetField(ref _listEffectiveTo, value); }
    public string ListDescription { get => _listDescription; set => SetField(ref _listDescription, value); }
    public bool ListActive { get => _listActive; set => SetField(ref _listActive, value); }
    public bool ListIdentityEnabled => _editingList is null;
    public bool ListChannelEnabled => ListType != "BASE";
    public bool ListCustomerGroupEnabled => ListType != "BASE" && ListType != "CHANNEL";
    public bool ListCustomerEnabled => ListType is "CUSTOMER" or "PROMOTION" or "CUSTOM";
    public string ListScopeHint => ListType switch
    {
        "BASE" => "Giá nền áp dụng làm mức khởi điểm trước các điều kiện khác.",
        "CHANNEL" => "Chọn kênh bán cần áp dụng mức giá này.",
        "CUSTOMER_GROUP" => "Chọn nhóm khách; có thể giới hạn thêm theo kênh.",
        "CUSTOMER" => "Chọn khách hàng cụ thể; có thể giới hạn thêm theo kênh hoặc nhóm.",
        "PROMOTION" => "Chương trình khuyến mãi có thể giới hạn theo kênh, nhóm hoặc khách hàng.",
        _ => "Điều kiện khác có thể giới hạn theo đối tượng bán phù hợp."
    };

    public string ItemProductId { get => _itemProductId; set => SetField(ref _itemProductId, value); }
    public string ItemVariantId { get => _itemVariantId; set { if (SetField(ref _itemVariantId, value)) OnPropertyChanged(nameof(CanSaveEditor)); } }
    public string ItemAdjustmentType { get => _itemAdjustmentType; set { if (SetField(ref _itemAdjustmentType, value)) { OnPropertyChanged(nameof(ItemUsesAmount)); OnPropertyChanged(nameof(ItemUsesPercent)); } } }
    public string ItemAmount { get => _itemAmount; set => SetField(ref _itemAmount, value); }
    public string ItemPercent { get => _itemPercent; set => SetField(ref _itemPercent, value); }
    public string ItemMinQuantity { get => _itemMinQuantity; set => SetField(ref _itemMinQuantity, value); }
    public string ItemMaxQuantity { get => _itemMaxQuantity; set => SetField(ref _itemMaxQuantity, value); }
    public string ItemEffectiveFrom { get => _itemEffectiveFrom; set => SetField(ref _itemEffectiveFrom, value); }
    public string ItemEffectiveTo { get => _itemEffectiveTo; set => SetField(ref _itemEffectiveTo, value); }
    public string ItemExternalRuleCode { get => _itemExternalRuleCode; set => SetField(ref _itemExternalRuleCode, value); }
    public string ItemNote { get => _itemNote; set => SetField(ref _itemNote, value); }
    public bool ItemActive { get => _itemActive; set => SetField(ref _itemActive, value); }
    public bool ItemIdentityEnabled => _editingItem is null;
    public bool ItemUsesAmount => ItemAdjustmentType is "FIXED_PRICE" or "AMOUNT_DISCOUNT" or "AMOUNT_MARKUP";
    public bool ItemUsesPercent => !ItemUsesAmount;

    public string ResolverProductId { get => _resolverProductId; set => SetField(ref _resolverProductId, value); }
    public string ResolverVariantId { get => _resolverVariantId; set { if (SetField(ref _resolverVariantId, value)) OnPropertyChanged(nameof(CanResolve)); } }
    public string ResolverQuantity { get => _resolverQuantity; set => SetField(ref _resolverQuantity, value); }
    public string ResolverChannelId { get => _resolverChannelId; set => SetField(ref _resolverChannelId, value); }
    public string ResolverCustomerGroupId { get => _resolverCustomerGroupId; set => SetField(ref _resolverCustomerGroupId, value); }
    public string ResolverCustomerId { get => _resolverCustomerId; set => SetField(ref _resolverCustomerId, value); }
    public string ResolverManualPrice { get => _resolverManualPrice; set => SetField(ref _resolverManualPrice, value); }
    public string ResolverManualReason { get => _resolverManualReason; set => SetField(ref _resolverManualReason, value); }
    public bool CanResolve => CanRead && IsNotBusy && !string.IsNullOrWhiteSpace(ResolverVariantId) && (string.IsNullOrWhiteSpace(ResolverManualPrice) || CanOverridePrice);
    public bool ManualPriceEnabled => CanOverridePrice;
    public string ResolvedBasePrice => _resolution is null ? "—" : PricingPresentation.Money(_resolution.BaseUnitPriceMinor);
    public string ResolvedFinalPrice => _resolution is null ? "—" : PricingPresentation.Money(_resolution.FinalUnitPriceMinor);
    public string ResolvedLineTotal => _resolution is null ? "—" : PricingPresentation.Money(_resolution.LineTotalMinor);
    public bool HasResolution => _resolution is not null;
}
