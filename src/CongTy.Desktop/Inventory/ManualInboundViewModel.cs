using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record ManualInboundOption(string Id, string Label);
public sealed record ManualInboundWarehouseOption(ManualInboundWarehouseOptionData Data)
{
    public string Id => Data.Id;
    public string Label => $"{Data.Code} · {Data.Name}";
}
public sealed record ManualInboundSupplierOption(ManualInboundSupplierOptionData Data)
{
    public string Id => Data.Id;
    public string Label => $"{Data.Code} · {Data.Name}";
}
public sealed record ManualInboundLocationOption(ManualInboundLocationOptionData Data)
{
    public string Id => Data.Id;
    public string Code => Data.Code;
    public string Label => $"{Data.Code} · {Data.Name}";
}
public sealed record ManualInboundProductResult(ManualInboundProductOptionData Data)
{
    public string Title => Data.ProductName;
    public string Identity => string.IsNullOrWhiteSpace(Data.VariantName)
        ? Data.Sku
        : $"{Data.Sku} · {Data.VariantName}";
    public string Meta => string.IsNullOrWhiteSpace(Data.UnitCost)
        ? $"{Data.UnitCode} · Chưa có giá vốn"
        : $"{Data.UnitCode} · Giá vốn {ManualInboundPresentation.Cost(Data.UnitCost)} đ";
}

public sealed class ManualInboundDraftRowView : INotifyPropertyChanged
{
    private readonly Action _changed;
    private string _sku;
    private string _sourceQuantity;
    private string _unitCost;
    private string _locationCode;
    private string _lotCode;
    private string _manufacturedDate;
    private string _expiryDate;
    private string _supplierLotReference;
    private string _productName;
    private string _unitCode;
    private string? _currentUnitCost;
    private int _sequence;

    public ManualInboundDraftRowView(
        Action changed,
        string sku = "",
        string sourceQuantity = "",
        string unitCost = "",
        string locationCode = "",
        string lotCode = "",
        string manufacturedDate = "",
        string expiryDate = "",
        string supplierLotReference = "",
        string productName = "",
        string unitCode = "",
        string? currentUnitCost = null)
    {
        _changed = changed;
        _sku = sku;
        _sourceQuantity = sourceQuantity;
        _unitCost = unitCost;
        _locationCode = locationCode;
        _lotCode = lotCode;
        _manufacturedDate = manufacturedDate;
        _expiryDate = expiryDate;
        _supplierLotReference = supplierLotReference;
        _productName = productName;
        _unitCode = unitCode;
        _currentUnitCost = currentUnitCost;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Sequence
    {
        get => _sequence;
        private set
        {
            if (_sequence == value) return;
            _sequence = value;
            OnPropertyChanged();
        }
    }

    public string Sku
    {
        get => _sku;
        set => SetDraft(ref _sku, value?.TrimStart() ?? string.Empty);
    }

    public string SourceQuantity
    {
        get => _sourceQuantity;
        set => SetDraft(ref _sourceQuantity, value ?? string.Empty);
    }

    public string UnitCost
    {
        get => _unitCost;
        set => SetDraft(ref _unitCost, value ?? string.Empty);
    }

    public string LocationCode
    {
        get => _locationCode;
        set => SetDraft(ref _locationCode, value?.TrimStart().ToUpperInvariant() ?? string.Empty);
    }

    public string LotCode
    {
        get => _lotCode;
        set => SetDraft(ref _lotCode, value?.TrimStart().ToUpperInvariant() ?? string.Empty);
    }

    public string ManufacturedDate
    {
        get => _manufacturedDate;
        set => SetDraft(ref _manufacturedDate, value ?? string.Empty);
    }

    public string ExpiryDate
    {
        get => _expiryDate;
        set => SetDraft(ref _expiryDate, value ?? string.Empty);
    }

    public string SupplierLotReference
    {
        get => _supplierLotReference;
        set => SetDraft(ref _supplierLotReference, value ?? string.Empty);
    }

    public string ProductName
    {
        get => string.IsNullOrWhiteSpace(_productName) ? "Kiểm tra để nhận diện" : _productName;
        private set
        {
            _productName = value;
            OnPropertyChanged();
        }
    }

    public string UnitCode
    {
        get => string.IsNullOrWhiteSpace(_unitCode) ? "—" : _unitCode;
        private set
        {
            _unitCode = value;
            OnPropertyChanged();
        }
    }

    public string CurrentCostText =>
        string.IsNullOrWhiteSpace(_currentUnitCost)
            ? string.Empty
            : $"Giá vốn hiện hành: {ManualInboundPresentation.Cost(_currentUnitCost)} đ";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Sku)
        && string.IsNullOrWhiteSpace(SourceQuantity)
        && string.IsNullOrWhiteSpace(UnitCost)
        && string.IsNullOrWhiteSpace(LocationCode)
        && string.IsNullOrWhiteSpace(LotCode)
        && string.IsNullOrWhiteSpace(ManufacturedDate)
        && string.IsNullOrWhiteSpace(ExpiryDate)
        && string.IsNullOrWhiteSpace(SupplierLotReference);

    public void SetSequence(int value) => Sequence = value;

