using System.Globalization;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel
{
    public async Task EnsureOverviewAsync()
    {
        if (_overviewLoaded || !CanRead) { RebuildOverviewRows(); return; }
        IsBusy = true;
        SetMessage("Đang tải bảng giá tổng hợp...", false);
        try
        {
            _overviewVariants.Clear();
            var gate = new SemaphoreSlim(6);
            var productTasks = _products.Where(row => row.IsActive).Select(async product =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var variants = await _service.ListVariantsAsync(product.Id).ConfigureAwait(false);
                    return (Product: product, Variants: variants.Where(row => row.IsActive && row.IsSellable).ToArray());
                }
                finally { gate.Release(); }
            }).ToArray();
            var productResults = await Task.WhenAll(productTasks).ConfigureAwait(true);
            foreach (var result in productResults.OrderBy(value => value.Product.Code))
                foreach (var variant in result.Variants.OrderBy(value => value.Sku)) _overviewVariants.Add((result.Product, variant));

            var listGate = new SemaphoreSlim(4);
            var itemTasks = _lists.Select(async list =>
            {
                await listGate.WaitAsync().ConfigureAwait(false);
                try { return (List: list, Items: await _service.ListPriceItemsAsync(list.Id).ConfigureAwait(false)); }
                finally { listGate.Release(); }
            }).ToArray();
            var itemResults = await Task.WhenAll(itemTasks).ConfigureAwait(true);
            _itemsByList.Clear();
            foreach (var result in itemResults) _itemsByList[result.List.Id] = result.Items;
            _overviewLoaded = true;
            RebuildOverviewModes();
            RebuildOverviewRows();
            RaiseOverviewSummary();
            OverviewColumnsChanged?.Invoke(this, EventArgs.Empty);
            SetMessage(string.Empty, false);
        }
        catch (Exception exception) { SetMessage(exception.Message, true); }
        finally { IsBusy = false; }
    }

    public string ExportDefaultFileName(bool allLists)
    {
        if (allLists) return "toan-bo-bang-gia.xlsx";
        var selected = VisibleOverviewPriceLists.SingleOrDefault();
        return selected is null ? "bang-gia.xlsx" : $"bang-gia-{selected.Code}.xlsx";
    }

    public async Task ExportWorkbookAsync(string filePath, bool allLists)
    {
        var selected = allLists ? null : VisibleOverviewPriceLists.SingleOrDefault();
        if (!allLists && selected is null)
        {
            SetMessage("Chọn một bảng giá cụ thể để xuất.", true);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var intent = allLists ? "pricing-overview-export-all" : $"pricing-overview-export-{selected!.Code.ToUpperInvariant()}";
            var snapshot = await _service.ExportOfficialPricingAsync(KeyFor(intent)).ConfigureAwait(true);
            var rules = ParseOfficialRules(snapshot.Rows);
            var selectedRules = allLists ? rules : rules.Where(row => row.List?.Code.Equals(selected!.Code, StringComparison.OrdinalIgnoreCase) == true).ToArray();
            var sheets = new List<PricingWorkbookSheet>
            {
                BuildSummarySheet(rules, selected),
                BuildDetailSheet(selectedRules)
            };
            PricingWorkbookWriter.Write(filePath, sheets);
            CompleteIntent(intent);
            SetMessage(allLists
                ? $"Đã xuất {_overviewVariants.Count} SKU và {rules.Count} dòng điều kiện áp dụng."
                : $"Đã xuất {_overviewVariants.Count} SKU và {selectedRules.Count} dòng điều kiện của {selected!.Code} · {selected.Name}.", false);
        }).ConfigureAwait(true);
    }

    private PricingWorkbookSheet BuildSummarySheet(IReadOnlyList<OfficialPricingRule> rules, PriceListData? selected)
    {
        var lists = selected is null
            ? _lists.Where(row => row.ListType != "BASE").OrderByDescending(row => row.Priority).ThenBy(row => row.Code).ToArray()
            : [selected];
        var headers = new List<string> { "Mã SP", "Tên SP", "SKU", "Quy cách", "ĐVT", "Giá nền" };
        headers.AddRange(lists.Select(row => $"{row.Code} · {row.Name}{(row.IsActive ? string.Empty : " (Ngừng)")}"));
        var unitById = _units.ToDictionary(row => row.Id, row => string.IsNullOrWhiteSpace(row.Symbol) ? row.Name : row.Symbol!, StringComparer.Ordinal);
        var rows = new List<string[]>();
        foreach (var pair in _overviewVariants)
        {
            var baseItems = rules.Where(row => row.List?.ListType == "BASE" && row.Item.Sku.Equals(pair.Variant.Sku, StringComparison.OrdinalIgnoreCase)).Select(row => row.Item).ToArray();
            var values = new List<string>
            {
                pair.Product.Code,
                pair.Product.Name,
                pair.Variant.Sku,
                pair.Variant.Name,
                pair.Variant.UnitId is not null && unitById.TryGetValue(pair.Variant.UnitId, out var unit) ? unit : string.Empty,
                PricingPresentation.SummaryRule(baseItems)
            };
            foreach (var list in lists)
            {
                var listItems = rules.Where(row => row.List?.Code.Equals(list.Code, StringComparison.OrdinalIgnoreCase) == true && row.Item.Sku.Equals(pair.Variant.Sku, StringComparison.OrdinalIgnoreCase)).Select(row => row.Item).ToArray();
                values.Add(PricingPresentation.SummaryRule(listItems));
            }
            rows.Add(values.ToArray());
        }
        return new PricingWorkbookSheet(selected is null ? "Bảng giá tổng hợp" : $"Tổng hợp {selected.Code}", headers, rows);
    }

    private PricingWorkbookSheet BuildDetailSheet(IReadOnlyList<OfficialPricingRule> rules)
    {
        var headers = new[]
        {
            "Mã bảng giá", "Tên bảng giá", "Loại", "Kênh bán", "Nhóm khách", "Khách hàng", "Ưu tiên", "Cách kết hợp", "Không xét tiếp",
            "Mã SP", "Tên SP", "SKU", "Quy cách", "ĐVT", "Cách áp dụng", "Giá trị", "SL từ", "SL đến", "Hiệu lực từ", "Hiệu lực đến",
            "Mã tham chiếu", "Ghi chú", "Trạng thái"
        };
        var meta = _overviewVariants.ToDictionary(row => row.Variant.Sku, row => row, StringComparer.OrdinalIgnoreCase);
        var unitById = _units.ToDictionary(row => row.Id, row => string.IsNullOrWhiteSpace(row.Symbol) ? row.Name : row.Symbol!, StringComparer.Ordinal);
        var rows = rules
            .OrderBy(row => row.List?.Code)
            .ThenBy(row => row.Item.Sku)
            .ThenBy(row => DecimalSort(row.Item.MinQuantity))
            .Select(rule =>
            {
                meta.TryGetValue(rule.Item.Sku, out var sku);
                var unit = sku.Variant?.UnitId is not null && unitById.TryGetValue(sku.Variant.UnitId, out var unitName) ? unitName : string.Empty;
                return new[]
                {
                    rule.List?.Code ?? rule.PriceListCode,
                    rule.List?.Name ?? rule.PriceListName,
                    PricingPresentation.ListTypeLabel(rule.List?.ListType ?? rule.ListType),
                    rule.List?.ChannelName ?? string.Empty,
                    rule.List?.CustomerGroupName ?? string.Empty,
                    rule.List?.CustomerName ?? string.Empty,
                    rule.List?.Priority.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    rule.List?.StackingMode == "STACKABLE" ? "Có thể kết hợp" : "Chỉ áp dụng một mức",
                    rule.List?.StopProcessing == true ? "Có" : "Không",
                    sku.Product?.Code ?? string.Empty,
                    sku.Product?.Name ?? string.Empty,
                    rule.Item.Sku,
                    sku.Variant?.Name ?? string.Empty,
                    unit,
                    PricingPresentation.AdjustmentLabel(rule.Item.AdjustmentType),
                    PricingPresentation.AdjustmentValue(rule.Item),
                    rule.Item.MinQuantity,
                    rule.Item.MaxQuantity ?? string.Empty,
                    InputDate(rule.Item.EffectiveFrom),
                    InputDate(rule.Item.EffectiveTo),
                    rule.Item.ExternalRuleCode ?? string.Empty,
                    rule.Item.Note ?? string.Empty,
                    PricingPresentation.Status(rule.Item.IsActive)
                };
            }).ToArray();
        return new PricingWorkbookSheet("Điều kiện áp dụng", headers, rows);
    }

    private IReadOnlyList<OfficialPricingRule> ParseOfficialRules(IEnumerable<Dictionary<string, JsonElement>> rows)
    {
        var listByCode = _lists.ToDictionary(row => row.Code, StringComparer.OrdinalIgnoreCase);
        return rows.Select(row =>
        {
            var listCode = Text(row, "priceListCode");
            listByCode.TryGetValue(listCode, out var list);
            var rateText = Text(row, "rateBps");
            var item = new PriceListItemData
            {
                Sku = Text(row, "sku"),
                AdjustmentType = Text(row, "adjustmentType"),
                AmountMinor = NullText(row, "amountMinor"),
                RateBps = int.TryParse(rateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rate) ? rate : null,
                MinQuantity = string.IsNullOrWhiteSpace(Text(row, "minQuantity")) ? "0" : Text(row, "minQuantity"),
                MaxQuantity = NullText(row, "maxQuantity"),
                EffectiveFrom = NullText(row, "effectiveFrom"),
                EffectiveTo = NullText(row, "effectiveTo"),
                ExternalRuleCode = NullText(row, "externalRuleCode"),
                Note = NullText(row, "note"),
                SourceKind = Text(row, "sourceKind"),
                SourceKey = NullText(row, "sourceKey"),
                IsActive = Bool(row, "isActive")
            };
            return new OfficialPricingRule(listCode, Text(row, "priceListName"), Text(row, "listType"), list, item);
        }).ToArray();
    }

    private static string Text(IReadOnlyDictionary<string, JsonElement> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private static string? NullText(IReadOnlyDictionary<string, JsonElement> row, string key)
    {
        var value = Text(row, key).Trim();
        return value.Length == 0 ? null : value;
    }

    private static bool Bool(IReadOnlyDictionary<string, JsonElement> row, string key) =>
        row.TryGetValue(key, out var value) && (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed));

    private sealed record OfficialPricingRule(string PriceListCode, string PriceListName, string ListType, PriceListData? List, PriceListItemData Item);
}
