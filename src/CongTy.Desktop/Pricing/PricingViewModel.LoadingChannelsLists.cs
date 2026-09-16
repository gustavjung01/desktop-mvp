using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public sealed partial class PricingViewModel
{
    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsBusy) return;
        if (!CanRead)
        {
            SetMessage("Tài khoản chưa được cấp quyền xem Giá bán và khuyến mãi.", true);
            return;
        }
        await LoadAllAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        _loaded = false;
        _overviewLoaded = false;
        _itemsByList.Clear();
        await LoadAllAsync().ConfigureAwait(true);
        if (TabIndex == 2 && !string.IsNullOrWhiteSpace(SelectedPriceListId)) await LoadPriceItemsAsync(SelectedPriceListId).ConfigureAwait(true);
        if (TabIndex == 3) await EnsureOverviewAsync().ConfigureAwait(true);
    }

    public async Task ActivateTabAsync(int index)
    {
        TabIndex = index;
        await EnsureLoadedAsync().ConfigureAwait(true);
        if (index == 2 && !string.IsNullOrWhiteSpace(SelectedPriceListId)) await LoadPriceItemsAsync(SelectedPriceListId).ConfigureAwait(true);
        if (index == 3) await EnsureOverviewAsync().ConfigureAwait(true);
    }

    private async Task LoadAllAsync()
    {
        if (!CanRead || IsBusy) return;
        IsBusy = true;
        SetMessage("Đang tải dữ liệu giá bán...", false);
        try
        {
            var channelsTask = _service.ListChannelsAsync();
            var listsTask = _service.ListPriceListsAsync();
            var productsTask = _service.ListProductsAsync();
            var unitsTask = _service.ListUnitsAsync();
            var groupsTask = _service.ListCustomerGroupsAsync();
            var customersTask = _service.ListCustomersAsync();
            await Task.WhenAll(channelsTask, listsTask, productsTask, unitsTask, groupsTask, customersTask).ConfigureAwait(true);
            Replace(_channels, channelsTask.Result.OrderBy(row => row.Code));
            Replace(_lists, listsTask.Result.OrderByDescending(row => row.Priority).ThenBy(row => row.Code));
            Replace(_products, productsTask.Result.OrderBy(row => row.Code));
            Replace(_units, unitsTask.Result.OrderBy(row => row.Code));
            Replace(_groups, groupsTask.Result.OrderBy(row => row.Code));
            Replace(_customers, customersTask.Result.OrderBy(row => row.Code));
            RebuildMasterCollections();
            _loaded = true;
            SetMessage(string.Empty, false);
        }
        catch (Exception exception) { SetMessage(exception.Message, true); }
        finally { IsBusy = false; }
    }

    public void OpenChannelCreate()
    {
        if (!CanWrite) { SetMessage("Tài khoản chưa được cấp quyền chỉnh sửa giá bán.", true); return; }
        _editingChannel = null;
        ChannelCode = string.Empty; ChannelName = string.Empty; ChannelDescription = string.Empty; ChannelActive = true;
        SetEditor(EditorKind.Channel);
    }

    public void OpenChannelEdit(PricingChannelRow row)
    {
        if (!CanWrite) return;
        _editingChannel = row.Data;
        ChannelCode = row.Data.Code; ChannelName = row.Data.Name; ChannelDescription = row.Data.Description ?? string.Empty; ChannelActive = row.Data.IsActive;
        SetEditor(EditorKind.Channel);
        OnPropertyChanged(nameof(ChannelCodeEnabled));
    }

    public async Task SaveChannelAsync()
    {
        if (!CanSaveEditor || _editorKind != EditorKind.Channel) return;
        await RunBusyAsync(async () =>
        {
            if (_editingChannel is null)
            {
                var intent = $"pricing-channel-create-{ChannelCode.Trim().ToUpperInvariant()}";
                var saved = await _service.CreateChannelAsync(new SalesChannelCreateRequest(ChannelCode.Trim(), ChannelName.Trim(), NullIfBlank(ChannelDescription), ChannelActive), KeyFor(intent)).ConfigureAwait(true);
                CompleteIntent(intent);
                _channels.Add(saved);
                SetMessage("Đã tạo kênh bán.", false);
            }
            else
            {
                var saved = await _service.UpdateChannelAsync(_editingChannel.Id, new SalesChannelUpdateRequest(ChannelName.Trim(), NullIfBlank(ChannelDescription), ChannelActive, _editingChannel.UpdatedAt)).ConfigureAwait(true);
                ReplaceById(_channels, saved, row => row.Id);
                SetMessage("Đã cập nhật kênh bán.", false);
            }
            RebuildMasterCollections();
            CloseEditor();
        }).ConfigureAwait(true);
    }

    public async Task ToggleChannelAsync(PricingChannelRow row) => await RunBusyAsync(async () =>
    {
        var current = row.Data;
        var saved = await _service.UpdateChannelAsync(current.Id, new SalesChannelUpdateRequest(current.Name, current.Description, !current.IsActive, current.UpdatedAt)).ConfigureAwait(true);
        ReplaceById(_channels, saved, value => value.Id);
        RebuildMasterCollections();
    }).ConfigureAwait(true);

    public void OpenListCreate()
    {
        if (!CanWrite) { SetMessage("Tài khoản chưa được cấp quyền chỉnh sửa giá bán.", true); return; }
        _editingList = null;
        ListCode = string.Empty; ListName = string.Empty; ListType = "BASE"; ListPriority = "100"; ListStacking = "EXCLUSIVE"; ListStopProcessing = false;
        ListChannelId = string.Empty; ListCustomerGroupId = string.Empty; ListCustomerId = string.Empty; ListEffectiveFrom = string.Empty; ListEffectiveTo = string.Empty; ListDescription = string.Empty; ListActive = true;
        SetEditor(EditorKind.List);
        OnPropertyChanged(nameof(ListIdentityEnabled));
    }

    public void OpenListEdit(PricingListRow row)
    {
        if (!CanWrite) return;
        var data = row.Data;
        _editingList = data;
        _listType = data.ListType;
        OnPropertyChanged(nameof(ListType));
        ListCode = data.Code; ListName = data.Name; ListPriority = data.Priority.ToString(CultureInfo.InvariantCulture); ListStacking = data.StackingMode; ListStopProcessing = data.StopProcessing;
        ListChannelId = data.ChannelId ?? string.Empty; ListCustomerGroupId = data.CustomerGroupId ?? string.Empty; ListCustomerId = data.CustomerId ?? string.Empty;
        ListEffectiveFrom = InputDate(data.EffectiveFrom); ListEffectiveTo = InputDate(data.EffectiveTo); ListDescription = data.Description ?? string.Empty; ListActive = data.IsActive;
        SetEditor(EditorKind.List);
        RaiseListScopeState();
        OnPropertyChanged(nameof(ListIdentityEnabled));
    }

    public async Task SaveListAsync()
    {
        if (!CanSaveEditor || _editorKind != EditorKind.List) return;
        if (!int.TryParse(ListPriority, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority) || priority < 0) { SetMessage("Thứ tự ưu tiên phải là số không âm.", true); return; }
        string? from; string? to;
        try { from = ApiDate(ListEffectiveFrom); to = ApiDate(ListEffectiveTo); }
        catch (Exception exception) { SetMessage(exception.Message, true); return; }
        if (from is not null && to is not null && DateTimeOffset.Parse(to) <= DateTimeOffset.Parse(from)) { SetMessage("Hiệu lực đến phải sau hiệu lực từ.", true); return; }

        await RunBusyAsync(async () =>
        {
            if (_editingList is null)
            {
                var intent = $"pricing-list-create-{ListCode.Trim().ToUpperInvariant()}";
                var saved = await _service.CreatePriceListAsync(new PriceListCreateRequest(
                    ListCode.Trim(), ListName.Trim(), ListType, priority, ListStacking, ListStopProcessing,
                    NullIfBlank(ListChannelId), NullIfBlank(ListCustomerGroupId), NullIfBlank(ListCustomerId), from, to, NullIfBlank(ListDescription), ListActive), KeyFor(intent)).ConfigureAwait(true);
                CompleteIntent(intent);
                _lists.Add(saved);
                SelectedPriceListId = saved.Id;
                SetMessage("Đã tạo bảng giá.", false);
            }
            else
            {
                var saved = await _service.UpdatePriceListAsync(_editingList.Id, new PriceListUpdateRequest(
                    ListName.Trim(), priority, ListStacking, ListStopProcessing, NullIfBlank(ListChannelId), NullIfBlank(ListCustomerGroupId), NullIfBlank(ListCustomerId), from, to, NullIfBlank(ListDescription), ListActive, _editingList.UpdatedAt)).ConfigureAwait(true);
                ReplaceById(_lists, saved, value => value.Id);
                SetMessage("Đã cập nhật bảng giá.", false);
            }
            RebuildMasterCollections();
            _overviewLoaded = false;
            CloseEditor();
        }).ConfigureAwait(true);
    }

    public async Task ToggleListAsync(PricingListRow row) => await RunBusyAsync(async () =>
    {
        var current = row.Data;
        var saved = await _service.UpdatePriceListStatusAsync(current.Id, new PriceListStatusUpdateRequest(!current.IsActive, current.UpdatedAt)).ConfigureAwait(true);
        ReplaceById(_lists, saved, value => value.Id);
        RebuildMasterCollections();
        _overviewLoaded = false;
    }).ConfigureAwait(true);

    public async Task OpenListItemsAsync(PricingListRow row)
    {
        SelectedPriceListId = row.Id;
        TabIndex = 2;
        await LoadPriceItemsAsync(row.Id).ConfigureAwait(true);
    }

    public async Task SelectPriceListAsync(string? id)
    {
        SelectedPriceListId = id ?? string.Empty;
        CloseEditor();
        if (!string.IsNullOrWhiteSpace(SelectedPriceListId)) await LoadPriceItemsAsync(SelectedPriceListId).ConfigureAwait(true);
        else
        {
            PriceItems.Clear();
            OnPropertyChanged(nameof(HasPriceItems)); OnPropertyChanged(nameof(NoPriceItems));
        }
    }

    private async Task LoadPriceItemsAsync(string priceListId)
    {
        if (string.IsNullOrWhiteSpace(priceListId) || !CanRead) return;
        IsBusy = true;
        try
        {
            var rows = await _service.ListPriceItemsAsync(priceListId).ConfigureAwait(true);
            _itemsByList[priceListId] = rows;
            RebuildSelectedItems();
            SetMessage(string.Empty, false);
        }
        catch (Exception exception) { SetMessage(exception.Message, true); }
        finally { IsBusy = false; }
    }
}
