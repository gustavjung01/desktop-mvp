using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Sales;

public sealed class SalesQuotationViewModel : INotifyPropertyChanged
{
    private const string SalesOrderRead = "core.sales-order.read";
    private const string ProductRead = "core.product.read";
    private const string PriceRead = "core.price.read";
    private const string CustomerRead = "core.customer.read";
    private const int VariantBatchSize = 500;
    private const int MaxQuotationSkus = 1000;
    private const int SearchPageSize = 30;

    public static readonly string[] ExportHeaders =
        ["Mã hàng", "Sản phẩm", "Quy cách", "Số lượng", "Tiền tệ", "Đơn giá", "Thành tiền", "Nguồn giá"];

    private readonly IDataExchangeService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private ProductCategoryData[] _categories = [];
    private SalesChannelData[] _channels = [];
    private DataExchangeCustomerGroupData[] _groups = [];
    private DataExchangeCustomerData[] _customers = [];
    private bool _loaded;
    private bool _isLoadingReferences;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private string _scope = "all";
    private string _categoryId = string.Empty;
    private string _channelId = string.Empty;
    private string _customerGroupId = string.Empty;
    private string _quantity = "1";
    private string _customerSearchText = string.Empty;
    private DataExchangeCustomerData? _selectedCustomer;
    private string _skuSearchText = string.Empty;
    private bool _skuSearching;
    private CancellationTokenSource? _skuSearchCancellation;
    private string? _operationKey;

    public SalesQuotationViewModel(
        IDataExchangeService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;
        _access.Changed += (_, _) =>
        {
            _loaded = false;
            _operationKey = null;
            ClearReferenceData();
            ClearResults();
            OnPropertyChanged(nameof(CanOpen));
            RaiseActionState();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SalesQuotationChoice> Categories { get; } = [];
    public ObservableCollection<SalesQuotationChoice> Channels { get; } = [];
    public ObservableCollection<SalesQuotationChoice> CustomerGroups { get; } = [];
    public ObservableCollection<SalesQuotationCustomerOption> CustomerResults { get; } = [];
    public ObservableCollection<SalesQuotationSkuOption> SkuResults { get; } = [];
    public ObservableCollection<SalesQuotationSkuOption> SelectedSkus { get; } = [];
    public ObservableCollection<SalesQuotationRowView> Rows { get; } = [];

    public bool CanOpen =>
        _access.HasPermission(SalesOrderRead)
        && _access.HasPermission(ProductRead)
        && _access.HasPermission(PriceRead)
        && _access.HasPermission(CustomerRead);

    public bool IsLoadingReferences
    {
        get => _isLoadingReferences;
        private set
        {
            if (!SetField(ref _isLoadingReferences, value)) return;
            RaiseActionState();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseActionState();
        }
    }

    public bool CanBuild => CanOpen && !IsLoadingReferences && !IsBusy;
    public bool CanExport => Rows.Count > 0 && !IsBusy;
    public bool HasRows => Rows.Count > 0;
    public bool NoRows => Rows.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public string StatusText => IsLoadingReferences ? "Đang chuẩn bị dữ liệu…" : "Sẵn sàng tính báo giá";

    public string Scope
    {
        get => _scope;
        set
        {
            var normalized = value is "category" or "sku" ? value : "all";
            if (!SetField(ref _scope, normalized)) return;
            SkuSearchText = string.Empty;
            SkuResults.Clear();
            InvalidateResult();
            RaiseScopeState();
        }
    }

    public bool IsAllScope
    {
        get => Scope == "all";
        set { if (value) Scope = "all"; }
    }

    public bool IsCategoryScope
    {
        get => Scope == "category";
        set { if (value) Scope = "category"; }
    }

    public bool IsSkuScope
    {
        get => Scope == "sku";
        set { if (value) Scope = "sku"; }
    }

    public string CategoryId
    {
        get => _categoryId;
        set
        {
            if (!SetField(ref _categoryId, value ?? string.Empty)) return;
            InvalidateResult();
            OnPropertyChanged(nameof(ScopeDescription));
        }
    }

    public string ChannelId
    {
        get => _channelId;
        set
        {
            if (!SetField(ref _channelId, value ?? string.Empty)) return;
            InvalidateResult();
        }
    }

    public string CustomerGroupId
    {
        get => _customerGroupId;
        set
        {
            if (!SetField(ref _customerGroupId, value ?? string.Empty)) return;
            if (SelectedCustomer is null) InvalidateResult();
        }
    }

    public string Quantity
    {
        get => _quantity;
        set
        {
            if (!SetField(ref _quantity, value ?? string.Empty)) return;
            InvalidateResult();
        }
    }

    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            if (!SetField(ref _customerSearchText, value ?? string.Empty)) return;
            RebuildCustomerResults();
            OnPropertyChanged(nameof(IsCustomerSearchActive));
            OnPropertyChanged(nameof(CustomerSearchEmpty));
        }
    }

