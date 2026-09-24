using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CongTy.Contracts;
using CongTy.Desktop.Products;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel
{
    private readonly List<PricingAdjustmentSkuRow> _adjustmentAllRows = [];
    private bool _isAdjustmentOpen;
    private bool _isAdjustmentFileMode;
    private string _adjustmentPriceListId = string.Empty;
    private string _adjustmentType = "FIXED_PRICE";
    private string _adjustmentApplyMode = "NOW";
    private string _adjustmentApplyAt = string.Empty;
    private string _adjustmentSearch = string.Empty;
    private string _adjustmentSharedValue = string.Empty;
    private string _adjustmentMinQuantity = "0";
    private string _adjustmentMaxQuantity = string.Empty;
    private string _adjustmentNote = string.Empty;
    private string _adjustmentFileName = string.Empty;

    public ObservableCollection<PricingAdjustmentSkuRow> AdjustmentRows { get; } = [];
    public IReadOnlyList<PricingOption> AdjustmentApplyModes { get; } =
    [
        new("NOW", "Cập nhật ngay"),
        new("SCHEDULED", "Áp dụng từ ngày")
    ];

    public IReadOnlyList<PricingLookup> AdjustmentPriceListOptions =>
        _lists.Where(row => row.IsActive)
            .OrderByDescending(row => row.ListType == "BASE")
            .ThenByDescending(row => row.Priority)
            .ThenBy(row => row.Code)
            .Select(row => new PricingLookup(row.Id, row.Code + " — " + row.Name, row))
            .ToArray();

    public bool IsAdjustmentOpen
    {
        get => _isAdjustmentOpen;
        private set => SetField(ref _isAdjustmentOpen, value);
    }

    public bool IsAdjustmentFileMode
    {
        get => _isAdjustmentFileMode;
        private set
        {
            if (!SetField(ref _isAdjustmentFileMode, value)) return;
            OnPropertyChanged(nameof(IsAdjustmentDirectMode));
            OnPropertyChanged(nameof(AdjustmentTitle));
            OnPropertyChanged(nameof(AdjustmentDescription));
            OnPropertyChanged(nameof(AdjustmentUsesPerSkuPrice));
            OnPropertyChanged(nameof(AdjustmentUsesSharedValue));
            OnPropertyChanged(nameof(AdjustmentConfirmText));
            OnPropertyChanged(nameof(AdjustmentSelectionSummary));
        }
    }

    public bool IsAdjustmentDirectMode => !IsAdjustmentFileMode;
    public string AdjustmentTitle => IsAdjustmentFileMode ? "Điều chỉnh giá từ file" : "Điều chỉnh giá trực tiếp";
    public string AdjustmentDescription => IsAdjustmentFileMode
        ? "File chỉ cần SKU và Giá bán (VND). Chỉ các SKU có trong file mới thay đổi."
        : "Chỉ SKU được chọn mới thay đổi. Giá cũ được giữ trong lịch sử và tự kết thúc tại thời điểm áp dụng giá mới.";

    public string AdjustmentPriceListId
    {
        get => _adjustmentPriceListId;
        set
        {
            if (!SetField(ref _adjustmentPriceListId, value)) return;
            RefreshAdjustmentRowsCurrentValues();
        }
    }

    public string AdjustmentType
    {
        get => _adjustmentType;
        set
        {
            if (!SetField(ref _adjustmentType, value)) return;
            OnPropertyChanged(nameof(AdjustmentUsesPerSkuPrice));
            OnPropertyChanged(nameof(AdjustmentUsesSharedValue));
            OnPropertyChanged(nameof(AdjustmentSharedValueLabel));
            RefreshAdjustmentRowsCurrentValues();
        }
    }

    public string AdjustmentApplyMode
    {
        get => _adjustmentApplyMode;
        set
        {
            if (!SetField(ref _adjustmentApplyMode, value)) return;
            OnPropertyChanged(nameof(AdjustmentScheduled));
        }
    }

    public bool AdjustmentScheduled => AdjustmentApplyMode == "SCHEDULED";
    public string AdjustmentApplyAt { get => _adjustmentApplyAt; set => SetField(ref _adjustmentApplyAt, value); }

    public string AdjustmentSearch
    {
        get => _adjustmentSearch;
        set
        {
            if (!SetField(ref _adjustmentSearch, value)) return;
            RebuildAdjustmentRows();
        }
    }

    public string AdjustmentSharedValue { get => _adjustmentSharedValue; set => SetField(ref _adjustmentSharedValue, value); }

    public string AdjustmentMinQuantity
    {
        get => _adjustmentMinQuantity;
        set
        {
            if (!SetField(ref _adjustmentMinQuantity, value)) return;
            RefreshAdjustmentRowsCurrentValues();
        }
    }

    public string AdjustmentMaxQuantity
    {
        get => _adjustmentMaxQuantity;
        set
        {
            if (!SetField(ref _adjustmentMaxQuantity, value)) return;
            RefreshAdjustmentRowsCurrentValues();
        }
    }

    public string AdjustmentNote { get => _adjustmentNote; set => SetField(ref _adjustmentNote, value); }
    public string AdjustmentFileName { get => _adjustmentFileName; private set => SetField(ref _adjustmentFileName, value); }
    public bool AdjustmentUsesPerSkuPrice => IsAdjustmentFileMode || AdjustmentType == "FIXED_PRICE";
    public bool AdjustmentUsesSharedValue => IsAdjustmentDirectMode && AdjustmentType != "FIXED_PRICE";
    public string AdjustmentSharedValueLabel => AdjustmentType.StartsWith("PERCENT_", StringComparison.Ordinal) ? "Phần trăm (%)" : "Số tiền (₫)";
    public string AdjustmentSelectionSummary => IsAdjustmentFileMode
        ? _adjustmentAllRows.Count + " SKU từ file"
        : _adjustmentAllRows.Count(row => row.Selected) + " SKU đã chọn";
    public string AdjustmentConfirmText => IsAdjustmentFileMode
        ? "Xác nhận " + _adjustmentAllRows.Count + " SKU"
        : "Xác nhận " + _adjustmentAllRows.Count(row => row.Selected) + " SKU";

    public async Task OpenDirectAdjustmentAsync()
    {
        if (!CanWrite)
        {
            SetMessage("Tài khoản chưa được cấp quyền chỉnh sửa giá bán.", true);
            return;
        }

        await EnsureOverviewAsync().ConfigureAwait(true);
        if (!_overviewLoaded) return;

        ClearAdjustmentRows();
        ResetAdjustmentFields(fileMode: false);
        foreach (var pair in _overviewVariants.OrderBy(row => row.Product.Code).ThenBy(row => row.Variant.Sku))
        {
            var row = new PricingAdjustmentSkuRow(pair.Variant.Sku, pair.Product.Name, pair.Variant.Name);
            row.PropertyChanged += AdjustmentRowOnPropertyChanged;
            _adjustmentAllRows.Add(row);
        }

        AdjustmentPriceListId = PreferredAdjustmentPriceListId();
        RefreshAdjustmentRowsCurrentValues();
        RebuildAdjustmentRows();
        OnPropertyChanged(nameof(AdjustmentPriceListOptions));
        RaiseAdjustmentSummary();
        IsAdjustmentOpen = true;
    }

    public async Task OpenFileAdjustmentAsync(string filePath)
    {
        if (!CanWrite)
        {
            SetMessage("Tài khoản chưa được cấp quyền chỉnh sửa giá bán.", true);
            return;
        }

        await EnsureOverviewAsync().ConfigureAwait(true);
        if (!_overviewLoaded) return;

        try
        {
            var matrix = await SpreadsheetMatrixReader.ReadAsync(filePath).ConfigureAwait(true);
            if (matrix.Count == 0) throw new InvalidOperationException("File điều chỉnh giá không có dữ liệu.");
            var skuIndex = HeaderIndex(matrix[0], "sku");
            var amountIndex = HeaderIndex(matrix[0], "amountMinor");
            if (skuIndex < 0 || amountIndex < 0) throw new InvalidOperationException("File phải có đúng hai cột sku và amountMinor.");

            var sourceRows = matrix.Skip(1).ToArray();
            if (sourceRows.Length == 0) throw new InvalidOperationException("File điều chỉnh giá chưa có SKU.");
            if (sourceRows.Length > 2000) throw new InvalidOperationException("Mỗi lần điều chỉnh tối đa 2.000 SKU.");

            var metadata = _overviewVariants
                .GroupBy(row => row.Variant.Sku, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ClearAdjustmentRows();
            ResetAdjustmentFields(fileMode: true);
            for (var index = 0; index < sourceRows.Length; index++)
            {
                var source = sourceRows[index];
                var sku = Cell(source, skuIndex).Trim().ToUpperInvariant();
                var amount = Cell(source, amountIndex).Trim();
                if (sku.Length == 0) throw new InvalidOperationException("Dòng " + (index + 2) + ": SKU đang trống.");
                if (!seen.Add(sku)) throw new InvalidOperationException("Dòng " + (index + 2) + ": SKU " + sku + " bị lặp trong file.");
                if (!ValidMoney(amount)) throw new InvalidOperationException("Dòng " + (index + 2) + " · SKU " + sku + ": Giá bán phải là số nguyên không âm.");

                metadata.TryGetValue(sku, out var meta);
                var row = new PricingAdjustmentSkuRow(
                    sku,
                    meta.Product?.Name ?? "Chưa tra cứu",
                    meta.Variant?.Name ?? string.Empty)
                {
                    Selected = true,
                    NewAmount = amount
                };
                row.PropertyChanged += AdjustmentRowOnPropertyChanged;
                _adjustmentAllRows.Add(row);
            }

            AdjustmentFileName = Path.GetFileName(filePath);
            AdjustmentPriceListId = PreferredAdjustmentPriceListId();
            RefreshAdjustmentRowsCurrentValues();
            RebuildAdjustmentRows();
            OnPropertyChanged(nameof(AdjustmentPriceListOptions));
            RaiseAdjustmentSummary();
            IsAdjustmentOpen = true;
        }
        catch (Exception exception)
        {
            ClearAdjustmentRows();
            SetMessage(exception.Message, true);
        }
    }

    public void ExportAdjustmentTemplate(string filePath)
    {
        try
        {
            PricingWorkbookWriter.Write(filePath,
            [
                new PricingWorkbookSheet("Điều chỉnh giá", ["sku", "amountMinor"], Array.Empty<string[]>())
            ]);
            SetMessage("Đã tạo file mẫu điều chỉnh giá.", false);
        }
        catch (Exception exception)
        {
            SetMessage(exception.Message, true);
        }
    }

    public void SelectVisibleAdjustmentRows()
    {
        if (!IsAdjustmentDirectMode) return;
        foreach (var row in AdjustmentRows) row.Selected = true;
        RaiseAdjustmentSummary();
    }

    public void ClearAdjustmentSelection()
    {
        if (!IsAdjustmentDirectMode) return;
        foreach (var row in _adjustmentAllRows) row.Selected = false;
        RaiseAdjustmentSummary();
    }

    public void CloseAdjustment() => IsAdjustmentOpen = false;

    public async Task ApplyAdjustmentAsync()
    {
        if (!CanWrite || IsBusy) return;

        var list = _lists.FirstOrDefault(row => row.Id == AdjustmentPriceListId && row.IsActive);
        if (list is null)
        {
            SetMessage("Chọn bảng giá cần điều chỉnh.", true);
            return;
        }

        var targets = IsAdjustmentFileMode
            ? _adjustmentAllRows.ToArray()
            : _adjustmentAllRows.Where(row => row.Selected).ToArray();
        if (targets.Length == 0)
        {
            SetMessage("Chọn ít nhất một SKU.", true);
            return;
        }
        if (targets.Length > 2000)
        {
            SetMessage("Mỗi lần điều chỉnh tối đa 2.000 SKU.", true);
            return;
        }

        var adjustmentType = IsAdjustmentFileMode ? "FIXED_PRICE" : AdjustmentType;
        if (list.ListType == "BASE" && adjustmentType != "FIXED_PRICE")
        {
            SetMessage("Bảng giá nền chỉ nhận giá trực tiếp.", true);
            return;
        }

        string? applyAt = null;
        if (AdjustmentScheduled)
        {
            if (string.IsNullOrWhiteSpace(AdjustmentApplyAt))
            {
                SetMessage("Chọn ngày áp dụng giá mới.", true);
                return;
            }
            try { applyAt = ApiDate(AdjustmentApplyAt); }
            catch
            {
                SetMessage("Ngày áp dụng giá không hợp lệ.", true);
                return;
            }
        }

        var minQuantity = IsAdjustmentFileMode ? "0" : AdjustmentMinQuantity.Trim();
        var maxQuantity = IsAdjustmentFileMode ? null : NullIfBlank(AdjustmentMaxQuantity);
        if (!ValidQuantity(minQuantity, out var min) || min < 0)
        {
            SetMessage("Số lượng từ không hợp lệ.", true);
            return;
        }
        if (maxQuantity is not null && (!ValidQuantity(maxQuantity, out var max) || max <= min))
        {
            SetMessage("Số lượng đến phải lớn hơn số lượng từ.", true);
            return;
        }

        string? sharedAmount = null;
        int? rateBps = null;
        if (adjustmentType != "FIXED_PRICE")
        {
            if (adjustmentType.StartsWith("PERCENT_", StringComparison.Ordinal))
            {
                try { rateBps = PricingPresentation.PercentToBps(AdjustmentSharedValue.Trim()); }
                catch (Exception exception)
                {
                    SetMessage(exception.Message, true);
                    return;
                }
            }
            else
            {
                sharedAmount = AdjustmentSharedValue.Trim();
                if (!ValidMoney(sharedAmount))
                {
                    SetMessage("Giá trị tiền phải là số nguyên không âm.", true);
                    return;
                }
            }
        }

        var items = new List<PricingAdjustmentItemRequest>(targets.Length);
        foreach (var row in targets)
        {
            var amount = adjustmentType == "FIXED_PRICE" ? row.NewAmount.Trim() : sharedAmount;
            if (adjustmentType == "FIXED_PRICE" && !ValidMoney(amount))
            {
                SetMessage("Nhập giá hợp lệ cho SKU " + row.Sku + ".", true);
                return;
            }

            items.Add(new PricingAdjustmentItemRequest(
                PriceListCode: list.Code,
                Sku: row.Sku,
                AdjustmentType: adjustmentType,
                AmountMinor: amount,
                RateBps: rateBps,
                MinQuantity: minQuantity,
                MaxQuantity: maxQuantity,
                SourceKind: IsAdjustmentFileMode ? "IMPORT" : "ADMIN",
                ExternalRuleCode: IsAdjustmentFileMode ? "PRICE_FILE_ADJUSTMENT" : "PRICE_ADJUSTMENT",
                Note: IsAdjustmentFileMode ? null : NullIfBlank(AdjustmentNote),
                IsActive: true));
        }

        var fingerprint = AdjustmentFingerprint(list.Code, adjustmentType, applyAt, minQuantity, maxQuantity, items);
        var intent = "pricing-adjust-" + fingerprint;

        await RunBusyAsync(async () =>
        {
            var operationKey = KeyFor(intent);
            var result = await _service.AdjustPricingAsync(
                new PricingAdjustmentRequest(
                    MatchBySku: true,
                    ReplaceFrom: true,
                    ApplyAt: applyAt,
                    SourceBatchId: operationKey,
                    Items: items),
                operationKey).ConfigureAwait(true);

            CompleteIntent(intent);
            await RefreshAdjustedPriceListAsync(list.Id).ConfigureAwait(true);
            SetMessage(
                "Đã điều chỉnh " + result.TotalItems + " SKU"
                + (AdjustmentScheduled ? " theo ngày đã chọn." : " và áp dụng ngay."),
                false);
            CloseAdjustment();
        }).ConfigureAwait(true);
    }

    private async Task RefreshAdjustedPriceListAsync(string priceListId)
    {
        var rows = await _service.ListPriceItemsAsync(priceListId).ConfigureAwait(true);
        _itemsByList[priceListId] = rows;
        if (SelectedPriceListId == priceListId) RebuildSelectedItems();
        RebuildOverviewRows();
        RaiseOverviewSummary();
        OverviewColumnsChanged?.Invoke(this, EventArgs.Empty);
        RefreshAdjustmentRowsCurrentValues();
    }

    private void ResetAdjustmentFields(bool fileMode)
    {
        IsAdjustmentFileMode = fileMode;
        _adjustmentType = "FIXED_PRICE";
        _adjustmentApplyMode = "NOW";
        _adjustmentApplyAt = string.Empty;
        _adjustmentSearch = string.Empty;
        _adjustmentSharedValue = string.Empty;
        _adjustmentMinQuantity = "0";
        _adjustmentMaxQuantity = string.Empty;
        _adjustmentNote = string.Empty;
        AdjustmentFileName = string.Empty;
        OnPropertyChanged(nameof(AdjustmentType));
        OnPropertyChanged(nameof(AdjustmentApplyMode));
        OnPropertyChanged(nameof(AdjustmentScheduled));
        OnPropertyChanged(nameof(AdjustmentApplyAt));
        OnPropertyChanged(nameof(AdjustmentSearch));
        OnPropertyChanged(nameof(AdjustmentSharedValue));
        OnPropertyChanged(nameof(AdjustmentMinQuantity));
        OnPropertyChanged(nameof(AdjustmentMaxQuantity));
        OnPropertyChanged(nameof(AdjustmentNote));
        OnPropertyChanged(nameof(AdjustmentUsesPerSkuPrice));
        OnPropertyChanged(nameof(AdjustmentUsesSharedValue));
        OnPropertyChanged(nameof(AdjustmentSharedValueLabel));
    }

    private string PreferredAdjustmentPriceListId()
    {
        if (OverviewMode != BaseOnly && OverviewMode != AllLists)
        {
            var selected = _lists.FirstOrDefault(row => row.Id == OverviewMode && row.IsActive);
            if (selected is not null) return selected.Id;
        }

        return _lists.FirstOrDefault(row => row.IsActive && row.ListType == "BASE")?.Id
            ?? _lists.FirstOrDefault(row => row.IsActive)?.Id
            ?? string.Empty;
    }

    private void RebuildAdjustmentRows()
    {
        var search = AdjustmentSearch.Trim();
        AdjustmentRows.Clear();
        foreach (var row in _adjustmentAllRows)
        {
            if (search.Length > 0
                && !row.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !row.ProductName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                && !row.VariantName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }
            AdjustmentRows.Add(row);
        }
        RaiseAdjustmentSummary();
    }

    private void RefreshAdjustmentRowsCurrentValues()
    {
        if (_adjustmentAllRows.Count == 0) return;
        _itemsByList.TryGetValue(AdjustmentPriceListId, out var items);
        items ??= [];
        var now = DateTimeOffset.UtcNow;
        foreach (var row in _adjustmentAllRows)
        {
            var current = items
                .Where(item => item.IsActive
                    && item.Sku.Equals(row.Sku, StringComparison.OrdinalIgnoreCase)
                    && item.AdjustmentType == AdjustmentType
                    && DecimalKey(item.MinQuantity) == DecimalKey(AdjustmentMinQuantity)
                    && DecimalKey(item.MaxQuantity) == DecimalKey(NullIfBlank(AdjustmentMaxQuantity))
                    && IntervalContains(item, now))
                .OrderByDescending(item => EffectiveStart(item.EffectiveFrom))
                .FirstOrDefault();
            row.UpdateCurrent(
                current is null ? "Chưa có mức hiện tại" : PricingPresentation.AdjustmentValue(current),
                current is null ? "Tạo mới" : "Điều chỉnh");
        }
    }

    private void AdjustmentRowOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PricingAdjustmentSkuRow.Selected) or nameof(PricingAdjustmentSkuRow.NewAmount))
            RaiseAdjustmentSummary();
    }

    private void RaiseAdjustmentSummary()
    {
        OnPropertyChanged(nameof(AdjustmentSelectionSummary));
        OnPropertyChanged(nameof(AdjustmentConfirmText));
    }

    private void ClearAdjustmentRows()
    {
        foreach (var row in _adjustmentAllRows) row.PropertyChanged -= AdjustmentRowOnPropertyChanged;
        _adjustmentAllRows.Clear();
        AdjustmentRows.Clear();
        RaiseAdjustmentSummary();
    }

    private static int HeaderIndex(IReadOnlyList<string> headers, string expected)
    {
        for (var index = 0; index < headers.Count; index++)
            if (headers[index].Trim().Equals(expected, StringComparison.OrdinalIgnoreCase)) return index;
        return -1;
    }

    private static string Cell(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;

    private static bool ValidMoney(string? value)
    {
        var input = (value ?? string.Empty).Trim();
        if (input.Length is < 1 or > 19) return false;
        if (input.Length > 1 && input[0] == '0') return false;
        return input.All(char.IsDigit);
    }

    private static bool ValidQuantity(string value, out decimal parsed) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed);

    private static string DecimalKey(string? value)
    {
        var input = (value ?? string.Empty).Trim();
        if (input.Length == 0) return string.Empty;
        if (!decimal.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)) return input;
        return parsed.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private static bool IntervalContains(PriceListItemData item, DateTimeOffset at)
    {
        if (!string.IsNullOrWhiteSpace(item.EffectiveFrom)
            && DateTimeOffset.TryParse(item.EffectiveFrom, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var from)
            && at < from.ToUniversalTime()) return false;
        if (!string.IsNullOrWhiteSpace(item.EffectiveTo)
            && DateTimeOffset.TryParse(item.EffectiveTo, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var to)
            && at >= to.ToUniversalTime()) return false;
        return true;
    }

    private static DateTimeOffset EffectiveStart(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : DateTimeOffset.MinValue;

    private static string AdjustmentFingerprint(
        string priceListCode,
        string adjustmentType,
        string? applyAt,
        string minQuantity,
        string? maxQuantity,
        IReadOnlyList<PricingAdjustmentItemRequest> items)
    {
        var builder = new StringBuilder()
            .Append(priceListCode).Append('|')
            .Append(adjustmentType).Append('|')
            .Append(applyAt).Append('|')
            .Append(minQuantity).Append('|')
            .Append(maxQuantity).Append('|');

        foreach (var item in items.OrderBy(row => row.Sku, StringComparer.Ordinal))
        {
            builder.Append(item.Sku).Append(':')
                .Append(item.AmountMinor).Append(':')
                .Append(item.RateBps).Append(':')
                .Append(item.SourceKind).Append(':')
                .Append(item.Note).Append('|');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }
}

public sealed class PricingAdjustmentSkuRow : INotifyPropertyChanged
{
    private bool _selected;
    private string _newAmount = string.Empty;
    private string _currentValue = "—";
    private string _previewStatus = "Tạo mới";

    public PricingAdjustmentSkuRow(string sku, string productName, string variantName)
    {
        Sku = sku;
        ProductName = productName;
        VariantName = variantName;
    }

    public string Sku { get; }
    public string ProductName { get; }
    public string VariantName { get; }

    public bool Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    }

    public string NewAmount
    {
        get => _newAmount;
        set => SetField(ref _newAmount, value);
    }

    public string CurrentValue
    {
        get => _currentValue;
        private set => SetField(ref _currentValue, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        private set => SetField(ref _previewStatus, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateCurrent(string currentValue, string previewStatus)
    {
        CurrentValue = currentValue;
        PreviewStatus = previewStatus;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
