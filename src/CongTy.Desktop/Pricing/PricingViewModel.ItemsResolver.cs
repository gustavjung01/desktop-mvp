using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel
{
    public void OpenItemCreate()
    {
        if (!CanCreatePriceItem) return;
        _editingItem = null;
        ItemProductId = string.Empty; ItemVariantId = string.Empty; ItemAdjustmentType = "FIXED_PRICE"; ItemAmount = string.Empty; ItemPercent = string.Empty;
        ItemMinQuantity = "0"; ItemMaxQuantity = string.Empty; ItemEffectiveFrom = string.Empty; ItemEffectiveTo = string.Empty; ItemExternalRuleCode = string.Empty; ItemNote = string.Empty; ItemActive = true;
        ItemVariantOptions.Clear();
        SetEditor(EditorKind.Item);
        OnPropertyChanged(nameof(ItemIdentityEnabled));
    }

    public async Task OpenItemEditAsync(PricingItemRow row)
    {
        if (!CanWrite) return;
        _editingItem = row.Data;
        ItemProductId = row.Data.ProductId;
        await LoadItemVariantsAsync(ItemProductId).ConfigureAwait(true);
        ItemVariantId = row.Data.VariantId; ItemAdjustmentType = row.Data.AdjustmentType; ItemAmount = row.Data.AmountMinor ?? string.Empty; ItemPercent = row.Data.RateBps is null ? string.Empty : PricingPresentation.BpsToPercent(row.Data.RateBps.Value);
        ItemMinQuantity = row.Data.MinQuantity; ItemMaxQuantity = row.Data.MaxQuantity ?? string.Empty; ItemEffectiveFrom = InputDate(row.Data.EffectiveFrom); ItemEffectiveTo = InputDate(row.Data.EffectiveTo);
        ItemExternalRuleCode = row.Data.ExternalRuleCode ?? string.Empty; ItemNote = row.Data.Note ?? string.Empty; ItemActive = row.Data.IsActive;
        SetEditor(EditorKind.Item);
        OnPropertyChanged(nameof(ItemIdentityEnabled));
    }

    public async Task LoadItemVariantsAsync(string? productId)
    {
        ItemVariantOptions.Clear();
        ItemVariantId = string.Empty;
        if (string.IsNullOrWhiteSpace(productId)) return;
        try
        {
            var rows = await _service.ListVariantsAsync(productId).ConfigureAwait(true);
            foreach (var row in rows.Where(value => value.IsActive && value.IsSellable).OrderBy(value => value.Sku)) ItemVariantOptions.Add(new PricingLookup(row.Id, $"{row.Sku} — {row.Name}", row));
        }
        catch (Exception exception) { SetMessage(exception.Message, true); }
    }

    public async Task SaveItemAsync()
    {
        if (!CanSaveEditor || _editorKind != EditorKind.Item || SelectedPriceList is null) return;
        string? from; string? to; int? rate = null; string? amount = null;
        try
        {
            from = ApiDate(ItemEffectiveFrom); to = ApiDate(ItemEffectiveTo);
            if (ItemUsesAmount)
            {
                if (string.IsNullOrWhiteSpace(ItemAmount) || !long.TryParse(ItemAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var money) || money < 0) throw new InvalidOperationException("Số tiền phải là số nguyên không âm.");
                amount = ItemAmount.Trim();
            }
            else rate = PricingPresentation.PercentToBps(ItemPercent);
        }
        catch (Exception exception) { SetMessage(exception.Message, true); return; }
        if (!decimal.TryParse(ItemMinQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var min) || min < 0) { SetMessage("Số lượng từ không hợp lệ.", true); return; }
        if (!string.IsNullOrWhiteSpace(ItemMaxQuantity) && (!decimal.TryParse(ItemMaxQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var max) || max <= min)) { SetMessage("Số lượng đến phải lớn hơn số lượng từ.", true); return; }
        if (from is not null && to is not null && DateTimeOffset.Parse(to) <= DateTimeOffset.Parse(from)) { SetMessage("Hiệu lực đến phải sau hiệu lực từ.", true); return; }

        await RunBusyAsync(async () =>
        {
            PriceListItemData saved;
            if (_editingItem is null)
            {
                var intent = $"pricing-item-create-{SelectedPriceList.Id}-{ItemVariantId}-{ItemAdjustmentType}-{ItemMinQuantity}-{ItemMaxQuantity}-{from}-{to}";
                saved = await _service.CreatePriceItemAsync(SelectedPriceList.Id, new PriceListItemCreateRequest(ItemVariantId, ItemAdjustmentType, amount, rate, string.IsNullOrWhiteSpace(ItemMinQuantity) ? "0" : ItemMinQuantity.Trim(), NullIfBlank(ItemMaxQuantity), from, to, NullIfBlank(ItemExternalRuleCode), NullIfBlank(ItemNote), "ADMIN", ItemActive), KeyFor(intent)).ConfigureAwait(true);
                CompleteIntent(intent);
                SetMessage("Đã thêm giá sản phẩm.", false);
            }
            else
            {
                saved = await _service.UpdatePriceItemAsync(SelectedPriceList.Id, _editingItem.Id, new PriceListItemUpdateRequest(amount, rate, string.IsNullOrWhiteSpace(ItemMinQuantity) ? "0" : ItemMinQuantity.Trim(), NullIfBlank(ItemMaxQuantity), from, to, NullIfBlank(ItemExternalRuleCode), NullIfBlank(ItemNote), ItemActive, _editingItem.UpdatedAt)).ConfigureAwait(true);
                SetMessage("Đã cập nhật giá sản phẩm.", false);
            }
            var current = _itemsByList.TryGetValue(SelectedPriceList.Id, out var oldRows) ? oldRows.ToList() : [];
            var index = current.FindIndex(row => row.Id == saved.Id);
            if (index >= 0) current[index] = saved; else current.Add(saved);
            _itemsByList[SelectedPriceList.Id] = current;
            RebuildSelectedItems();
            _overviewLoaded = false;
            CloseEditor();
        }).ConfigureAwait(true);
    }

    public async Task ToggleItemAsync(PricingItemRow row) => await RunBusyAsync(async () =>
    {
        var current = row.Data;
        var saved = await _service.UpdatePriceItemStatusAsync(current.PriceListId, current.Id, new PriceListItemStatusUpdateRequest(!current.IsActive, current.UpdatedAt)).ConfigureAwait(true);
        if (_itemsByList.TryGetValue(current.PriceListId, out var rows))
        {
            var next = rows.ToList(); var index = next.FindIndex(value => value.Id == saved.Id); if (index >= 0) next[index] = saved; _itemsByList[current.PriceListId] = next;
        }
        RebuildSelectedItems();
        _overviewLoaded = false;
    }).ConfigureAwait(true);

    public async Task LoadResolverVariantsAsync(string? productId)
    {
        ResolverVariantOptions.Clear(); ResolverVariantId = string.Empty; _resolution = null; ResolutionSteps.Clear(); RaiseResolution();
        if (string.IsNullOrWhiteSpace(productId)) return;
        try
        {
            var rows = await _service.ListVariantsAsync(productId).ConfigureAwait(true);
            foreach (var row in rows.Where(value => value.IsActive && value.IsSellable).OrderBy(value => value.Sku)) ResolverVariantOptions.Add(new PricingLookup(row.Id, $"{row.Sku} — {row.Name}", row));
        }
        catch (Exception exception) { SetMessage(exception.Message, true); }
    }

    public async Task ResolveAsync()
    {
        if (!CanResolve) return;
        if (!decimal.TryParse(ResolverQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0) { SetMessage("Số lượng phải lớn hơn 0.", true); return; }
        if (!string.IsNullOrWhiteSpace(ResolverManualPrice) && (!long.TryParse(ResolverManualPrice, NumberStyles.None, CultureInfo.InvariantCulture, out var manual) || manual < 0)) { SetMessage("Giá điều chỉnh thủ công không hợp lệ.", true); return; }
        if (!string.IsNullOrWhiteSpace(ResolverManualPrice) && string.IsNullOrWhiteSpace(ResolverManualReason)) { SetMessage("Cần nhập lý do khi điều chỉnh giá thủ công.", true); return; }
        await RunBusyAsync(async () =>
        {
            _resolution = await _service.ResolveAsync(new PricingResolveRequest(ResolverVariantId, ResolverQuantity.Trim(), NullIfBlank(ResolverChannelId), NullIfBlank(ResolverCustomerGroupId), NullIfBlank(ResolverCustomerId), NullIfBlank(ResolverManualPrice), NullIfBlank(ResolverManualReason))).ConfigureAwait(true);
            ResolutionSteps.Clear();
            foreach (var step in _resolution.Steps) ResolutionSteps.Add(new PricingResolutionStepRow(PricingPresentation.ResolutionDetail(step), PricingPresentation.ResolutionPrices(step)));
            RaiseResolution();
            SetMessage(string.Empty, false);
        }).ConfigureAwait(true);
    }
}