    public void ApplyProduct(ManualInboundProductOptionData product)
    {
        _sku = product.Sku;
        _sourceQuantity = "1";
        _productName = product.ProductName;
        _unitCode = product.UnitCode;
        _currentUnitCost = product.UnitCost;
        RaiseAll();
    }

    public void ApplyResolved(string? productName, string? unitCode, string? currentUnitCost)
    {
        _productName = productName ?? string.Empty;
        _unitCode = unitCode ?? string.Empty;
        _currentUnitCost = currentUnitCost;
        RaiseAll();
    }

    public void ApplyCorrection(string field, string value)
    {
        switch (field)
        {
            case "locationCode": _locationCode = value.Trim().ToUpperInvariant(); OnPropertyChanged(nameof(LocationCode)); break;
            case "lotCode": _lotCode = value.Trim().ToUpperInvariant(); OnPropertyChanged(nameof(LotCode)); break;
            case "expiryDate": _expiryDate = value.Trim(); OnPropertyChanged(nameof(ExpiryDate)); break;
            case "unitCost": _unitCost = value.Trim(); OnPropertyChanged(nameof(UnitCost)); break;
        }
    }

    public ManualInboundDraftRowRequest ToRequest() =>
        new(
            Sku.Trim(),
            SourceQuantity.Trim(),
            Optional(UnitCost),
            Optional(LocationCode),
            Optional(LotCode),
            Optional(ManufacturedDate),
            Optional(ExpiryDate),
            Optional(SupplierLotReference));