    public bool IsCustomerSearchActive => !string.IsNullOrWhiteSpace(CustomerSearchText);
    public bool CustomerSearchEmpty => IsCustomerSearchActive && CustomerResults.Count == 0;

    public DataExchangeCustomerData? SelectedCustomer
    {
        get => _selectedCustomer;
        private set
        {
            if (!SetField(ref _selectedCustomer, value)) return;
            OnPropertyChanged(nameof(HasSelectedCustomer));
            OnPropertyChanged(nameof(SelectedCustomerDisplay));
            OnPropertyChanged(nameof(CustomerGroupEnabled));
            OnPropertyChanged(nameof(ResultContext));
        }
    }

    public bool HasSelectedCustomer => SelectedCustomer is not null;
    public bool CustomerGroupEnabled => SelectedCustomer is null;
    public string SelectedCustomerDisplay =>
        SelectedCustomer is null ? string.Empty : $"{SelectedCustomer.Name} · {SelectedCustomer.Code}";
    public string ResultContext =>
        SelectedCustomer is null ? "Khách chung" : $"{SelectedCustomer.Code} · {SelectedCustomer.Name}";

    public string SkuSearchText
    {
        get => _skuSearchText;
        set
        {
            if (!SetField(ref _skuSearchText, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(IsSkuSearchActive));
            _ = SearchSkuAsync();
        }
    }

    public bool IsSkuSearchActive => IsSkuScope && !string.IsNullOrWhiteSpace(SkuSearchText);

    public bool SkuSearching
    {
        get => _skuSearching;
        private set
        {
            if (!SetField(ref _skuSearching, value)) return;
            OnPropertyChanged(nameof(SkuSearchStatus));
        }
    }

    public string SkuSearchStatus =>
        SkuSearching ? "Đang tìm hàng hóa…"
        : !IsSkuSearchActive ? "Gõ tên sản phẩm, mã hàng, SKU hoặc barcode để tìm."
        : SkuResults.Count == 0 ? "Không tìm thấy hàng hóa phù hợp."
        : $"{SkuResults.Count} kết quả";

    public string ScopeDescription
    {
        get
        {
            if (Scope == "sku")
                return SelectedSkus.Count == 0 ? "Chưa chọn mã hàng" : $"{SelectedSkus.Count:N0} mã hàng đã chọn";
            if (Scope == "category")
            {
                var selected = Categories.FirstOrDefault(item => item.Id == CategoryId);
                return selected is null || string.IsNullOrWhiteSpace(selected.Id) ? "Chưa chọn nhóm sản phẩm" : selected.Display;
            }
            return "Toàn bộ mã hàng đang bán";
        }
    }

    public string RowCountText => Rows.Count.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    public string PricedCountText => Rows.Count(row => !string.IsNullOrWhiteSpace(row.EffectiveUnitPrice))
        .ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public string TotalValueText
    {
        get
        {
            BigInteger total = 0;
            foreach (var row in Rows)
                if (BigInteger.TryParse(row.EffectiveLineTotal, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                    total += value;
            return FormatMoney(total.ToString(CultureInfo.InvariantCulture), "VND");
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsLoadingReferences || !CanOpen) return;
        IsLoadingReferences = true;
        SetMessage(string.Empty);
        try
        {
            var categoryTask = _service.ListCategoriesAsync();
            var channelTask = _service.ListSalesChannelsAsync();
            var groupTask = _service.ListCustomerGroupsAsync();
            var customerTask = _service.ListCustomersAsync();
            await Task.WhenAll(categoryTask, channelTask, groupTask, customerTask).ConfigureAwait(true);

            _categories = [.. categoryTask.Result];
            _channels = [.. channelTask.Result];
            _groups = [.. groupTask.Result];
            _customers = [.. customerTask.Result];
            RebuildReferenceChoices();
            _loaded = true;
        }
        catch (Exception exception)
        {
            SetMessage(string.IsNullOrWhiteSpace(exception.Message) ? "Không tải được dữ liệu để lập báo giá." : exception.Message, true);
        }
        finally
        {
            IsLoadingReferences = false;
        }
    }

    public async Task RefreshAsync()
    {
        _loaded = false;
        ClearReferenceData();
        await EnsureLoadedAsync().ConfigureAwait(true);
    }

    public void SelectCustomer(SalesQuotationCustomerOption option)
    {
        var customer = _customers.FirstOrDefault(item => item.Id == option.Id);
        if (customer is null || !customer.IsActive) return;

        InvalidateResult();
        SelectedCustomer = customer;
        _customerGroupId = customer.GroupId ?? string.Empty;
        OnPropertyChanged(nameof(CustomerGroupId));
        CustomerSearchText = string.Empty;
    }

    public void ClearCustomer()
    {
        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText)) return;

        InvalidateResult();
        SelectedCustomer = null;
        _customerGroupId = string.Empty;
        OnPropertyChanged(nameof(CustomerGroupId));
        CustomerSearchText = string.Empty;
    }

    public void AddSku(SalesQuotationSkuOption option)
    {
        if (!option.Selectable)
        {
            SetMessage(string.IsNullOrWhiteSpace(option.EligibilityMessage) ? "Mã hàng này chưa đủ điều kiện bán." : option.EligibilityMessage, true);
            return;
        }

        if (SelectedSkus.Any(item => item.Id == option.Id || string.Equals(item.Sku, option.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            SkuSearchText = string.Empty;
            SkuResults.Clear();
            return;
        }

        if (SelectedSkus.Count >= MaxQuotationSkus)
        {
            SetMessage("Mỗi báo giá tối đa 1.000 mã hàng.", true);
            return;
        }

        InvalidateResult();
        SelectedSkus.Add(option);
        SkuSearchText = string.Empty;
        SkuResults.Clear();
        OnPropertyChanged(nameof(ScopeDescription));
    }

    public void RemoveSku(string id)
    {
        var row = SelectedSkus.FirstOrDefault(item => item.Id == id);
        if (row is null) return;

        InvalidateResult();
        SelectedSkus.Remove(row);
        OnPropertyChanged(nameof(ScopeDescription));
    }

    public void UseSystemPrice(SalesQuotationRowView row) => row.UseSystemPrice();

    public async Task BuildAsync()
    {
        if (!CanBuild) return;
        IsBusy = true;
        SetMessage(string.Empty);
        try
        {
            var normalizedQuantity = ExactQuantity(Quantity);
            if (IsZero(normalizedQuantity))
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

            string[] skus;
            if (Scope == "sku")
            {
                if (SelectedSkus.Count == 0)
                    throw new InvalidOperationException("Chọn ít nhất một mã hàng cần báo giá.");

                skus = SelectedSkus.Select(item => item.Sku.Trim())
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            else
            {
                var products = await _service.ListQuotationProductsAsync().ConfigureAwait(true);
                var selectedProducts = products.Where(product => product.IsActive && product.IsOrderable);
                if (Scope == "category")
                {
                    if (string.IsNullOrWhiteSpace(CategoryId))
                        throw new InvalidOperationException("Chọn ngành hoặc nhóm sản phẩm cần báo giá.");
                    selectedProducts = selectedProducts.Where(product => product.CategoryId == CategoryId);
                }

                var productIds = selectedProducts.Select(product => product.Id)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                if (productIds.Length == 0)
                    throw new InvalidOperationException("Không có sản phẩm đang bán phù hợp với phạm vi đã chọn.");

                var variants = new List<DataExchangeProductVariantData>();
                for (var index = 0; index < productIds.Length; index += VariantBatchSize)
                {
                    var batch = productIds.Skip(index).Take(VariantBatchSize).ToArray();
                    variants.AddRange(await _service.QueryQuotationVariantsAsync(batch).ConfigureAwait(true));
                }

                skus = variants
                    .Where(variant => variant.IsActive && variant.IsSellable)
                    .Select(variant => variant.Sku.Trim())
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            if (skus.Length == 0)
                throw new InvalidOperationException("Không có mã hàng phù hợp để lập báo giá.");
            if (skus.Length > MaxQuotationSkus)
                throw new InvalidOperationException($"Phạm vi hiện có {skus.Length:N0} mã hàng. Mỗi báo giá tối đa 1.000 mã hàng; hãy chọn nhóm sản phẩm hoặc danh sách mã hàng cụ thể.");

            var request = new DataExchangeQuotationRequest(
                skus,
                normalizedQuantity,
                "VND",
                EmptyToNull(ChannelId),
                EmptyToNull(CustomerGroupId),
                SelectedCustomer?.Id,
                "tabular");

            _operationKey ??= _idempotencyKeys.Create("sales-quotation");
            var result = await _service.BuildQuotationAsync(request, _operationKey).ConfigureAwait(true);

            ClearRowsOnly();
            foreach (var data in result.Rows)
            {
                var row = new SalesQuotationRowView(
                    ReadJson(data, "sku"),
                    ReadJson(data, "productName"),
                    ReadJson(data, "skuName"),
                    ReadJson(data, "quantity", normalizedQuantity),
                    ReadJson(data, "unitPriceMinor"),
                    ReadJson(data, "lineTotalMinor"),
                    ReadJson(data, "priceListCode"),
                    ReadJson(data, "currencyCode", "VND"));
                row.Changed += QuotationRowChanged;
                Rows.Add(row);
            }

            _operationKey = null;
            RaiseResultState();
            SetMessage($"Đã tính báo giá cho {Rows.Count:N0} mã hàng.");
        }
        catch (Exception exception)
        {
            SetMessage(string.IsNullOrWhiteSpace(exception.Message) ? "Không tính được báo giá." : exception.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Export(string format, string filePath)
    {
        if (!CanExport) throw new InvalidOperationException("Hãy tính báo giá trước khi xuất file.");
        if (format is not ("xlsx" or "csv"))
            throw new ArgumentException("Định dạng tệp không hợp lệ.", nameof(format));

        var rows = Rows.Select(row => new[]
        {
            row.Sku,
            row.ProductName,
            row.SkuName,
            row.Quantity,
            row.CurrencyCode,
            row.EffectiveUnitPrice,
            row.EffectiveLineTotal,
            row.PriceSource
        }).ToArray();

        DataExchangeFileHelper.Write(filePath, "Báo giá", ExportHeaders, rows, format);
        SetMessage($"Đã xuất {rows.Length:N0} dòng báo giá ra {(format == "xlsx" ? "Excel" : "CSV")}.");
    }

    public string DefaultExportFileName(string format)
    {
        var customer = SelectedCustomer is null ? string.Empty : SafeFilePart(SelectedCustomer.Code);
        return $"bao-gia{(customer.Length == 0 ? string.Empty : "-" + customer)}.{format}";
    }

    private async Task SearchSkuAsync()
    {
        _skuSearchCancellation?.Cancel();
        _skuSearchCancellation?.Dispose();
        _skuSearchCancellation = null;
        SkuResults.Clear();

        var term = SkuSearchText.Trim();
        if (!IsSkuScope || term.Length < 1)
        {
            SkuSearching = false;
            OnPropertyChanged(nameof(SkuSearchStatus));
            return;
        }

        var cancellation = new CancellationTokenSource();
        _skuSearchCancellation = cancellation;
        SkuSearching = true;
        try
        {
            var results = await _service.SearchQuotationSkuAsync(term, cancellation.Token).ConfigureAwait(true);
            if (cancellation.IsCancellationRequested) return;
            foreach (var item in results.Take(SearchPageSize))
                SkuResults.Add(SalesQuotationSkuOption.From(item));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!cancellation.IsCancellationRequested)
                SetMessage(string.IsNullOrWhiteSpace(exception.Message) ? "Không tìm được hàng hóa." : exception.Message, true);
        }
        finally
        {
            if (ReferenceEquals(_skuSearchCancellation, cancellation))
            {
                SkuSearching = false;
                _skuSearchCancellation.Dispose();
                _skuSearchCancellation = null;
                OnPropertyChanged(nameof(SkuSearchStatus));
            }
        }
    }

    private void RebuildReferenceChoices()
    {
        Replace(Categories,
            new[] { new SalesQuotationChoice(string.Empty, "Chọn nhóm sản phẩm") }
                .Concat(_categories.Where(item => item.IsActive).OrderBy(item => item.Code)
                    .Select(item => new SalesQuotationChoice(item.Id, $"{item.Code} · {item.Name}"))));
        Replace(Channels,
            new[] { new SalesQuotationChoice(string.Empty, "Không chọn kênh") }
                .Concat(_channels.Where(item => item.IsActive).OrderBy(item => item.Code)
                    .Select(item => new SalesQuotationChoice(item.Id, $"{item.Code} · {item.Name}"))));
        Replace(CustomerGroups,
            new[] { new SalesQuotationChoice(string.Empty, "Không chọn nhóm") }
                .Concat(_groups.Where(item => item.IsActive).OrderBy(item => item.Code)
                    .Select(item => new SalesQuotationChoice(item.Id, $"{item.Code} · {item.Name}"))));

        RebuildCustomerResults();
        OnPropertyChanged(nameof(ScopeDescription));
    }

    private void RebuildCustomerResults()
    {
        CustomerResults.Clear();
        var term = SalesSkuLocalCatalog.NormalizeSearchText(CustomerSearchText);
        if (term.Length == 0)
        {
            OnPropertyChanged(nameof(CustomerSearchEmpty));
            return;
        }

        foreach (var customer in _customers
                     .Where(item => item.IsActive)
                     .Where(item => SalesSkuLocalCatalog.NormalizeSearchText($"{item.Code} {item.Name}")
                         .Contains(term, StringComparison.Ordinal))
                     .OrderBy(item => item.Code)
                     .Take(SearchPageSize))
            CustomerResults.Add(new SalesQuotationCustomerOption(customer.Id, customer.Code, customer.Name));

        OnPropertyChanged(nameof(CustomerSearchEmpty));
    }

    private void InvalidateResult()
    {
        _operationKey = null;
        ClearResults();
    }

    private void ClearResults()
    {
        ClearRowsOnly();
        SetMessage(string.Empty);
        RaiseResultState();
    }

    private void ClearRowsOnly()
    {
        foreach (var row in Rows)
            row.Changed -= QuotationRowChanged;
        Rows.Clear();
    }

    private void QuotationRowChanged(object? sender, EventArgs e) => RaiseResultState();

    private void RaiseResultState()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(NoRows));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(RowCountText));
        OnPropertyChanged(nameof(PricedCountText));
        OnPropertyChanged(nameof(TotalValueText));
    }

    private void RaiseScopeState()
    {
        OnPropertyChanged(nameof(IsAllScope));
        OnPropertyChanged(nameof(IsCategoryScope));
        OnPropertyChanged(nameof(IsSkuScope));
        OnPropertyChanged(nameof(IsSkuSearchActive));
        OnPropertyChanged(nameof(ScopeDescription));
        OnPropertyChanged(nameof(SkuSearchStatus));
    }

    private void RaiseActionState()
    {
        OnPropertyChanged(nameof(CanBuild));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(StatusText));
    }

    private void ClearReferenceData()
    {
        _categories = [];
        _channels = [];
        _groups = [];
        _customers = [];
        Categories.Clear();
        Channels.Clear();
        CustomerGroups.Clear();
        CustomerResults.Clear();
        SelectedCustomer = null;
        _customerGroupId = string.Empty;
        OnPropertyChanged(nameof(CustomerGroupId));
    }

    private void SetMessage(string value, bool isError = false)
    {
        MessageIsError = isError;
        Message = value;
    }

    private static string ExactQuantity(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                normalized,
                @"^(0|[1-9]\d{0,13})(?:\.\d{1,6})?$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            throw new InvalidOperationException("Số lượng phải là số không âm, tối đa 6 số lẻ.");
        return normalized;
    }

    private static bool IsZero(string quantity) =>
        decimal.TryParse(quantity, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) && value == 0m;

    private static string ReadJson(
        IReadOnlyDictionary<string, JsonElement> row,
        string key,
        string fallback = "") =>
        row.TryGetValue(key, out var value) ? DataExchangePresentation.JsonText(value) : fallback;

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SafeFilePart(string value)
    {
        var result = new string((value ?? string.Empty).Trim().ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-' ? character : '-')
            .ToArray()).Trim('-');
        while (result.Contains("--", StringComparison.Ordinal))
            result = result.Replace("--", "-", StringComparison.Ordinal);
        return result.Length <= 40 ? result : result[..40];
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    internal static string FormatMoney(string value, string currency)
    {
        if (!BigInteger.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        var formatted = parsed.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        return string.Equals(currency, "VND", StringComparison.OrdinalIgnoreCase)
            ? $"{formatted} ₫"
            : $"{formatted} {currency}";
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

public sealed record SalesQuotationChoice(string Id, string Display);

public sealed record SalesQuotationCustomerOption(string Id, string Code, string Name)
{
    public string Display => $"{Name} · {Code}";
}

public sealed record SalesQuotationSkuOption(
    string Id,
    string ProductName,
    string ProductCode,
    string Sku,
    string VariantName,
    string? Barcode,
    string? UnitCode,
    bool Selectable,
    string EligibilityMessage)
{
    public string Display =>
        $"{ProductName} · SKU {Sku}"
        + (string.IsNullOrWhiteSpace(VariantName) ? string.Empty : $" · {VariantName}");

    public string Detail =>
        ProductCode + (string.IsNullOrWhiteSpace(Barcode) ? string.Empty : $" · Barcode {Barcode}");

    public static SalesQuotationSkuOption From(SalesOrderSkuSearchOptionData item) =>
        new(
            item.Id,
            item.ProductName,
            item.ProductCode,
            item.Sku,
            item.VariantName,
            item.Barcode,
            item.UnitCode,
            item.Eligibility.Selectable,
            item.Eligibility.Message);
}

public sealed class SalesQuotationRowView : INotifyPropertyChanged
{
    private string _manualPrice = string.Empty;

    public SalesQuotationRowView(
        string sku,
        string productName,
        string skuName,
        string quantity,
        string systemPrice,
        string systemLineTotal,
        string priceListCode,
        string currencyCode)
    {
        Sku = sku;
        ProductName = productName;
        SkuName = skuName;
        Quantity = quantity;
        SystemPrice = systemPrice;
        SystemLineTotal = systemLineTotal;
        PriceListCode = priceListCode;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    public string Sku { get; }
    public string ProductName { get; }
    public string SkuName { get; }
    public string Quantity { get; }
    public string SystemPrice { get; }
    public string SystemLineTotal { get; }
    public string PriceListCode { get; }
    public string CurrencyCode { get; }

    public string ProductDisplay =>
        string.IsNullOrWhiteSpace(SkuName) ? ProductName : $"{ProductName} · {SkuName}";

    public bool HasManualPrice => _manualPrice.Length > 0;
    public string EffectiveUnitPrice => HasManualPrice ? _manualPrice : SystemPrice;
    public string EffectiveLineTotal =>
        HasManualPrice ? LineTotalForPrice(_manualPrice, Quantity) : SystemLineTotal;
    public string UnitPriceDisplay => FormatDigits(EffectiveUnitPrice);
    public string LineTotalDisplay =>
        SalesQuotationViewModel.FormatMoney(EffectiveLineTotal, CurrencyCode);
    public string PriceSource =>
        HasManualPrice
            ? "Giá chỉnh trên báo giá"
            : string.IsNullOrWhiteSpace(PriceListCode) ? "Giá áp dụng tự động" : PriceListCode;

    public string UnitPriceInput
    {
        get => UnitPriceDisplay;
        set
        {
            var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
            if (_manualPrice == digits) return;
            _manualPrice = digits;
            RaisePriceState();
        }
    }

    public void UseSystemPrice()
    {
        if (_manualPrice.Length == 0) return;
        _manualPrice = string.Empty;
        RaisePriceState();
    }

    private void RaisePriceState()
    {
        foreach (var name in new[]
        {
            nameof(HasManualPrice),
            nameof(EffectiveUnitPrice),
            nameof(EffectiveLineTotal),
            nameof(UnitPriceInput),
            nameof(UnitPriceDisplay),
            nameof(LineTotalDisplay),
            nameof(PriceSource)
        })
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static string LineTotalForPrice(string priceMinor, string quantity)
    {
        if (!BigInteger.TryParse(priceMinor, NumberStyles.None, CultureInfo.InvariantCulture, out var price))
            return string.Empty;

        var parts = quantity.Trim().Split('.', 2);
        if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole))
            return string.Empty;

        var fractionText = parts.Length > 1 ? parts[1] : string.Empty;
        if (fractionText.Length > 6 || fractionText.Any(character => !char.IsAsciiDigit(character)))
            return string.Empty;

        fractionText = fractionText.PadRight(6, '0');
        var fraction = fractionText.Length == 0
            ? BigInteger.Zero
            : BigInteger.Parse(fractionText, NumberStyles.None, CultureInfo.InvariantCulture);
        var scale = new BigInteger(1_000_000);
        var scaledQuantity = whole * scale + fraction;
        return ((price * scaledQuantity + scale / 2) / scale).ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatDigits(string value)
    {
        if (!BigInteger.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return value;
        return parsed.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    }
}
