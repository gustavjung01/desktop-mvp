using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Purchasing;

public sealed class PurchasePriceViewModel : INotifyPropertyChanged
{
    private const string Read = "core.supplier-purchase-price.read";
    private const string Manage = "core.supplier-purchase-price.manage";

    private readonly ISupplierPurchasePriceService _service;
    private readonly IPartnerService _partners;
    private readonly IPurchaseOrderService _purchaseOrders;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dispatcher _uiDispatcher;
    private readonly List<SupplierPurchasePriceData> _allPrices = [];

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _supplierFilter = string.Empty;
    private bool _isEditorOpen;
    private SupplierPurchasePriceData? _editing;
    private string? _createAttemptKey;
    private string _selectedSupplierId = string.Empty;
    private string _selectedVariantId = string.Empty;
    private string _selectedUnitId = string.Empty;
    private string _selectedSkuText = string.Empty;
    private string _skuSearchText = string.Empty;
    private string _unitPrice = string.Empty;
    private string _currencyCode = "VND";
    private string _minQuantity = "0";
    private DateTime? _effectiveFrom = DateTime.Today;
    private DateTime? _effectiveTo;
    private string _supplierSku = string.Empty;
    private string _sourceReference = string.Empty;
    private string _note = string.Empty;
    private bool _isActive = true;

    public PurchasePriceViewModel(
        ISupplierPurchasePriceService service,
        IPartnerService partners,
        IPurchaseOrderService purchaseOrders,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _partners = partners;
        _purchaseOrders = purchaseOrders;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        Suppliers.Add(new PurchasePriceSupplierOption(string.Empty, "Tất cả nhà cung cấp"));
        _access.Changed += (_, _) =>
        {
            if (_uiDispatcher.CheckAccess())
            {
                HandleAccessChanged();
                return;
            }

            _ = _uiDispatcher.BeginInvoke((Action)HandleAccessChanged);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PurchasePriceSupplierOption> Suppliers { get; } = [];
    public ObservableCollection<PurchasePriceRow> Rows { get; } = [];
    public ObservableCollection<PurchasePriceSkuRow> SkuResults { get; } = [];

    public bool CanRead => _access.HasPermission(Read);
    public bool CanManage => _access.HasPermission(Manage);
    public bool CanSave => IsEditorOpen && CanManage && !IsBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(RefreshButtonText));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string Message { get => _message; private set => Set(ref _message, value ?? string.Empty); }
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }
    public string RefreshButtonText => IsBusy ? "Đang cập nhật…" : "Cập nhật dữ liệu";
    public string SaveButtonText => IsBusy ? "Đang lưu…" : "Lưu giá mua";
    public string EditorTitle => _editing is null ? "Giá mua theo nhà cung cấp" : $"{_editing.Sku} — {_editing.SupplierCode}";
    public string EditorModeText => _editing is null ? "THIẾT LẬP MỚI" : "CHỈNH SỬA GIÁ MUA";