    private void SetDraft(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value) return;
        field = value;
        OnPropertyChanged(propertyName);
        _changed();
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(Sku));
        OnPropertyChanged(nameof(SourceQuantity));
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(UnitCode));
        OnPropertyChanged(nameof(CurrentCostText));
    }

    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ManualInboundPreviewRowView : INotifyPropertyChanged
{
    private static readonly HashSet<string> AdminCodes = new(StringComparer.Ordinal)
    {
        "INVENTORY_POLICY_UNAVAILABLE",
        "SKU_AMBIGUOUS",
        "BASE_VARIANT_NOT_AVAILABLE",
        "CONVERSION_NOT_CONFIGURED",
        "TRACKING_POLICY_NOT_FOUND"
    };

    private readonly Action<ManualInboundPreviewRowView, string, string> _changed;
    private string _locationCode;
    private string _lotCode;
    private string _expiryDate;
    private string _unitCost;

    public ManualInboundPreviewRowView(
        ManualInboundPreviewRowData data,
        IReadOnlyList<ManualInboundPreviewErrorData> errors,
        IReadOnlyList<ManualInboundLocationOption> locations,
        Action<ManualInboundPreviewRowView, string, string> changed)
    {
        Data = data;
        Errors = errors;
        Locations = locations;
        _changed = changed;
        _locationCode = data.LocationCode ?? string.Empty;
        _lotCode = data.LotCode ?? string.Empty;
        _expiryDate = data.ExpiryDate ?? string.Empty;
        _unitCost = data.UnitCost ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ManualInboundPreviewRowData Data { get; }
    public IReadOnlyList<ManualInboundPreviewErrorData> Errors { get; }
    public IReadOnlyList<ManualInboundLocationOption> Locations { get; }
    public int LineNumber => Data.LineNumber;
    public int[] SourceLineNumbers => Data.SourceLineNumbers;
    public string Sku => Data.Sku;
    public string ProductName => Data.ProductName ?? "—";
    public string UnitCode => Data.SourceUnitCode ?? "—";
    public string Quantity => ManualInboundPresentation.Quantity(Data.SourceQuantity);
    public string CurrentOnHand => Data.CurrentOnHand is null ? "—" : $"{ManualInboundPresentation.Quantity(Data.CurrentOnHand)} {Data.BaseUnitCode}".Trim();
    public string AfterOnHand => Data.AfterOnHand is null ? "—" : $"{ManualInboundPresentation.Quantity(Data.AfterOnHand)} {Data.BaseUnitCode}".Trim();
    public string Warehouse => Data.WarehouseCode ?? "—";
    public string LocationDisplay => string.IsNullOrWhiteSpace(Data.LocationCode)
        ? Data.LocationRequired ? "—" : "Tồn chung"
        : Data.LocationCode;
    public string ErrorText => string.Join(" · ", Errors.Select(item => item.Message));
    public bool NeedsLocationEditor => Data.RequiredFields.Contains("LOCATION", StringComparer.Ordinal) || Errors.Any(item => item.Code == "LOCATION_NOT_FOUND");
    public bool NeedsLotEditor => Data.RequiredFields.Contains("LOT", StringComparer.Ordinal) || Errors.Any(item => item.Code == "LOT_NOT_ALLOWED");
    public bool NeedsExpiryEditor => Data.RequiredFields.Contains("EXPIRY", StringComparer.Ordinal) || Errors.Any(item => item.Code is "EXPIRY_NOT_ALLOWED" or "LOT_EXPIRY_MISMATCH");
    public bool NeedsCostEditor => Data.RequiredFields.Contains("COST", StringComparer.Ordinal);
    public string LotDisplay => string.IsNullOrWhiteSpace(Data.LotCode)
        ? Data.LotTrackingMode == "REQUIRED" ? "—" : "Không quản lý"
        : Data.LotCode;
    public string ExpiryDisplay => string.IsNullOrWhiteSpace(Data.ExpiryDate)
        ? Data.ExpiryTrackingMode == "OPTIONAL" ? "Tùy chọn" : Data.ExpiryTrackingMode == "REQUIRED" ? "—" : "Không quản lý"
        : ManualInboundPresentation.Date(Data.ExpiryDate);
    public string CostDisplay => string.IsNullOrWhiteSpace(Data.UnitCost)
        ? "—"
        : $"{ManualInboundPresentation.Cost(Data.UnitCost)} đ{(Data.CostSource == "CURRENT" ? " · hiện hành" : string.Empty)}";

    public string StatusText
    {
        get
        {
            if (Data.Status == "READY") return "Sẵn sàng";
            if (Errors.Any(item => AdminCodes.Contains(item.Code))) return "Cần quản trị";
            if (Data.RequiredFields.Length > 0) return "Cần bổ sung";
            return "Cần chỉnh";
        }
    }

    public string LocationCode
    {
        get => _locationCode;
        set => SetCorrection(ref _locationCode, value?.Trim().ToUpperInvariant() ?? string.Empty, "locationCode");
    }

    public string LotCode
    {
        get => _lotCode;
        set => SetCorrection(ref _lotCode, value?.Trim().ToUpperInvariant() ?? string.Empty, "lotCode");
    }

    public string ExpiryDate
    {
        get => _expiryDate;
        set => SetCorrection(ref _expiryDate, value?.Trim() ?? string.Empty, "expiryDate");
    }

    public string UnitCost
    {
        get => _unitCost;
        set => SetCorrection(ref _unitCost, value?.Trim() ?? string.Empty, "unitCost");
    }

    private void SetCorrection(ref string field, string value, string fieldName, [CallerMemberName] string? propertyName = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        _changed(this, fieldName, value);
    }
}

public sealed record ManualInboundHistoryRow(ManualInboundHistoryDocumentData Data, bool CanReverse)
{
    public string Title => Data.ReferenceNumber ?? ManualInboundPresentation.InboundType(Data.InboundType);
    public string Status => ManualInboundPresentation.HistoryStatus(Data.Status);
    public string Meta => $"{ManualInboundPresentation.Date(Data.DocumentDate)} · {ManualInboundPresentation.InboundType(Data.InboundType)} · {Data.WarehouseCode} · {Data.WarehouseName}";
}

public sealed record ManualInboundHistoryDetailLineRow(ManualInboundHistoryMovementLineData Data)
{
    public string Product => Data.ProductName ?? Data.Sku;
    public string Sku => Data.Sku;
    public string Unit => Data.BaseUnitCode ?? "—";
    public string Before => ManualInboundPresentation.Quantity(Data.QuantityBefore);
    public string Delta => $"+{ManualInboundPresentation.Quantity(Data.QuantityDelta)}";
    public string After => ManualInboundPresentation.Quantity(Data.QuantityAfter);
}

public sealed class ManualInboundViewModel : INotifyPropertyChanged
{
    private readonly IManualInboundService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _entryMode = "direct";
    private string _warehouseId = string.Empty;
    private string _supplierId = string.Empty;
    private string _inboundType = "MANUAL_RECEIPT";
    private DateTime? _documentDate = DateTime.Today;
    private string _referenceNumber = string.Empty;
    private string _note = string.Empty;
    private string _fileName = string.Empty;
    private string _productSearch = string.Empty;
    private bool _productSearchLoading;
    private int _productSearchRun;
    private ManualInboundPreviewData? _preview;
    private bool _previewDirty;
    private string? _pendingConfirmFingerprint;
    private string? _pendingConfirmKey;
    private string _historyType = string.Empty;
    private string _historyReference = string.Empty;
    private string _historyMessage = string.Empty;
    private bool _historyBusy;
    private ManualInboundHistoryMovementData? _historyDetail;
    private string _historyDetailError = string.Empty;
    private bool _historyDetailBusy;
    private ManualInboundHistoryDocumentData? _reverseDocument;
    private DateTime? _reverseDate;
    private string _reverseReason = string.Empty;
    private bool _reverseBusy;
    private string? _pendingReverseFingerprint;
    private string? _pendingReverseKey;

    public ManualInboundViewModel(
        IManualInboundService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        InboundTypes.Add(new ManualInboundOption("MANUAL_RECEIPT", "Nhập hàng thủ công"));
        InboundTypes.Add(new ManualInboundOption("OFF_DOCUMENT_CUSTOMER_RETURN", "Khách trả ngoài chứng từ"));
        InboundTypes.Add(new ManualInboundOption("RECOVERY", "Hàng thu hồi"));
        InboundTypes.Add(new ManualInboundOption("OTHER", "Khác"));

        HistoryTypes.Add(new ManualInboundOption(string.Empty, "Tất cả"));
        foreach (var item in InboundTypes) HistoryTypes.Add(item);

        Rows.Add(NewRow());
        RefreshRowNumbers();

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            ClearPendingKeys();
            ResetSessionData();
            RaisePermissions();
            if (CanOpen && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
            else if (!_access.Current.IsAuthenticated) ClearMessage();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ManualInboundOption> InboundTypes { get; } = [];
    public ObservableCollection<ManualInboundOption> HistoryTypes { get; } = [];
    public ObservableCollection<ManualInboundWarehouseOption> Warehouses { get; } = [];
    public ObservableCollection<ManualInboundSupplierOption> Suppliers { get; } = [];
    public ObservableCollection<ManualInboundLocationOption> Locations { get; } = [];
    public ObservableCollection<ManualInboundProductResult> ProductResults { get; } = [];
    public ObservableCollection<ManualInboundDraftRowView> Rows { get; } = [];
    public ObservableCollection<ManualInboundPreviewRowView> PreviewRows { get; } = [];
    public ObservableCollection<string> PreviewErrors { get; } = [];
    public ObservableCollection<ManualInboundHistoryRow> HistoryRows { get; } = [];
    public ObservableCollection<ManualInboundHistoryDetailLineRow> HistoryDetailLines { get; } = [];

    public IEnumerable<ManualInboundDraftRowView> DirectRows => Rows.Where(item => !item.IsEmpty);

    public bool CanRead => _access.HasPermission("core.inventory-manual-inbound.read");
    public bool CanPrepare => _access.HasPermission("core.inventory-manual-inbound.prepare");
    public bool CanPost => _access.HasPermission("core.inventory-manual-inbound.post");
    public bool CanReverse => _access.HasPermission("core.inventory-manual-inbound.reverse");
    public bool CanOpen => CanRead || CanPrepare;
    public bool CanUseEntry => CanPrepare;
    public bool CanUseHistory => CanRead;
    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string EntryMode
    {
        get => _entryMode;
        private set
        {
            if (!SetField(ref _entryMode, value)) return;
            OnPropertyChanged(nameof(IsDirectMode));
            OnPropertyChanged(nameof(IsFileMode));
            InvalidatePreview();
        }
    }

    public bool IsDirectMode => EntryMode == "direct";
    public bool IsFileMode => EntryMode == "file";

    public string WarehouseId
    {
        get => _warehouseId;
        set
        {
            if (!SetField(ref _warehouseId, value ?? string.Empty)) return;
            ProductSearch = string.Empty;
            ProductResults.Clear();
            InvalidatePreview();
            _ = LoadLocationsAsync();
            OnPropertyChanged(nameof(SelectedWarehouse));
            OnPropertyChanged(nameof(ManagedLocationNoteVisible));
        }
    }

    public ManualInboundWarehouseOptionData? SelectedWarehouse =>
        Warehouses.FirstOrDefault(item => item.Id == WarehouseId)?.Data;

    public bool ManagedLocationNoteVisible => SelectedWarehouse?.LocationRequired == true;

    public string SupplierId
    {
        get => _supplierId;
        set
        {
            if (SetField(ref _supplierId, value ?? string.Empty)) InvalidatePreview();
        }
    }

    public string InboundType
    {
        get => _inboundType;
        set
        {
            if (!SetField(ref _inboundType, value ?? "MANUAL_RECEIPT")) return;
            InvalidatePreview();
            OnPropertyChanged(nameof(IsOtherType));
        }
    }

    public bool IsOtherType => InboundType == "OTHER";

    public DateTime? DocumentDate
    {
        get => _documentDate;
        set
        {
            if (SetField(ref _documentDate, value)) InvalidatePreview();
        }
    }

    public string ReferenceNumber
    {
        get => _referenceNumber;
        set
        {
            if (SetField(ref _referenceNumber, value)) InvalidatePreview();
        }
    }

    public string Note
    {
        get => _note;
        set
        {
            if (SetField(ref _note, value)) InvalidatePreview();
        }
    }

    public string FileName
    {
        get => _fileName;
        private set => SetField(ref _fileName, value);
    }

    public string ProductSearch
    {
        get => _productSearch;
        set => SetField(ref _productSearch, value);
    }

    public bool ProductSearchLoading
    {
        get => _productSearchLoading;
        private set => SetField(ref _productSearchLoading, value);
    }

    public bool HasProductResults => ProductResults.Count > 0;

    public bool HasPreview => _preview is not null;
    public bool PreviewDirty => _previewDirty;
    public bool PreviewReady => _preview?.Ready == true && !PreviewDirty;
    public string PreviewState => PreviewDirty ? "Cần kiểm tra lại" : _preview?.Ready == true ? "Sẵn sàng" : "Cần xử lý";
    public string PreviewSummary => _preview is null
        ? string.Empty
        : $"{_preview.Totals.ReadyRowCount} dòng sẵn sàng · {_preview.Totals.AttentionRowCount} dòng cần xử lý · Tổng số lượng {ManualInboundPresentation.Quantity(_preview.Totals.SourceQuantityTotal)}";
    public string MergeNote => (_preview?.Totals.MergedDuplicateCount ?? 0) > 0
        ? $"Đã gộp {_preview!.Totals.MergedDuplicateCount} dòng trùng cùng SKU, vị trí, lô và giá vốn."
        : string.Empty;
    public bool HasMergeNote => (_preview?.Totals.MergedDuplicateCount ?? 0) > 0;
    public bool CanCheck => CanPrepare && IsNotBusy;
    public bool CanConfirm => CanPost && IsNotBusy && PreviewReady;

    public string HistoryType
    {
        get => _historyType;
        set => SetField(ref _historyType, value ?? string.Empty);
    }

    public string HistoryReference
    {
        get => _historyReference;
        set => SetField(ref _historyReference, value);
    }

    public string HistoryMessage
    {
        get => _historyMessage;
        private set => SetField(ref _historyMessage, value);
    }

    public bool HistoryBusy
    {
        get => _historyBusy;
        private set => SetField(ref _historyBusy, value);
    }

    public bool HasHistory => HistoryRows.Count > 0;
    public bool HasNoHistory => CanRead && !HistoryBusy && HistoryRows.Count == 0;

    public ManualInboundHistoryMovementData? HistoryDetail
    {
        get => _historyDetail;
        private set
        {
            if (!SetField(ref _historyDetail, value)) return;
            OnPropertyChanged(nameof(HasHistoryDetail));
            OnPropertyChanged(nameof(HistoryDetailTitle));
        }
    }

    public bool HasHistoryDetail => HistoryDetail is not null || HistoryDetailBusy || !string.IsNullOrWhiteSpace(HistoryDetailError);
    public string HistoryDetailTitle => HistoryDetail is null
        ? "Biến động tồn theo chứng từ"
        : $"{HistoryDetail.WarehouseCode} · {HistoryDetail.WarehouseName} · {ManualInboundPresentation.Date(HistoryDetail.DocumentDate)}{(string.IsNullOrWhiteSpace(HistoryDetail.ReferenceNumber) ? string.Empty : $" · {HistoryDetail.ReferenceNumber}")}";

    public string HistoryDetailError
    {
        get => _historyDetailError;
        private set
        {
            if (!SetField(ref _historyDetailError, value)) return;
            OnPropertyChanged(nameof(HasHistoryDetail));
        }
    }

    public bool HistoryDetailBusy
    {
        get => _historyDetailBusy;
        private set
        {
            if (!SetField(ref _historyDetailBusy, value)) return;
            OnPropertyChanged(nameof(HasHistoryDetail));
        }
    }

    public bool IsReverseOpen => _reverseDocument is not null;
    public string ReverseTitle => _reverseDocument is null
        ? string.Empty
        : $"Đảo chứng từ: {_reverseDocument.ReferenceNumber ?? ManualInboundPresentation.InboundType(_reverseDocument.InboundType)}";

    public DateTime? ReverseDate
    {
        get => _reverseDate;
        set
        {
            if (SetField(ref _reverseDate, value)) ClearPendingReverse();
        }
    }

    public string ReverseReason
    {
        get => _reverseReason;
        set
        {
            if (SetField(ref _reverseReason, value)) ClearPendingReverse();
        }
    }

    public bool ReverseBusy
    {
        get => _reverseBusy;
        private set => SetField(ref _reverseBusy, value);
    }

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);

        _initialLoadTask = LoadAsync();
        try { return await _initialLoadTask.ConfigureAwait(true); }
        finally { _initialLoadTask = null; }
    }

    public async Task<bool> LoadAsync()
    {
        var generation = _accessGeneration;
        if (!CanOpen)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền dùng Nhập kho thủ công.");
            return false;
        }

        SetBusy("load");
        ClearMessage();
        var succeeded = true;

        if (CanPrepare)
        {
            try
            {
                var warehouses = await _service.ListWarehousesAsync().ConfigureAwait(true);
                if (generation != _accessGeneration)
                {
                    SetBusy(null);
                    return false;
                }
                Replace(Warehouses, warehouses.Select(item => new ManualInboundWarehouseOption(item)));
                if (Warehouses.Count == 1 && string.IsNullOrWhiteSpace(WarehouseId)) WarehouseId = Warehouses[0].Id;
            }
            catch (Exception exception)
            {
                succeeded = false;
                SetError(exception);
            }

            try
            {
                var suppliers = await _service.ListSuppliersAsync().ConfigureAwait(true);
                if (generation != _accessGeneration)
                {
                    SetBusy(null);
                    return false;
                }
                Replace(Suppliers, suppliers.Select(item => new ManualInboundSupplierOption(item)));
            }
            catch (Exception exception)
            {
                succeeded = false;
                SetError(exception);
            }
        }

        if (CanRead)
        {
            try { await SearchHistoryAsync(clearMainMessage: false).ConfigureAwait(true); }
            catch { succeeded = false; }
        }

        if (generation != _accessGeneration)
        {
            SetBusy(null);
            return false;
        }

        _loaded = succeeded;
        SetBusy(null);
        RaiseState();
        return succeeded;
    }

    public Task<bool> RefreshAsync() => LoadAsync();

    public void SetEntryMode(string mode)
    {
        if (mode is "direct" or "file") EntryMode = mode;
    }

    public async Task SearchProductsAsync(string search, CancellationToken cancellationToken = default)
    {
        ProductSearch = search;
        var run = ++_productSearchRun;
        if (!CanPrepare || !IsDirectMode || string.IsNullOrWhiteSpace(WarehouseId) || string.IsNullOrWhiteSpace(search))
        {
            ProductResults.Clear();
            OnPropertyChanged(nameof(HasProductResults));
            return;
        }

        ProductSearchLoading = true;
        try
        {
            var items = await _service.SearchProductsAsync(WarehouseId, search.Trim(), cancellationToken).ConfigureAwait(true);
            if (run != _productSearchRun || cancellationToken.IsCancellationRequested) return;
            Replace(ProductResults, items.Select(item => new ManualInboundProductResult(item)));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (run == _productSearchRun) SetError(exception);
        }
        finally
        {
            if (run == _productSearchRun) ProductSearchLoading = false;
            OnPropertyChanged(nameof(HasProductResults));
        }
    }

    public void AddProduct(ManualInboundProductResult result)
    {
        if (!CanPrepare) return;

        var empty = Rows.FirstOrDefault(item => item.IsEmpty);
        if (empty is not null)
        {
            empty.ApplyProduct(result.Data);
        }
        else
        {
            var row = NewRow();
            row.ApplyProduct(result.Data);
            Rows.Insert(0, row);
        }

        ProductSearch = string.Empty;
        ProductResults.Clear();
        OnPropertyChanged(nameof(HasProductResults));
        RefreshRowNumbers();
        InvalidatePreview();
    }

    public void AddFileRow()
    {
        Rows.Add(NewRow());
        RefreshRowNumbers();
    }

    public void RemoveRow(ManualInboundDraftRowView row)
    {
        if (!Rows.Remove(row)) return;
        if (Rows.Count == 0) Rows.Add(NewRow());
        RefreshRowNumbers();
        InvalidatePreview();
    }

    public void LoadFile(string path)
    {
        try
        {
            var parsed = ManualInboundFile.Read(path);
            Replace(
                Rows,
                parsed.Select(item => new ManualInboundDraftRowView(
                    OnDraftChanged,
                    item.Sku,
                    item.SourceQuantity,
                    item.UnitCost,
                    item.LocationCode,
                    item.LotCode,
                    item.ManufacturedDate,
                    item.ExpiryDate,
                    item.SupplierLotReference)));
            FileName = Path.GetFileName(path);
            RefreshRowNumbers();
            InvalidatePreview();
            SetNotice($"Đã đọc {parsed.Count} dòng từ {FileName}. Hãy kiểm tra dữ liệu trước khi tiếp tục.");
        }
        catch (Exception exception)
        {
            FileName = string.Empty;
            SetErrorMessage(exception.Message);
        }
    }

    public async Task PreviewAsync()
    {
        if (!CanPrepare || IsBusy) return;
        var request = BuildRequest(validate: true);
        if (request is null) return;

        SetBusy("preview");
        ClearMessage();
        try
        {
            var result = await _service.PreviewAsync(request).ConfigureAwait(true);
            _preview = result;
            _previewDirty = false;
            _pendingConfirmFingerprint = null;
            _pendingConfirmKey = null;

            var errorsByLine = result.RowErrors
                .GroupBy(item => item.LineNumber)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<ManualInboundPreviewErrorData>)group.ToArray());

            foreach (var previewRow in result.Rows)
            {
                foreach (var sourceLine in previewRow.SourceLineNumbers)
                {
                    if (sourceLine < 1 || sourceLine > Rows.Count) continue;
                    Rows[sourceLine - 1].ApplyResolved(previewRow.ProductName, previewRow.SourceUnitCode, previewRow.UnitCost);
                }
            }

            Replace(
                PreviewRows,
                result.Rows.Select(item => new ManualInboundPreviewRowView(
                    item,
                    errorsByLine.GetValueOrDefault(item.LineNumber) ?? [],
                    Locations.ToArray(),
                    ApplyPreviewCorrection)));
            Replace(PreviewErrors, result.RowErrors.Select(item => item.Message).Take(20));

            SetNotice(result.Ready
                ? "Dữ liệu đã sẵn sàng. Kiểm tra chưa làm thay đổi tồn kho."
                : "Còn dòng cần xử lý. Kiểm tra chưa làm thay đổi tồn kho.");
        }
        catch (Exception exception)
        {
            _preview = null;
            PreviewRows.Clear();
            PreviewErrors.Clear();
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaisePreviewState();
        }
    }

    public async Task ConfirmAsync()
    {
        if (!CanConfirm) return;
        var request = BuildRequest(validate: true);
        if (request is null) return;

        var fingerprint = JsonSerializer.Serialize(request);
        if (!string.Equals(_pendingConfirmFingerprint, fingerprint, StringComparison.Ordinal))
        {
            _pendingConfirmFingerprint = fingerprint;
            _pendingConfirmKey = _idempotencyKeys.Create("manual-inbound-confirm");
        }

        SetBusy("confirm");
        ClearMessage();
        try
        {
            await _service.ConfirmAsync(request, _pendingConfirmKey!).ConfigureAwait(true);
            _pendingConfirmFingerprint = null;
            _pendingConfirmKey = null;
            Replace(Rows, [NewRow()]);
            RefreshRowNumbers();
            FileName = string.Empty;
            SupplierId = string.Empty;
            ReferenceNumber = string.Empty;
            Note = string.Empty;
            _preview = null;
            _previewDirty = false;
            PreviewRows.Clear();
            PreviewErrors.Clear();
            SetNotice("Đã xác nhận nhập kho. Tồn kho đã được cập nhật.");
            if (CanRead) await SearchHistoryAsync(clearMainMessage: false).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
        finally
        {
            SetBusy(null);
            RaisePreviewState();
        }
    }

    public async Task SearchHistoryAsync(bool clearMainMessage = true)
    {
        if (!CanRead) return;
        HistoryBusy = true;
        HistoryMessage = string.Empty;
        if (clearMainMessage) ClearMessage();

        try
        {
            var documents = await _service.SearchHistoryAsync(HistoryType, HistoryReference).ConfigureAwait(true);
            Replace(
                HistoryRows,
                documents.Take(12).Select(item => new ManualInboundHistoryRow(item, CanReverse && item.Status == "POSTED")));
        }
        catch (Exception exception)
        {
            HistoryMessage = OfficeError(exception);
            if (clearMainMessage) SetError(exception);
        }
        finally
        {
            HistoryBusy = false;
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(HasNoHistory));
        }
    }

    public async Task OpenHistoryDetailAsync(ManualInboundHistoryRow row)
    {
        if (!CanRead) return;

        HistoryDetail = null;
        HistoryDetailError = string.Empty;
        HistoryDetailBusy = true;
        HistoryDetailLines.Clear();
        try
        {
            var detail = await _service.ReadHistoryDetailAsync(row.Data.Id).ConfigureAwait(true);
            HistoryDetail = detail;
            Replace(HistoryDetailLines, detail.Lines.Select(item => new ManualInboundHistoryDetailLineRow(item)));
        }
        catch (Exception exception)
        {
            HistoryDetailError = OfficeError(exception);
        }
        finally
        {
            HistoryDetailBusy = false;
        }
    }

    public void CloseHistoryDetail()
    {
        HistoryDetail = null;
        HistoryDetailError = string.Empty;
        HistoryDetailLines.Clear();
    }

    public void OpenReverse(ManualInboundHistoryRow row)
    {
        if (!row.CanReverse) return;
        _reverseDocument = row.Data;
        ReverseDate = null;
        ReverseReason = string.Empty;
        ClearPendingReverse();
        OnPropertyChanged(nameof(IsReverseOpen));
        OnPropertyChanged(nameof(ReverseTitle));
    }

    public void CloseReverse()
    {
        _reverseDocument = null;
        ReverseDate = null;
        ReverseReason = string.Empty;
        ClearPendingReverse();
        OnPropertyChanged(nameof(IsReverseOpen));
        OnPropertyChanged(nameof(ReverseTitle));
    }

    public async Task ReverseAsync()
    {
        if (_reverseDocument is null || !CanReverse || ReverseBusy) return;
        if (ReverseDate is null)
        {
            HistoryMessage = "Chọn ngày đảo chứng từ.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ReverseReason))
        {
            HistoryMessage = "Nhập lý do đảo chứng từ.";
            return;
        }

        var date = ReverseDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var reason = ReverseReason.Trim();
        var fingerprint = $"{_reverseDocument.Id}:{date}:{reason}";
        if (!string.Equals(_pendingReverseFingerprint, fingerprint, StringComparison.Ordinal))
        {
            _pendingReverseFingerprint = fingerprint;
            _pendingReverseKey = _idempotencyKeys.Create("manual-inbound-reverse");
        }

        ReverseBusy = true;
        HistoryMessage = string.Empty;
        try
        {
            await _service.ReverseAsync(
                _reverseDocument.Id,
                new ManualInboundReverseRequest(date, reason),
                _pendingReverseKey!).ConfigureAwait(true);
            ClearPendingReverse();
            _reverseDocument = null;
            OnPropertyChanged(nameof(IsReverseOpen));
            OnPropertyChanged(nameof(ReverseTitle));
            HistoryMessage = "Đã đảo chứng từ. Lịch sử nhập kho vẫn được giữ nguyên.";
            await SearchHistoryAsync(clearMainMessage: false).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            HistoryMessage = OfficeError(exception);
        }
        finally
        {
            ReverseBusy = false;
        }
    }

    private async Task LoadLocationsAsync()
    {
        Locations.Clear();
        if (!CanPrepare || string.IsNullOrWhiteSpace(WarehouseId)) return;

        try
        {
            var response = await _service.ListLocationsAsync(WarehouseId).ConfigureAwait(true);
            Replace(Locations, response.Locations.Select(item => new ManualInboundLocationOption(item)));
        }
        catch (Exception exception)
        {
            SetError(exception);
        }
    }

    private ManualInboundOperatorRequest? BuildRequest(bool validate)
    {
        var activeRows = Rows.Where(item => !item.IsEmpty).ToArray();
        if (validate)
        {
            if (string.IsNullOrWhiteSpace(WarehouseId))
            {
                SetErrorMessage("Chọn kho nhập trước khi kiểm tra.");
                return null;
            }

            if (DocumentDate is null)
            {
                SetErrorMessage("Nhập ngày chứng từ trước khi kiểm tra.");
                return null;
            }

            if (InboundType == "OTHER" && string.IsNullOrWhiteSpace(Note))
            {
                SetErrorMessage("Loại “Khác” cần có ghi chú.");
                return null;
            }

            if (activeRows.Length == 0 || activeRows.Any(item => string.IsNullOrWhiteSpace(item.Sku) || string.IsNullOrWhiteSpace(item.SourceQuantity)))
            {
                SetErrorMessage("Mỗi dòng hàng cần có sản phẩm và số lượng.");
                return null;
            }

            if (activeRows.Length > ManualInboundFile.MaxRows)
            {
                SetErrorMessage($"Mỗi lần kiểm tra tối đa {ManualInboundFile.MaxRows} dòng.");
                return null;
            }
        }

        return new ManualInboundOperatorRequest(
            WarehouseId,
            Optional(SupplierId),
            InboundType,
            DocumentDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Optional(ReferenceNumber),
            Optional(Note),
            activeRows.Select(item => item.ToRequest()).ToArray());
    }

    private void ApplyPreviewCorrection(ManualInboundPreviewRowView row, string field, string value)
    {
        foreach (var lineNumber in row.SourceLineNumbers)
        {
            if (lineNumber < 1 || lineNumber > Rows.Count) continue;
            Rows[lineNumber - 1].ApplyCorrection(field, value);
        }

        _previewDirty = true;
        _pendingConfirmFingerprint = null;
        _pendingConfirmKey = null;
        SetNotice("Đã bổ sung thông tin. Bấm Kiểm tra dữ liệu lại trước khi xác nhận nhập.");
        RaisePreviewState();
    }

    private void OnDraftChanged()
    {
        RefreshRowNumbers();
        InvalidatePreview();
    }

    private void RefreshRowNumbers()
    {
        for (var index = 0; index < Rows.Count; index++) Rows[index].SetSequence(index + 1);
        OnPropertyChanged(nameof(DirectRows));
    }

    private void InvalidatePreview()
    {
        _preview = null;
        _previewDirty = false;
        PreviewRows.Clear();
        PreviewErrors.Clear();
        _pendingConfirmFingerprint = null;
        _pendingConfirmKey = null;
        RaisePreviewState();
    }

    private ManualInboundDraftRowView NewRow() => new(OnDraftChanged);

    private void ResetSessionData()
    {
        Warehouses.Clear();
        Suppliers.Clear();
        Locations.Clear();
        ProductResults.Clear();
        PreviewRows.Clear();
        PreviewErrors.Clear();
        HistoryRows.Clear();
        HistoryDetailLines.Clear();
        Replace(Rows, [NewRow()]);
        RefreshRowNumbers();
        WarehouseId = string.Empty;
        SupplierId = string.Empty;
        InboundType = "MANUAL_RECEIPT";
        DocumentDate = DateTime.Today;
        ReferenceNumber = string.Empty;
        Note = string.Empty;
        FileName = string.Empty;
        ProductSearch = string.Empty;
        _preview = null;
        _previewDirty = false;
        HistoryMessage = string.Empty;
        HistoryDetail = null;
        HistoryDetailError = string.Empty;
        _reverseDocument = null;
        ReverseDate = null;
        ReverseReason = string.Empty;
        ClearPendingKeys();
        RaiseState();
    }

    private void ClearPendingKeys()
    {
        _pendingConfirmFingerprint = null;
        _pendingConfirmKey = null;
        ClearPendingReverse();
    }

    private void ClearPendingReverse()
    {
        _pendingReverseFingerprint = null;
        _pendingReverseKey = null;
    }

    private void RaisePreviewState()
    {
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(PreviewDirty));
        OnPropertyChanged(nameof(PreviewReady));
        OnPropertyChanged(nameof(PreviewState));
        OnPropertyChanged(nameof(PreviewSummary));
        OnPropertyChanged(nameof(MergeNote));
        OnPropertyChanged(nameof(HasMergeNote));
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanConfirm));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanPrepare));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(CanReverse));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanUseEntry));
        OnPropertyChanged(nameof(CanUseHistory));
        RaisePreviewState();
    }

    private void RaiseState()
    {
        RaisePermissions();
        OnPropertyChanged(nameof(SelectedWarehouse));
        OnPropertyChanged(nameof(ManagedLocationNoteVisible));
        OnPropertyChanged(nameof(IsOtherType));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HasNoHistory));
    }

    private void SetBusy(string? action)
    {
        if (_busyAction == action) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        RaisePreviewState();
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetNotice(string message)
    {
        MessageIsError = false;
        Message = message;
    }

    private void SetErrorMessage(string message)
    {
        MessageIsError = true;
        Message = message;
    }

    private void SetError(Exception exception) => SetErrorMessage(OfficeError(exception));

    private static string OfficeError(Exception exception)
    {
        if (exception is not CanonicalApiException apiException) return exception.Message;

        var message = apiException.Code switch
        {
            "INVENTORY_POLICY_UNAVAILABLE" => "Chính sách quản lý tồn chưa sẵn sàng. Hãy kiểm tra cấu hình sản phẩm trước khi nhập.",
            "WAREHOUSE_LOCATION_MODE_REQUIRED" => "Kho chưa thiết lập chế độ quản lý vị trí.",
            "WAREHOUSE_SCOPE_DENIED" => "Kho nằm ngoài phạm vi được cấp.",
            "SKU_NOT_INVENTORY_MANAGED" => "Có mã hàng không quản lý tồn nên không thể nhập kho.",
            "INVENTORY_REVERSAL_DOWNSTREAM_CONFLICT" => "Tồn kho đã phát sinh giao dịch sau chứng từ này. Không thể đảo chứng từ.",
            _ => CanonicalErrorMessages.ToOfficeMessage(apiException)
        };

        return CanonicalErrorMessages.WithRequestId(message, apiException.RequestId);
    }

    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
