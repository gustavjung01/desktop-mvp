using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel
{
    public void CloseEditor()
    {
        _editingChannel = null; _editingList = null; _editingItem = null; SetEditor(EditorKind.None);
    }

    public async Task SaveEditorAsync()
    {
        switch (_editorKind)
        {
            case EditorKind.Channel: await SaveChannelAsync().ConfigureAwait(true); break;
            case EditorKind.List: await SaveListAsync().ConfigureAwait(true); break;
            case EditorKind.Item: await SaveItemAsync().ConfigureAwait(true); break;
        }
    }

    public void ReportExternalNavigationError(string errorMessage) =>
        SetMessage(errorMessage, true);

    private void RebuildMasterCollections()
    {
        Channels.Clear(); foreach (var row in _channels.OrderBy(value => value.Code)) Channels.Add(new PricingChannelRow(row));
        PriceLists.Clear(); foreach (var row in _lists.OrderByDescending(value => value.Priority).ThenBy(value => value.Code)) PriceLists.Add(new PricingListRow(row));
        ProductOptions.Clear(); foreach (var row in _products.Where(value => value.IsActive).OrderBy(value => value.Code)) ProductOptions.Add(new PricingLookup(row.Id, $"{row.Code} — {row.Name}", row));
        ChannelOptions.Clear(); foreach (var row in _channels.Where(value => value.IsActive).OrderBy(value => value.Code)) ChannelOptions.Add(new PricingLookup(row.Id, $"{row.Code} — {row.Name}", row));
        CustomerGroupOptions.Clear(); foreach (var row in _groups.Where(value => value.IsActive).OrderBy(value => value.Code)) CustomerGroupOptions.Add(new PricingLookup(row.Id, $"{row.Code} — {row.Name}", row));
        CustomerOptions.Clear(); foreach (var row in _customers.Where(value => value.IsActive).OrderBy(value => value.Code)) CustomerOptions.Add(new PricingLookup(row.Id, $"{row.Code} — {row.Name}", row));
        RebuildOverviewModes();
        if (string.IsNullOrWhiteSpace(SelectedPriceListId) && _lists.Count > 0) SelectedPriceListId = _lists[0].Id;
        OnPropertyChanged(nameof(HasChannels)); OnPropertyChanged(nameof(NoChannels));
        OnPropertyChanged(nameof(HasPriceLists)); OnPropertyChanged(nameof(NoPriceLists));
    }

    private void RebuildOverviewModes()
    {
        var selected = OverviewMode;
        OverviewModes.Clear();
        OverviewModes.Add(new OverviewPriceListOption(BaseOnly, "Giá nền", null));
        OverviewModes.Add(new OverviewPriceListOption(AllLists, "Tất cả bảng giá", null));
        foreach (var list in _lists.Where(row => row.ListType != "BASE").OrderByDescending(row => row.Priority).ThenBy(row => row.Code))
            OverviewModes.Add(new OverviewPriceListOption(list.Id, $"{list.Code} · {list.Name}{(list.IsActive ? string.Empty : " · Ngừng")}", list));
        if (!OverviewModes.Any(row => row.Key == selected)) _overviewMode = BaseOnly;
        OnPropertyChanged(nameof(OverviewMode));
    }

    private void RebuildOverviewRows()
    {
        if (!_overviewLoaded) return;
        var search = OverviewSearch.Trim();
        var units = _units.ToDictionary(row => row.Id, row => string.IsNullOrWhiteSpace(row.Symbol) ? row.Name : row.Symbol!, StringComparer.Ordinal);
        var baseListIds = _lists.Where(row => row.ListType == "BASE").Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        var selectedLists = VisibleOverviewPriceLists;
        OverviewRows.Clear();
        foreach (var pair in _overviewVariants)
        {
            if (search.Length > 0 && !($"{pair.Product.Code} {pair.Product.Name} {pair.Variant.Sku} {pair.Variant.Name}").Contains(search, StringComparison.CurrentCultureIgnoreCase)) continue;
            var baseRules = _itemsByList.Where(entry => baseListIds.Contains(entry.Key)).SelectMany(entry => entry.Value).Where(item => item.Sku.Equals(pair.Variant.Sku, StringComparison.OrdinalIgnoreCase)).ToArray();
            var row = new PricingOverviewRow
            {
                ProductCode = pair.Product.Code,
                ProductName = pair.Product.Name,
                Sku = pair.Variant.Sku,
                VariantName = pair.Variant.Name,
                UnitName = pair.Variant.UnitId is not null && units.TryGetValue(pair.Variant.UnitId, out var unit) ? unit : "—",
                BasePrice = PricingPresentation.SummaryRule(baseRules)
            };
            foreach (var list in selectedLists)
            {
                var rules = _itemsByList.TryGetValue(list.Id, out var items) ? items.Where(item => item.Sku.Equals(pair.Variant.Sku, StringComparison.OrdinalIgnoreCase)).ToArray() : [];
                row.PriceCells[list.Id] = PricingPresentation.SummaryRule(rules);
            }
            OverviewRows.Add(row);
        }
        RaiseOverviewSummary();
        OnPropertyChanged(nameof(HasOverviewRows)); OnPropertyChanged(nameof(NoOverviewRows));
    }

    private void RebuildSelectedItems()
    {
        PriceItems.Clear();
        if (_itemsByList.TryGetValue(SelectedPriceListId, out var rows))
            foreach (var row in rows.OrderBy(value => value.Sku).ThenBy(value => DecimalSort(value.MinQuantity))) PriceItems.Add(new PricingItemRow(row));
        OnPropertyChanged(nameof(HasPriceItems)); OnPropertyChanged(nameof(NoPriceItems));
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true; MessageIsError = false;
        try { await action().ConfigureAwait(true); }
        catch (Exception exception) { SetMessage(exception.Message, true); }
        finally { IsBusy = false; }
    }

    private string KeyFor(string intent)
    {
        if (_intentKeys.TryGetValue(intent, out var key)) return key;
        key = _idempotencyKeys.Create(intent);
        _intentKeys[intent] = key;
        if (_intentKeys.Count > 256) _intentKeys.Remove(_intentKeys.Keys.First());
        return key;
    }

    private void CompleteIntent(string intent) => _intentKeys.Remove(intent);

    private void SetEditor(EditorKind kind)
    {
        _editorKind = kind;
        OnPropertyChanged(nameof(IsEditorOpen)); OnPropertyChanged(nameof(IsChannelEditor)); OnPropertyChanged(nameof(IsListEditor)); OnPropertyChanged(nameof(IsItemEditor));
        OnPropertyChanged(nameof(EditorTitle)); OnPropertyChanged(nameof(EditorDescription)); OnPropertyChanged(nameof(EditorSaveText)); OnPropertyChanged(nameof(CanSaveEditor));
    }

    private void RaiseActionState()
    {
        OnPropertyChanged(nameof(CanSaveEditor)); OnPropertyChanged(nameof(CanCreatePriceItem)); OnPropertyChanged(nameof(CanResolve));
    }

    private void RaiseListScopeState()
    {
        OnPropertyChanged(nameof(ListChannelEnabled)); OnPropertyChanged(nameof(ListCustomerGroupEnabled)); OnPropertyChanged(nameof(ListCustomerEnabled)); OnPropertyChanged(nameof(ListScopeHint));
    }

    private void RaiseResolution()
    {
        OnPropertyChanged(nameof(HasResolution)); OnPropertyChanged(nameof(ResolvedBasePrice)); OnPropertyChanged(nameof(ResolvedFinalPrice)); OnPropertyChanged(nameof(ResolvedLineTotal));
    }

    private void RaiseOverviewSummary()
    {
        OnPropertyChanged(nameof(OverviewSkuCount)); OnPropertyChanged(nameof(OverviewListCount)); OnPropertyChanged(nameof(OverviewRuleCount));
    }

    private void SetMessage(string value, bool isError) { Message = value; MessageIsError = isError; }

    private void ClearData()
    {
        _channels.Clear(); _lists.Clear(); _products.Clear(); _units.Clear(); _groups.Clear(); _customers.Clear(); _itemsByList.Clear(); _overviewVariants.Clear();
        Channels.Clear(); PriceLists.Clear(); PriceItems.Clear(); ProductOptions.Clear(); ItemVariantOptions.Clear(); ResolverVariantOptions.Clear(); ChannelOptions.Clear(); CustomerGroupOptions.Clear(); CustomerOptions.Clear(); OverviewModes.Clear(); OverviewRows.Clear(); ResolutionSteps.Clear();
    }

    private static void Replace<T>(List<T> target, IEnumerable<T> source) { target.Clear(); target.AddRange(source); }
    private static void ReplaceById<T>(List<T> target, T value, Func<T, string> id) { var index = target.FindIndex(row => id(row) == id(value)); if (index >= 0) target[index] = value; else target.Add(value); }
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static decimal DecimalSort(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : decimal.MaxValue;

    private static string? ApiDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateTimeOffset.TryParse(value.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            && !DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            throw new InvalidOperationException("Thời gian hiệu lực không đúng định dạng.");
        return parsed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string InputDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) return string.Empty;
        return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; OnPropertyChanged(propertyName); return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