    public string SupplierFilter
    {
        get => _supplierFilter;
        set
        {
            if (!Set(ref _supplierFilter, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        private set
        {
            if (Set(ref _isEditorOpen, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string SelectedSupplierId { get => _selectedSupplierId; set { if (Set(ref _selectedSupplierId, value ?? string.Empty)) MarkDraftChanged(); } }
    public string SelectedSkuText { get => _selectedSkuText; private set => Set(ref _selectedSkuText, value); }
    public string SkuSearchText { get => _skuSearchText; set => Set(ref _skuSearchText, value ?? string.Empty); }
    public string UnitPrice { get => _unitPrice; set { if (Set(ref _unitPrice, value ?? string.Empty)) MarkDraftChanged(); } }
    public string CurrencyCode { get => _currencyCode; set { if (Set(ref _currencyCode, (value ?? string.Empty).ToUpperInvariant())) MarkDraftChanged(); } }
    public string MinQuantity { get => _minQuantity; set { if (Set(ref _minQuantity, value ?? string.Empty)) MarkDraftChanged(); } }
    public DateTime? EffectiveFrom { get => _effectiveFrom; set { if (Set(ref _effectiveFrom, value)) MarkDraftChanged(); } }
    public DateTime? EffectiveTo { get => _effectiveTo; set { if (Set(ref _effectiveTo, value)) MarkDraftChanged(); } }
    public string SupplierSku { get => _supplierSku; set { if (Set(ref _supplierSku, value ?? string.Empty)) MarkDraftChanged(); } }
    public string SourceReference { get => _sourceReference; set { if (Set(ref _sourceReference, value ?? string.Empty)) MarkDraftChanged(); } }
    public string Note { get => _note; set { if (Set(ref _note, value ?? string.Empty)) MarkDraftChanged(); } }
    public bool IsActive { get => _isActive; set { if (Set(ref _isActive, value)) MarkDraftChanged(); } }

    public string TotalCount => PurchasePricePresentation.Number(_allPrices.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string ActiveCount => PurchasePricePresentation.Number(_allPrices.Count(x => x.IsActive).ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string SupplierCount => PurchasePricePresentation.Number(_allPrices.Select(x => x.SupplierId).Distinct(StringComparer.Ordinal).Count().ToString(System.Globalization.CultureInfo.InvariantCulture));
    public string VisibleCountText => $"{Rows.Count} dòng";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Bảng giá mua.");
            return;
        }

        IsBusy = true;
        try
        {
            var pricesTask = _service.ListAsync();
            var suppliersTask = _partners.ListSuppliersAsync();
            await Task.WhenAll(pricesTask, suppliersTask).ConfigureAwait(true);

            _allPrices.Clear();
            _allPrices.AddRange(await pricesTask.ConfigureAwait(true));

            Suppliers.Clear();
            Suppliers.Add(new PurchasePriceSupplierOption(string.Empty, "Tất cả nhà cung cấp"));
            foreach (var supplier in (await suppliersTask.ConfigureAwait(true))
                         .Where(x => x.IsActive)
                         .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                Suppliers.Add(new PurchasePriceSupplierOption(supplier.Id, $"{supplier.Code} — {supplier.Name}"));
            }

            ApplyFilter();
            _loaded = true;
            SetNotice("Bảng giá mua đã được cập nhật.");
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tải được bảng giá mua.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void BeginCreate()
    {
        if (!CanManage)
        {
            SetError("Tài khoản chưa được cấp quyền quản lý Bảng giá mua.");
            return;
        }

        _editing = null;
        SelectedSupplierId = string.IsNullOrWhiteSpace(SupplierFilter)
            ? Suppliers.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Id))?.Id ?? string.Empty
            : SupplierFilter;
        _selectedVariantId = string.Empty;
        _selectedUnitId = string.Empty;
        SelectedSkuText = "Chưa chọn SKU";
        SkuSearchText = string.Empty;
        SkuResults.Clear();
        UnitPrice = string.Empty;
        CurrencyCode = "VND";
        MinQuantity = "0";
        EffectiveFrom = DateTime.Today;
        EffectiveTo = null;
        SupplierSku = string.Empty;
        SourceReference = string.Empty;
        Note = string.Empty;
        IsActive = true;
        _createAttemptKey = _idempotencyKeys.Create("supplier-purchase-price-create");
        IsEditorOpen = true;
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorModeText));
    }

    public void BeginEdit(PurchasePriceRow row)
    {
        if (!row.CanEdit) return;
        var price = row.Data;
        _editing = price;
        _createAttemptKey = null;
        _selectedSupplierId = price.SupplierId;
        _selectedVariantId = price.VariantId;
        _selectedUnitId = price.UnitId;
        _selectedSkuText = $"{price.Sku} — {price.ProductName} · {price.UnitCode}";
        _skuSearchText = price.Sku;
        _unitPrice = price.UnitPrice;
        _currencyCode = price.CurrencyCode;
        _minQuantity = price.MinQuantity;
        _effectiveFrom = DateTime.TryParse(price.EffectiveFrom, out var from) ? from : DateTime.Today;
        _effectiveTo = DateTime.TryParse(price.EffectiveTo, out var to) ? to : null;
        _supplierSku = price.SupplierSku ?? string.Empty;
        _sourceReference = price.SourceReference ?? string.Empty;
        _note = price.Note ?? string.Empty;
        _isActive = price.IsActive;
        SkuResults.Clear();
        IsEditorOpen = true;
        RaiseEditorFields();
    }

    public void CloseEditor()
    {
        if (IsBusy) return;
        IsEditorOpen = false;
        _editing = null;
        _createAttemptKey = null;
        SkuResults.Clear();
    }

    public async Task SearchSkuAsync()
    {
        var term = SkuSearchText.Trim();
        if (!IsEditorOpen || term.Length < 1) return;
        IsBusy = true;
        try
        {
            var results = await _purchaseOrders.SearchSkuAsync(term, 30, 0).ConfigureAwait(true);
            SkuResults.Clear();
            foreach (var option in results.Where(x => x.Eligibility.Selectable && !string.IsNullOrWhiteSpace(x.UnitId)))
            {
                SkuResults.Add(new PurchasePriceSkuRow(
                    option,
                    option.Sku,
                    string.IsNullOrWhiteSpace(option.VariantName) ? option.ProductName : $"{option.ProductName} · {option.VariantName}",
                    option.UnitCode ?? option.UnitName ?? "—",
                    option.Eligibility.Message,
                    true));
            }
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không tìm được SKU.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectSku(PurchasePriceSkuRow row)
    {
        if (!row.Selectable) return;
        _selectedVariantId = row.Data.Id;
        _selectedUnitId = row.Data.UnitId ?? string.Empty;
        SelectedSkuText = $"{row.Sku} — {row.Name} · {row.Unit}";
        SkuSearchText = row.Sku;
        SkuResults.Clear();
        MarkDraftChanged();
    }

    public async Task SaveAsync()
    {
        if (!CanSave) return;
        var validation = Validate();
        if (validation is not null)
        {
            SetError(validation);
            return;
        }

        var request = new SupplierPurchasePriceRequest
        {
            SupplierId = SelectedSupplierId,
            VariantId = _selectedVariantId,
            UnitId = _selectedUnitId,
            UnitPrice = UnitPrice.Trim().Replace(',', '.'),
            CurrencyCode = CurrencyCode.Trim().ToUpperInvariant(),
            MinQuantity = string.IsNullOrWhiteSpace(MinQuantity) ? "0" : MinQuantity.Trim().Replace(',', '.'),
            EffectiveFrom = EffectiveFrom!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            EffectiveTo = EffectiveTo?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            SupplierSku = NullIfWhiteSpace(SupplierSku),
            SourceReference = NullIfWhiteSpace(SourceReference),
            Note = NullIfWhiteSpace(Note),
            IsActive = IsActive,
            ExpectedRevision = _editing?.Revision
        };

        IsBusy = true;
        try
        {
            SupplierPurchasePriceData saved;
            if (_editing is null)
            {
                _createAttemptKey ??= _idempotencyKeys.Create("supplier-purchase-price-create");
                saved = await _service.CreateAsync(request, _createAttemptKey).ConfigureAwait(true);
            }
            else
            {
                saved = await _service.UpdateAsync(_editing.Id, request).ConfigureAwait(true);
            }

            var index = _allPrices.FindIndex(x => x.Id == saved.Id);
            if (index < 0) _allPrices.Insert(0, saved);
            else _allPrices[index] = saved;
            IsEditorOpen = false;
            _editing = null;
            _createAttemptKey = null;
            ApplyFilter();
            SetNotice("Giá mua đã được lưu.");
        }
        catch (Exception exception)
        {
            HandleException(exception, "Không lưu được giá mua.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void MarkDraftChanged()
    {
        if (IsEditorOpen && _editing is null)
        {
            _createAttemptKey = _idempotencyKeys.Create("supplier-purchase-price-create");
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(SelectedSupplierId)) return "Vui lòng chọn nhà cung cấp.";
        if (string.IsNullOrWhiteSpace(_selectedVariantId) || string.IsNullOrWhiteSpace(_selectedUnitId)) return "Vui lòng chọn một SKU mua hàng hợp lệ.";
        if (!PurchasePricePresentation.TryPositiveDecimal(UnitPrice.Replace(',', '.'), out _)) return "Giá mua phải lớn hơn 0.";
        if (CurrencyCode.Trim().Length != 3 || !CurrencyCode.Trim().All(char.IsLetter)) return "Tiền tệ phải gồm 3 chữ cái.";
        if (!PurchasePricePresentation.TryNonNegativeDecimal((string.IsNullOrWhiteSpace(MinQuantity) ? "0" : MinQuantity).Replace(',', '.'), out _)) return "Số lượng tối thiểu phải từ 0.";
        if (EffectiveFrom is null) return "Vui lòng chọn ngày bắt đầu hiệu lực.";
        if (EffectiveTo is not null && EffectiveTo.Value.Date < EffectiveFrom.Value.Date) return "Ngày kết thúc không được trước ngày bắt đầu.";
        if (SupplierSku.Trim().Length > 128) return "Mã SKU nhà cung cấp không được vượt quá 128 ký tự.";
        if (SourceReference.Trim().Length > 256) return "Tham chiếu thỏa thuận không được vượt quá 256 ký tự.";
        if (Note.Trim().Length > 2000) return "Ghi chú không được vượt quá 2000 ký tự.";
        return null;
    }

    private void ApplyFilter()
    {
        var visible = _allPrices
            .Where(x => string.IsNullOrWhiteSpace(SupplierFilter) || x.SupplierId == SupplierFilter)
            .OrderBy(x => x.SupplierCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(x => x.EffectiveFrom, StringComparer.Ordinal)
            .ToArray();

        Rows.Clear();
        for (var i = 0; i < visible.Length; i++)
        {
            var price = visible[i];
            Rows.Add(new PurchasePriceRow(
                price,
                i + 1,
                $"{price.SupplierCode} — {price.SupplierName}",
                $"{price.Sku} — {price.ProductName}",
                price.UnitCode,
                PurchasePricePresentation.Money(price.UnitPrice, price.CurrencyCode),
                PurchasePricePresentation.Number(price.MinQuantity),
                PurchasePricePresentation.DateRange(price.EffectiveFrom, price.EffectiveTo),
                string.IsNullOrWhiteSpace(price.SupplierSku) ? "—" : price.SupplierSku,
                price.IsActive ? "Đang dùng" : "Ngừng dùng",
                CanManage));
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(SupplierCount));
        OnPropertyChanged(nameof(VisibleCountText));
    }

    private void HandleAccessChanged()
    {
        _loaded = false;
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(CanSave));
        ApplyFilter();
    }

    private void RaiseEditorFields()
    {
        OnPropertyChanged(nameof(SelectedSupplierId));
        OnPropertyChanged(nameof(SelectedSkuText));
        OnPropertyChanged(nameof(SkuSearchText));
        OnPropertyChanged(nameof(UnitPrice));
        OnPropertyChanged(nameof(CurrencyCode));
        OnPropertyChanged(nameof(MinQuantity));
        OnPropertyChanged(nameof(EffectiveFrom));
        OnPropertyChanged(nameof(EffectiveTo));
        OnPropertyChanged(nameof(SupplierSku));
        OnPropertyChanged(nameof(SourceReference));
        OnPropertyChanged(nameof(Note));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorModeText));
        OnPropertyChanged(nameof(CanSave));
    }

    private void SetNotice(string message) { Message = message; MessageIsError = false; }
    private void SetError(string message) { Message = message; MessageIsError = true; }

    private void HandleException(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException api)
        {
            SetError(CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId));
            return;
        }

        SetError(string.IsNullOrWhiteSpace(exception.Message) ? fallback : exception.Message);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
