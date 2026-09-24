using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class OrderManagementRow : INotifyPropertyChanged
{
    private bool _isSelected;

    public required string Id { get; init; }
    public required string Number { get; init; }
    public required string CreatedAt { get; init; }
    public required string Customer { get; init; }
    public required string OrderStatus { get; init; }
    public required string Payment { get; init; }
    public required string Total { get; init; }
    public required string Fulfillment { get; init; }
    public required string Delivery { get; init; }
    public required string Lane { get; init; }
    public required string Source { get; init; }
    public required string StageKey { get; init; }
    public bool Printable { get; init; }

    public bool IsSelected
    {
        get=>_isSelected;
        set
        {
            if(_isSelected==value)return;
            _isSelected=value;
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class OrderManagementViewModel : INotifyPropertyChanged
{
    private const string ReadPermission="core.sales-order.read";
    private static readonly TimeSpan VietnamOffset=TimeSpan.FromHours(7);

    private readonly OrderManagementQueryService _service;
    private readonly IAccessStateService _access;
    private readonly List<SalesOrderData> _orders=[];
    private readonly List<SalesOrderData> _summaryOrders=[];
    private readonly List<SalesOrderData> _filtered=[];
    private readonly HashSet<string> _selectedIds=new(StringComparer.Ordinal);
    private bool _isLoaded;
    private bool _isBusy;
    private string _message=string.Empty;
    private bool _messageIsError;
    private string _search=string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _fromTime="00:00";
    private string _toTime="23:59";
    private string _stageFilter="all";
    private string _paymentFilter="all";
    private string _laneFilter="all";
    private string _sourceFilter="all";
    private int _pageSize=20;
    private int _pageIndex;

    public OrderManagementViewModel(OrderManagementQueryService service,IAccessStateService access)
    {
        _service=service;
        _access=access;
        _access.Changed+=(_,_)=>
        {
            OnPropertyChanged(nameof(CanRead));
            if(!CanRead)ClearSelection();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OrderManagementRow> Rows { get; }=[];
    public IReadOnlyList<SalesLookupOption> StageOptions=>OrderManagementPresentation.StageOptions;
    public IReadOnlyList<SalesLookupOption> PaymentOptions=>OrderManagementPresentation.PaymentOptions;
    public IReadOnlyList<SalesLookupOption> LaneOptions=>OrderManagementPresentation.LaneOptions;
    public IReadOnlyList<SalesLookupOption> SourceOptions=>OrderManagementPresentation.SourceOptions;
    public IReadOnlyList<SalesLookupOption> PageSizeOptions=>OrderManagementPresentation.PageSizeOptions;

    public bool CanRead=>_access.HasPermission(ReadPermission);
    public bool IsBusy { get=>_isBusy; private set { if(SetField(ref _isBusy,value)){OnPropertyChanged(nameof(IsNotBusy));OnPropertyChanged(nameof(CanPrintSelected));} } }
    public bool IsNotBusy=>!IsBusy;
    public string Message { get=>_message; private set { if(SetField(ref _message,value))OnPropertyChanged(nameof(HasMessage)); } }
    public bool MessageIsError { get=>_messageIsError; private set=>SetField(ref _messageIsError,value); }
    public bool HasMessage=>!string.IsNullOrWhiteSpace(Message);

    public string Search { get=>_search; set { if(SetField(ref _search,value??string.Empty))FilterChanged(); } }
    public DateTime? FromDate { get=>_fromDate; set { if(SetField(ref _fromDate,value))FilterChanged(); } }
    public DateTime? ToDate { get=>_toDate; set { if(SetField(ref _toDate,value))FilterChanged(); } }
    public string FromTime { get=>_fromTime; set { if(SetField(ref _fromTime,value??string.Empty))FilterChanged(); } }
    public string ToTime { get=>_toTime; set { if(SetField(ref _toTime,value??string.Empty))FilterChanged(); } }
    public string StageFilter { get=>_stageFilter; set { if(SetField(ref _stageFilter,value??"all"))FilterChanged(); } }
    public string PaymentFilter { get=>_paymentFilter; set { if(SetField(ref _paymentFilter,value??"all"))FilterChanged(); } }
    public string LaneFilter { get=>_laneFilter; set { if(SetField(ref _laneFilter,value??"all"))FilterChanged(); } }
    public string SourceFilter { get=>_sourceFilter; set { if(SetField(ref _sourceFilter,value??"all"))FilterChanged(); } }

    public string PageSizeValue
    {
        get=>PageSize.ToString(CultureInfo.InvariantCulture);
        set
        {
            if(!int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var parsed)||parsed is not (20 or 50 or 100))return;
            PageSize=parsed;
        }
    }

    public int PageSize
    {
        get=>_pageSize;
        private set
        {
            if(!SetField(ref _pageSize,value))return;
            _pageIndex=0;
            RefreshPage();
            OnPropertyChanged(nameof(PageSizeValue));
        }
    }

    public int PageIndex=>_pageIndex;
    public int PageCount=>Math.Max(1,(int)Math.Ceiling(_filtered.Count/(double)PageSize));
    public string PageText=>$"Trang {PageIndex+1}/{PageCount}";
    public bool CanPreviousPage=>PageIndex>0;
    public bool CanNextPage=>PageIndex+1<PageCount;
    public int FilteredCount=>_filtered.Count;
    public int SelectedCount=>_selectedIds.Count;
    public string SelectionText=>SelectedCount==0?"Chưa chọn đơn":$"Đã chọn {SelectedCount:N0} đơn";
    public bool CanPrintSelected=>IsNotBusy&&_selectedIds.Any(id=>_orders.FirstOrDefault(x=>x.Id==id) is { } order&&OrderManagementPresentation.IsPrintable(order));

    public int ActiveCount=>CountStage("active");
    public int PreparingCount=>CountStage("preparing");
    public int WaitingDeliveryCount=>CountStage("waiting_delivery");
    public int CompletedCount=>CountStage("completed");
    public int CancelledCount=>CountStage("cancelled");
    public string ActiveValue=>SumStage("active");
    public string PreparingValue=>SumStage("preparing");
    public string WaitingDeliveryValue=>SumStage("waiting_delivery");
    public string CompletedValue=>SumStage("completed");
    public string CancelledValue=>SumStage("cancelled");

    public async Task EnsureLoadedAsync()
    {
        if(_isLoaded)return;
        await RefreshAsync().ConfigureAwait(true);
    }

    public async Task RefreshAsync()
    {
        if(IsBusy)return;
        if(!CanRead)
        {
            MessageIsError=true;
            Message="Tài khoản chưa được cấp quyền xem Quản lý đơn hàng.";
            return;
        }

        IsBusy=true;
        Message=string.Empty;
        MessageIsError=false;
        try
        {
            var orders=await _service.ListAllAsync().ConfigureAwait(true);
            _orders.Clear();
            _orders.AddRange(orders);
            _isLoaded=true;
            _pageIndex=0;
            ClearSelection(false);
            ApplyFilters();
        }
        catch(Exception)
        {
            MessageIsError=true;
            Message="Không tải được danh sách đơn hàng. Vui lòng cập nhật dữ liệu và thử lại.";
        }
        finally
        {
            IsBusy=false;
        }
    }

    public void SetStage(string stage)=>StageFilter=stage;

    public void ToggleSelection(string id,bool selected)
    {
        if(selected)_selectedIds.Add(id);else _selectedIds.Remove(id);
        foreach(var row in Rows.Where(x=>x.Id==id))row.IsSelected=selected;
        RaiseSelection();
    }

    public void SelectAllFiltered()
    {
        foreach(var order in _filtered)_selectedIds.Add(order.Id);
        foreach(var row in Rows)row.IsSelected=_selectedIds.Contains(row.Id);
        RaiseSelection();
    }

    public void ClearSelection()=>ClearSelection(true);

    public void PreviousPage()
    {
        if(!CanPreviousPage)return;
        _pageIndex--;
        RefreshPage();
    }

    public void NextPage()
    {
        if(!CanNextPage)return;
        _pageIndex++;
        RefreshPage();
    }

    public async Task<IReadOnlyList<(SalesOrderData Order,SalesOrderVersionData Version)>> LoadPrintableSelectionAsync()
    {
        if(!CanPrintSelected)return [];
        var ids=_orders.Where(x=>_selectedIds.Contains(x.Id)&&OrderManagementPresentation.IsPrintable(x)).Select(x=>x.Id).ToArray();
        var result=new List<(SalesOrderData,SalesOrderVersionData)>();
        IsBusy=true;
        try
        {
            foreach(var id in ids)
            {
                var detail=await _service.GetAsync(id).ConfigureAwait(true);
                var version=OrderManagementPresentation.ActiveVersion(detail);
                if(version is not null&&OrderManagementPresentation.IsPrintable(detail))result.Add((detail,version));
            }
            if(result.Count==0)
            {
                MessageIsError=false;
                Message="Các đơn đã chọn hiện chưa đủ điều kiện in.";
            }
            return result;
        }
        catch(Exception)
        {
            MessageIsError=true;
            Message="Không tải được chi tiết các đơn cần in. Vui lòng thử lại.";
            return [];
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void FilterChanged()
    {
        _pageIndex=0;
        ClearSelection(false);
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        _summaryOrders.Clear();
        _filtered.Clear();
        if(!TryBuildRange(out var from,out var to))
        {
            Rows.Clear();
            MessageIsError=true;
            Message="Khoảng ngày giờ chưa hợp lệ. Thời điểm bắt đầu phải trước hoặc bằng thời điểm kết thúc.";
            RaiseCounts();
            RaisePage();
            return;
        }

        if(MessageIsError&&Message.StartsWith("Khoảng ngày giờ",StringComparison.Ordinal))
        {
            Message=string.Empty;
            MessageIsError=false;
        }

        var term=Search.Trim();
        foreach(var order in _orders)
        {
            var payment=OrderManagementPresentation.PaymentBucket(order);
            if(PaymentFilter!="all"&&payment!=PaymentFilter)continue;
            var lane=OrderManagementPresentation.DeliveryLane(order);
            if(LaneFilter!="all"&&lane!=LaneFilter)continue;
            var source=OrderManagementPresentation.SourceBucket(order);
            if(SourceFilter!="all"&&source!=SourceFilter)continue;
            var created=OrderManagementPresentation.CreatedAtValue(order);
            if(from is not null&&(created is null||created.Value<from.Value))continue;
            if(to is not null&&(created is null||created.Value>to.Value))continue;
            if(term.Length>0)
            {
                var haystack=$"{order.Number} {order.CustomerCode} {order.CustomerName} {order.WalkInDisplayName} {order.WalkInPhone} {order.WarehouseCode} {order.WarehouseName}";
                if(!haystack.Contains(term,StringComparison.OrdinalIgnoreCase))continue;
            }

            _summaryOrders.Add(order);
            var stage=OrderManagementPresentation.WorkStage(order);
            if(StageFilter=="all"||stage==StageFilter)_filtered.Add(order);
        }

        _filtered.Sort((a,b)=>Nullable.Compare(OrderManagementPresentation.CreatedAtValue(b),OrderManagementPresentation.CreatedAtValue(a)));
        RefreshPage();
        RaiseCounts();
    }

    private void RefreshPage()
    {
        if(_pageIndex>=PageCount)_pageIndex=Math.Max(0,PageCount-1);
        Rows.Clear();
        foreach(var order in _filtered.Skip(PageIndex*PageSize).Take(PageSize))
        {
            var stage=OrderManagementPresentation.WorkStage(order);
            var payment=OrderManagementPresentation.PaymentBucket(order);
            var lane=OrderManagementPresentation.DeliveryLane(order);
            var source=OrderManagementPresentation.SourceBucket(order);
            Rows.Add(new OrderManagementRow
            {
                Id=order.Id,
                Number=OrderManagementPresentation.CompactNumber(order.Number),
                CreatedAt=OrderManagementPresentation.CreatedAt(order),
                Customer=OrderManagementPresentation.Customer(order),
                OrderStatus=OrderManagementPresentation.OrderStatusLabel(order.Status),
                Payment=OrderManagementPresentation.PaymentLabel(payment),
                Total=OrderManagementPresentation.Total(order),
                Fulfillment=OrderManagementPresentation.FulfillmentLabel(order.FulfillmentStatus),
                Delivery=OrderManagementPresentation.DeliveryLabel(order),
                Lane=OrderManagementPresentation.LaneLabel(lane),
                Source=OrderManagementPresentation.SourceLabel(source),
                StageKey=stage,
                Printable=OrderManagementPresentation.IsPrintable(order),
                IsSelected=_selectedIds.Contains(order.Id)
            });
        }
        RaisePage();
    }

    private bool TryBuildRange(out DateTimeOffset? from,out DateTimeOffset? to)
    {
        from=null;to=null;
        if(FromDate is not null)
        {
            if(!TimeOnly.TryParseExact(FromTime,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time))return false;
            from=new DateTimeOffset(FromDate.Value.Date+time.ToTimeSpan(),VietnamOffset).ToUniversalTime();
        }
        if(ToDate is not null)
        {
            if(!TimeOnly.TryParseExact(ToTime,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time))return false;
            to=new DateTimeOffset(ToDate.Value.Date+time.ToTimeSpan(),VietnamOffset).ToUniversalTime();
        }
        return from is null||to is null||from<=to;
    }

    private int CountStage(string stage)=>_summaryOrders.Count(x=>OrderManagementPresentation.WorkStage(x)==stage);

    private string SumStage(string stage)
    {
        decimal total=0;
        foreach(var order in _summaryOrders.Where(x=>OrderManagementPresentation.WorkStage(x)==stage))
        {
            var amount=OrderManagementPresentation.ActiveVersion(order)?.Total ?? order.Total ?? "0";
            if(decimal.TryParse(amount,NumberStyles.Number,CultureInfo.InvariantCulture,out var value))total+=value;
        }
        return SalesPresentation.Money(total.ToString(CultureInfo.InvariantCulture));
    }

    private void ClearSelection(bool refreshRows)
    {
        _selectedIds.Clear();
        if(refreshRows)foreach(var row in Rows)row.IsSelected=false;
        RaiseSelection();
    }

    private void RaiseSelection()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(CanPrintSelected));
    }

    private void RaiseCounts()
    {
        foreach(var name in new[]{nameof(FilteredCount),nameof(ActiveCount),nameof(PreparingCount),nameof(WaitingDeliveryCount),nameof(CompletedCount),nameof(CancelledCount),nameof(ActiveValue),nameof(PreparingValue),nameof(WaitingDeliveryValue),nameof(CompletedValue),nameof(CancelledValue)})OnPropertyChanged(name);
    }

    private void RaisePage()
    {
        OnPropertyChanged(nameof(PageIndex));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(CanPreviousPage));
        OnPropertyChanged(nameof(CanNextPage));
        OnPropertyChanged(nameof(FilteredCount));
    }

    private bool SetField<T>(ref T field,T value,[CallerMemberName] string? name=null)
    {
        if(EqualityComparer<T>.Default.Equals(field,value))return false;
        field=value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}
