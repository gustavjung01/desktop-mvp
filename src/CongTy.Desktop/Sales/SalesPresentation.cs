using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed record SalesLookupOption(string Id, string Label);
public sealed record SalesOrderRow(string Stt,string Id,string Number,string Customer,string Warehouse,string Lane,string LaneKey,string Stage,string StageKey,string Source,string Total,string Settlement,string CreatedAt);
public sealed record SalesOrderLineRow(string LineNumber,string Sku,string Item,string Quantity,string OnHand,string HeldByOthers,string Available,string UnitPrice,string Discount,string Tax,string Total);
public sealed record SalesOrderVersionRow(string Version,string Status,string Reason,string Total,string CreatedAt);
public sealed record SalesSkuSearchRow(string Id,string Sku,string Product,string Unit,string Price,string Available,string Held,string Eligibility,bool Selectable,string? PriceMinor,SalesOrderSkuSearchOptionData Source);
public sealed record SalesDraftEstimateResult(decimal Gross,decimal Discount,decimal Tax,decimal Total,bool Valid,bool MixedScope);
public sealed record SalesPriceStepRow(string Label,string Detail,string After);
public sealed record SalesInventoryHistoryRow(string Time,string Movement,string Document,string Quantity,string StockAfter,string LocationLot,string User);

public sealed class SalesDraftLineRow : INotifyPropertyChanged
{
    private string _variantId;
    private string? _productId;
    private string _sku;
    private string _unitCode;
    private string _conversionToBase;
    private bool _allowsFractional;
    private string _quantity;
    private string _manualUnitPriceMinor;
    private string _manualReason;
    private string _discountMode;
    private string _discountValue;
    private string _baseUnitPriceMinor=string.Empty;
    private string _systemUnitPriceMinor=string.Empty;
    private string _pricingFingerprint=string.Empty;
    private string _priceSource="PRICE_ENGINE";
    private string _priceError=string.Empty;
    private string _pricingErrorCode=string.Empty;
    private bool _isPriceDetailExpanded;

    public SalesDraftLineRow(
        string variantId,string sku,string itemName,string unitCode,string taxMode,string taxRate,
        string quantity,string systemUnitPriceMinor,string manualUnitPriceMinor,string manualReason,
        string discountMode,string discountValue,string? productId=null,string conversionToBase="1",bool allowsFractional=false,
        string? clientLineId=null)
    {
        ClientLineId=string.IsNullOrWhiteSpace(clientLineId)?Guid.NewGuid().ToString("N"):clientLineId;
        _variantId=variantId; _productId=productId; _sku=sku; ItemName=itemName; _unitCode=unitCode;
        TaxMode=taxMode; TaxRate=taxRate; _conversionToBase=conversionToBase; _allowsFractional=allowsFractional;
        _quantity=OfficeNumberFormatting.CompactInvariant(quantity,"1"); _systemUnitPriceMinor=systemUnitPriceMinor; _manualUnitPriceMinor=manualUnitPriceMinor;
        _manualReason=manualReason; _discountMode=discountMode; _discountValue=OfficeNumberFormatting.CompactInvariant(discountValue,"0");
        UnitOptions.Add(new SalesLookupOption(_variantId,string.IsNullOrWhiteSpace(_unitCode)?"ĐVT":_unitCode));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<SalesLookupOption> UnitOptions { get; }=[];
    public ObservableCollection<SalesPriceStepRow> PriceSteps { get; }=[];
    public string ClientLineId { get; }
    public string VariantId=>_variantId;
    public string? ProductId=>_productId;
    public string Sku=>_sku;
    public string ItemName { get; }
    public string UnitCode=>_unitCode;
    public string ConversionToBase=>_conversionToBase;
    public bool AllowsFractional=>_allowsFractional;
    public bool HasMultipleUnits=>UnitOptions.Count>1;
    public string TaxMode { get; set; }
    public string TaxRate { get; set; }
    public string TaxRateText=>OfficeNumberFormatting.Percent(TaxRate);
    public string BaseUnitPriceMinor=>_baseUnitPriceMinor;
    public string BasePriceText=>SalesPresentation.Money(_baseUnitPriceMinor);
    public string SystemUnitPriceMinor { get=>_systemUnitPriceMinor; set { if(SetField(ref _systemUnitPriceMinor,value)){ RaiseComputed(); } } }
    public string SystemPriceText=>SalesPresentation.Money(SystemUnitPriceMinor);
    public string PricingFingerprint=>_pricingFingerprint;
    public string PriceSource=>_priceSource;
    public string PriceSourceText=>SalesPresentation.PriceSourceLabel(_priceSource);
    public string PriceError=>_priceError;
    public string PricingErrorCode=>_pricingErrorCode;
    public bool HasPriceError=>!string.IsNullOrWhiteSpace(_priceError);
    public bool HasPriceSteps=>PriceSteps.Count>0;
    public bool IsPriceDetailExpanded { get=>_isPriceDetailExpanded; set=>SetBoolField(ref _isPriceDetailExpanded,value); }
    public string Quantity { get=>_quantity; set { if(SetField(ref _quantity,value))RaiseComputed(); } }
    public string ManualUnitPriceMinor { get=>_manualUnitPriceMinor; set { if(SetField(ref _manualUnitPriceMinor,value)){ RaiseComputed(); } } }
    public bool HasManualPrice=>!string.IsNullOrWhiteSpace(ManualUnitPriceMinor);
    public string DirectUnitPriceText
    {
        get=>SalesPresentation.MoneyInput(SalesPresentation.FinalUnitPrice(this));
        set=>ManualUnitPriceMinor=SalesPresentation.NormalizeMoneyInput(value);
    }
    public string ManualReason { get=>_manualReason; set=>SetField(ref _manualReason,value); }
    public string DiscountMode { get=>_discountMode; set { if(SetField(ref _discountMode,value))RaiseComputed(); } }
    public string DiscountValue { get=>_discountValue; set { if(SetField(ref _discountValue,value))RaiseComputed(); } }
    public string UnitPriceText=>SalesPresentation.Money(SalesPresentation.FinalUnitPrice(this));
    public string DiscountText=>SalesPresentation.Money(SalesPresentation.EstimateDraft([this],"NONE","0").Discount.ToString(CultureInfo.InvariantCulture));
    public string TaxText=>SalesPresentation.Money(SalesPresentation.EstimateDraft([this],"NONE","0").Tax.ToString(CultureInfo.InvariantCulture));
    public string TotalText=>SalesPresentation.Money(SalesPresentation.EstimateDraft([this],"NONE","0").Total.ToString(CultureInfo.InvariantCulture));

    public void SetProductIdentity(string productId,string? conversionToBase,bool? allowsFractional)
    {
        _productId=productId;
        if(!string.IsNullOrWhiteSpace(conversionToBase))_conversionToBase=conversionToBase;
        _allowsFractional=allowsFractional??_allowsFractional;
        OnPropertyChanged(nameof(ProductId)); OnPropertyChanged(nameof(ConversionToBase)); OnPropertyChanged(nameof(AllowsFractional));
    }

    public void SetUnitOptions(IEnumerable<ProductVariantData> variants)
    {
        var options=variants.Select(v=>new SalesLookupOption(v.Id,SalesPresentation.VariantUnitLabel(v))).ToList();
        if(options.All(x=>x.Id!=VariantId))
            options.Insert(0,new SalesLookupOption(VariantId,string.IsNullOrWhiteSpace(UnitCode)?"ĐVT":UnitCode));
        UnitOptions.Clear();
        foreach(var option in options)UnitOptions.Add(option);
        OnPropertyChanged(nameof(HasMultipleUnits));
        OnPropertyChanged(nameof(VariantId));
    }

    public void CopyUnitOptionsFrom(SalesDraftLineRow source)
    {
        UnitOptions.Clear();
        foreach(var option in source.UnitOptions)UnitOptions.Add(option);
        OnPropertyChanged(nameof(HasMultipleUnits));
    }

    public bool ApplyVariant(ProductVariantData variant)
    {
        if(variant.Id==VariantId)return false;
        _variantId=variant.Id; _productId=variant.ProductId; _sku=variant.Sku;
        _unitCode=SalesPresentation.VariantUnitLabel(variant);
        _conversionToBase=variant.ConversionToBase??"1";
        _allowsFractional=variant.AllowsFractional??false;
        _manualUnitPriceMinor=string.Empty; _manualReason=string.Empty;
        ClearPricingForReprice();
        foreach(var name in new[]{nameof(VariantId),nameof(ProductId),nameof(Sku),nameof(UnitCode),nameof(ConversionToBase),nameof(AllowsFractional),nameof(ManualUnitPriceMinor),nameof(ManualReason),nameof(HasManualPrice),nameof(DirectUnitPriceText)})
            OnPropertyChanged(name);
        RaiseComputed();
        return true;
    }

    public void ApplyPricingResolution(SalesPriceResolutionData resolution)
    {
        _baseUnitPriceMinor=resolution.BaseUnitPriceMinor;
        _systemUnitPriceMinor=resolution.SystemUnitPriceMinor;
        _pricingFingerprint=resolution.ResolutionFingerprint;
        _priceSource=string.IsNullOrWhiteSpace(resolution.PriceSource)?"PRICE_ENGINE":resolution.PriceSource;
        _priceError=string.Empty; _pricingErrorCode=string.Empty;
        PriceSteps.Clear();
        foreach(var step in SalesPresentation.PriceStepRows(resolution.Steps))PriceSteps.Add(step);
        RaisePricing();
        RaiseComputed();
    }

    public void ApplyStoredPricing(string baseUnitPrice,string systemUnitPrice,string priceSource,IEnumerable<SalesPriceStepData> steps)
    {
        _baseUnitPriceMinor=baseUnitPrice;
        _systemUnitPriceMinor=systemUnitPrice;
        _priceSource=string.IsNullOrWhiteSpace(priceSource)?"PRICE_ENGINE":priceSource;
        var sourceSteps=steps.ToArray();
        _pricingFingerprint=sourceSteps.FirstOrDefault(x=>x.Kind=="RESOLUTION")?.ResolutionFingerprint??string.Empty;
        PriceSteps.Clear();
        foreach(var step in SalesPresentation.PriceStepRows(sourceSteps))PriceSteps.Add(step);
        RaisePricing();
        RaiseComputed();
    }

    public void ClearPricingForReprice()
    {
        _baseUnitPriceMinor="0"; _systemUnitPriceMinor="0"; _pricingFingerprint=string.Empty; _priceSource="PRICE_ENGINE";
        _priceError=string.Empty; _pricingErrorCode=string.Empty; PriceSteps.Clear();
        RaisePricing(); RaiseComputed();
    }

    public void SetPricingError(string code,string message)
    {
        _pricingFingerprint=string.Empty; _priceError=message; _pricingErrorCode=code; PriceSteps.Clear();
        RaisePricing();
    }

    public void TogglePriceDetail()=>IsPriceDetailExpanded=!IsPriceDetailExpanded;
    public void UseSystemPrice()=>ManualUnitPriceMinor=string.Empty;

    private bool SetField(ref string field,string value,[CallerMemberName] string? name=null)
    { if(field==value)return false; field=value; OnPropertyChanged(name); return true; }
    private bool SetBoolField(ref bool field,bool value,[CallerMemberName] string? name=null)
    { if(field==value)return false; field=value; OnPropertyChanged(name); return true; }

    private void RaisePricing()
    {
        foreach(var name in new[]{nameof(BaseUnitPriceMinor),nameof(BasePriceText),nameof(SystemPriceText),nameof(PricingFingerprint),nameof(PriceSource),nameof(PriceSourceText),nameof(PriceError),nameof(PricingErrorCode),nameof(HasPriceError),nameof(HasPriceSteps)})
            OnPropertyChanged(name);
    }

    private void RaiseComputed()
    {
        foreach(var name in new[]{nameof(UnitPriceText),nameof(DirectUnitPriceText),nameof(HasManualPrice),nameof(DiscountText),nameof(TaxText),nameof(TotalText)})
            OnPropertyChanged(name);
    }

    private void OnPropertyChanged(string? name)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}

public static class SalesPresentation
{
    public static readonly IReadOnlyList<SalesLookupOption> LaneOptions =
    [new("all","Tất cả"),new("counter","Mua tại quầy"),new("manual","Giao thủ công"),new("trip","Giao theo chuyến")];

    public static readonly IReadOnlyList<SalesLookupOption> StageOptions =
    [new("all","Tất cả trạng thái"),new("active","Đang xử lý"),new("preparing","Đang chuẩn bị"),new("waiting_delivery","Chờ giao"),new("completed","Đã hoàn thành"),new("cancelled","Hủy")];

    public static readonly IReadOnlyList<SalesLookupOption> SourceOptions =
    [new("all","Tất cả nguồn"),new("internal","Công Ty"),new("mcp","Nhân viên thị trường"),new("customer","Khách hàng")];

    public static readonly IReadOnlyList<SalesLookupOption> CustomerModeOptions =
    [new("EXISTING","Khách hàng"),new("WALK_IN","Khách vãng lai")];

    public static readonly IReadOnlyList<SalesLookupOption> DeliveryChoiceOptions =
    [new("TRIP","Giao theo chuyến"),new("MANUAL","Giao thủ công"),new("PICKUP","Mua tại quầy")];

    public static readonly IReadOnlyList<SalesLookupOption> CollectionOptions =
    [new("COLLECT_ON_DELIVERY","Thu khi giao/nhận"),new("PREPAID","Đã trả trước"),new("COLLECT_AFTER_DELIVERY","Giao trước, thu sau"),new("CREDIT_TERMS","Bán chịu theo hạn mức")];

    public static readonly IReadOnlyList<SalesLookupOption> PriceSelectionOptions =
    [new("STANDARD","Giá hiện hành"),new("LAST_PURCHASE","Giá lần mua trước")];

    public static readonly IReadOnlyList<SalesLookupOption> DiscountModeOptions =
    [new("PERCENT","%"),new("PER_UNIT","đ/ĐVT"),new("TOTAL_AMOUNT","Tổng đ")];

    public static readonly IReadOnlyList<SalesLookupOption> DocumentDiscountModeOptions =
    [new("NONE","Không chiết khấu"),new("PERCENT","Phần trăm"),new("TOTAL_AMOUNT","Số tiền")];

    public static readonly IReadOnlyList<SalesLookupOption> PaymentMethodOptions =
    [new("CASH","Tiền mặt"),new("BANK_TRANSFER","Chuyển khoản")];

    public static SalesOrderVersionData? ActiveVersion(SalesOrderData? order) =>
        order?.Versions.FirstOrDefault(v=>v.VersionNumber==order.CurrentVersionNumber) ?? order?.Versions.FirstOrDefault();

    public static SalesOrderVersionData? PendingVersion(SalesOrderData? order) =>
        order?.Versions.FirstOrDefault(v=>v.Status=="draft");

    public static string Lane(SalesOrderData order) =>
        order.DeliveryMode=="PICKUP" ? "counter" : order.DeliveryExecutionMode=="MANUAL" ? "manual" : "trip";

    public static string LaneLabel(SalesOrderData order) => Lane(order) switch
    { "counter"=>"Mua tại quầy","manual"=>"Giao thủ công",_=>"Giao theo chuyến" };

    public static string SourceBucket(SalesOrderData order) =>
        order.SourceType=="MCP" ? "mcp" :
        order.SourceType=="API" && order.SourceId?.StartsWith("CUSTOMER_PORTAL:",StringComparison.Ordinal)==true ? "customer" : "internal";

    public static string SourceLabel(SalesOrderData order) => SourceBucket(order) switch
    { "mcp"=>"Nhân viên thị trường","customer"=>"Khách hàng",_=>"Công Ty" };

    public static string Stage(SalesOrderData order)
    {
        if(order.Status=="cancelled"||order.DeliveryStatus=="cancelled") return "cancelled";
        if(order.Status=="closed"||order.DeliveryStatus=="delivered") return "completed";
        if(order.DeliveryStatus=="returned") return "active";
        if(new[]{"ready_to_dispatch","dispatched","partially_delivered","failed","rescheduled"}.Contains(order.DeliveryStatus)||order.FulfillmentStatus=="issued") return "waiting_delivery";
        if(order.Status=="confirmed"&&new[]{"reserved","partially_allocated","allocated","partially_fulfilled","fulfilled"}.Contains(order.FulfillmentStatus)) return "preparing";
        return "active";
    }

    public static string StageLabel(SalesOrderData order)
    {
        if(order.Status=="draft") return "Đặt hàng";
        return Stage(order) switch
        {
            "cancelled"=>"Đã hủy",
            "completed"=>order.DeliveryStatus=="delivered" ? "Đã giao" : "Đã hoàn thành",
            "waiting_delivery"=>DeliveryStatus(order.DeliveryStatus,order.FulfillmentStatus),
            "preparing"=>"Đang chuẩn bị",
            _=>"Đang xử lý"
        };
    }

    public static string OrderStatus(string status)=>status switch
    { "draft"=>"Nháp","confirmed"=>"Đã xác nhận","cancelled"=>"Đã hủy","closed"=>"Đã hoàn tất",_=>status };

    public static string VersionStatus(string status)=>status switch
    { "draft"=>"Nháp","confirmed"=>"Đã xác nhận","superseded"=>"Đã thay thế","cancelled"=>"Đã hủy",_=>status };

    public static string FulfillmentStatus(string status)=>status switch
    {
        "unallocated"=>"Chưa tạo nhu cầu giữ hàng","backordered"=>"Đang chờ hàng","partially_reserved"=>"Đã giữ một phần",
        "reserved"=>"Đã giữ đủ hàng","partially_allocated"=>"Phân bổ một phần","allocated"=>"Đã phân bổ",
        "partially_fulfilled"=>"Thực hiện một phần","fulfilled"=>"Đã thực hiện","issued"=>"Đã xuất kho","cancelled"=>"Đã hủy",_=>status
    };

    public static string DeliveryStatus(string status,string fulfillment="")=>status switch
    {
        "not_required"=>"Khách nhận tại kho","pending"=>fulfillment=="issued" ? "Đã xuất kho" : "Chờ chuẩn bị giao",
        "ready_to_dispatch"=>"Sẵn sàng xuất phát","dispatched"=>"Đang giao","partially_delivered"=>"Đã giao một phần",
        "delivered"=>"Đã giao","failed"=>"Giao chưa thành công","rescheduled"=>"Đã hẹn lại","returned"=>"Đã trả hàng","cancelled"=>"Đã hủy",_=>status
    };

    public static string SettlementStatus(string status)=>status switch
    {
        "not_due"=>"Chưa đến bước thu tiền","pending"=>"Chờ thanh toán","partially_paid"=>"Đã thanh toán một phần",
        "paid"=>"Đã thanh toán","overpaid"=>"Thanh toán thừa","refunded"=>"Đã hoàn tiền","written_off"=>"Đã xử lý xóa nợ",_=>status
    };

    public static string CollectionPolicy(string value)=>value switch
    { "PREPAID"=>"Đã trả trước","COLLECT_ON_DELIVERY"=>"Thu khi giao","COLLECT_AFTER_DELIVERY"=>"Giao trước, thu sau","CREDIT_TERMS"=>"Bán chịu theo hạn mức",_=>value };

    public static SalesDraftLineRow DraftLineFromVersion(SalesOrderLineData line,bool resetManualPrice=false)
    {
        var row=new SalesDraftLineRow(line.VariantId,line.Sku,line.ItemName,line.UnitName??line.UnitCode,line.TaxMode,line.TaxRate,line.Quantity,line.SystemUnitPrice,
            resetManualPrice?string.Empty:line.ManualOverrideReason is null?string.Empty:line.UnitPrice,
            resetManualPrice?string.Empty:line.ManualOverrideReason??string.Empty,line.DiscountMode,line.DiscountValue,
            conversionToBase:line.ConversionToBase,clientLineId:resetManualPrice?null:line.Id);
        row.ApplyStoredPricing(line.BaseUnitPrice,line.SystemUnitPrice,resetManualPrice?"PRICE_ENGINE":line.PriceSource,resetManualPrice?[]:line.PricingTrace);
        return row;
    }

    public static SalesDraftLineRow SplitDraftLine(SalesDraftLineRow source)
    {
        var split=new SalesDraftLineRow(source.VariantId,source.Sku,source.ItemName,source.UnitCode,source.TaxMode,source.TaxRate,
            "1","0",string.Empty,string.Empty,"PERCENT","0",source.ProductId,source.ConversionToBase,source.AllowsFractional);
        split.CopyUnitOptionsFrom(source);
        return split;
    }

    public static string FinalUnitPrice(SalesDraftLineRow line)=>
        string.IsNullOrWhiteSpace(line.ManualUnitPriceMinor)?line.SystemUnitPriceMinor:line.ManualUnitPriceMinor;

    public static string MoneyInput(string? value)
    {
        if(!decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount))return string.Empty;
        return amount.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"));
    }

    public static string NormalizeMoneyInput(string? value)
    {
        var digits=new string((value??string.Empty).Where(char.IsDigit).ToArray());
        if(digits.Length==0)return string.Empty;
        var trimmed=digits.TrimStart('0');
        return trimmed.Length==0?"0":trimmed;
    }

    public static string VariantUnitLabel(ProductVariantData variant)
    {
        var label=variant.UnitName?.Trim();
        if(string.IsNullOrWhiteSpace(label))label=variant.UnitSymbol?.Trim();
        if(string.IsNullOrWhiteSpace(label))label=variant.UnitCode?.Trim();
        return string.IsNullOrWhiteSpace(label)?"ĐVT":label;
    }

    public static string PriceSourceLabel(string? source)=>source switch
    {
        "HISTORY_REFERENCE"=>"Giá lần mua trước",
        "MANUAL_OVERRIDE"=>"Giá nhập tay",
        _=>"Giá hệ thống"
    };

    public static IReadOnlyList<SalesPriceStepRow> PriceStepRows(IEnumerable<SalesPriceStepData> steps)=>
        steps.Select(step=>new SalesPriceStepRow(PriceStepLabel(step),PriceStepDetail(step),
            string.IsNullOrWhiteSpace(step.AfterUnitPriceMinor)?string.Empty:Money(step.AfterUnitPriceMinor))).ToArray();

    private static string PriceStepLabel(SalesPriceStepData step)=>step.Kind switch
    {
        "BASE"=>"Giá nền",
        "RULE"=>PricePolicyLabel(step.PriceListCode,step.PriceListType),
        "SKIPPED"=>$"{PricePolicyLabel(step.PriceListCode,step.PriceListType)} · Không áp dụng",
        "HISTORY_REFERENCE"=>"Giá lần mua trước",
        "MANUAL_OVERRIDE"=>"Giá nhập tay",
        "RESOLUTION"=>"Kết quả tính giá",
        _=>"Bước tính giá"
    };

    private static string PriceStepDetail(SalesPriceStepData step)
    {
        if(step.Kind=="SKIPPED")return PricingReasonLabel(step.Reason);
        if(step.Kind=="HISTORY_REFERENCE")
            return string.IsNullOrWhiteSpace(step.SourceSalesOrderNumber)?"Tham chiếu lần mua trước":$"Đơn {step.SourceSalesOrderNumber}";
        if(step.Kind=="RULE")
        {
            if(step.RateBps is int bps&&bps!=0)return $"{(bps/100m).ToString("0.##",CultureInfo.GetCultureInfo("vi-VN"))}%";
            if(!string.IsNullOrWhiteSpace(step.AmountMinor))return Money(step.AmountMinor);
        }
        if(!string.IsNullOrWhiteSpace(step.BeforeUnitPriceMinor)&&!string.IsNullOrWhiteSpace(step.AfterUnitPriceMinor))
            return $"{Money(step.BeforeUnitPriceMinor)} → {Money(step.AfterUnitPriceMinor)}";
        return string.Empty;
    }

    private static string PricePolicyLabel(string? code,string? type)
    {
        if(!string.IsNullOrWhiteSpace(code))return code.Trim();
        return type?.Trim().ToUpperInvariant() switch
        {
            "BASE"=>"Giá nền","PROMOTION"=>"Khuyến mãi","CHANNEL"=>"Giá theo kênh",
            "CUSTOMER_GROUP"=>"Giá theo nhóm khách","CUSTOMER"=>"Giá theo khách hàng",_=>"Chính sách giá"
        };
    }

    private static string PricingReasonLabel(string? reason)=>reason?.Trim().ToUpperInvariant() switch
    {
        "LOWER_PRIORITY_EXCLUSIVE"=>"Đã có mức ưu tiên cao hơn được áp dụng",
        "OUTSIDE_EFFECTIVE_WINDOW"=>"Chưa đến hoặc đã qua thời gian áp dụng",
        "QUANTITY_NOT_ELIGIBLE"=>"Số lượng chưa đáp ứng điều kiện áp dụng",
        "CUSTOMER_NOT_ELIGIBLE"=>"Khách hàng chưa thuộc phạm vi áp dụng",
        "CHANNEL_NOT_ELIGIBLE"=>"Kênh bán chưa thuộc phạm vi áp dụng",
        _=>"Không áp dụng do điều kiện giá hiện tại"
    };

    public static string InventoryMovementLabel(string type,string quantity)=>type switch
    {
        "SALES_DELIVERY_ISSUE"=>"Xuất kho giao khách",
        "PURCHASE_RECEIPT"=>"Nhập hàng",
        "SUPPLIER_RETURN"=>"Xuất trả nhà cung cấp",
        "TRANSFER_ISSUE"=>"Xuất chuyển kho",
        "TRANSFER_RECEIPT"=>"Nhập chuyển kho",
        "OPENING_BALANCE"=>"Thiết lập tồn đầu kỳ",
        "MANUAL_INBOUND"=>"Nhập kho thủ công",
        "STOCKTAKE_ADJUSTMENT"=>"Cân bằng sau kiểm kê",
        "STOCKTAKE_ADJUSTMENT_REVERSAL"=>"Hoàn tác cân bằng kiểm kê",
        "LOGISTICS_TRIP_RETURN"=>"Nhập hàng hoàn",
        "REVERSAL"=>"Hoàn tác giao dịch kho",
        _ when type.StartsWith("MANUAL_ADJUSTMENT_",StringComparison.Ordinal)=>"Điều chỉnh tồn kho",
        _=>decimal.TryParse(quantity,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)&&amount>=0?"Nhập kho":"Xuất kho"
    };

    public static SalesInventoryHistoryRow InventoryHistoryRow(InventoryMovementHistoryData row,string unit)=>
        new(DateTimeText(row.PostedAt),InventoryMovementLabel(row.MovementType,row.BaseQuantityDelta),
            row.SourceDocumentNumber??row.DocumentNumber??"—",QuantityWithUnit(row.BaseQuantityDelta,unit),
            QuantityWithUnit(row.StockAfter,unit),
            string.Join(" · ",new[]{row.LocationSummary,row.LotSummary}.Where(x=>!string.IsNullOrWhiteSpace(x))),
            row.PostedByName??row.PostedBy);

    public static SalesDraftEstimateResult EstimateDraft(IReadOnlyList<SalesDraftLineRow> lines,string documentMode,string documentValue)
    {
        static bool Number(string value,out decimal result)=>decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out result);
        static decimal RoundVnd(decimal value)=>decimal.Round(value,0,MidpointRounding.AwayFromZero);
        var grossByLine=new decimal[lines.Count]; var lineDiscounts=new decimal[lines.Count]; var hasLineDiscount=false;
        for(var i=0;i<lines.Count;i++)
        {
            var line=lines[i];
            if(!Number(line.Quantity,out var quantity)||quantity<=0||!Number(FinalUnitPrice(line),out var unitPrice)||unitPrice<0)return new(0,0,0,0,false,false);
            var gross=RoundVnd(quantity*unitPrice); grossByLine[i]=gross;
            if(!Number(string.IsNullOrWhiteSpace(line.DiscountValue)?"0":line.DiscountValue,out var value)||value<0)return new(0,0,0,0,false,false);
            decimal discount=line.DiscountMode switch
            {
                "PERCENT" when value<=100m=>RoundVnd(gross*value/100m),
                "PER_UNIT"=>RoundVnd(quantity*value),
                "TOTAL_AMOUNT"=>RoundVnd(value),
                _=>-1m
            };
            if(discount<0||discount>gross)return new(0,0,0,0,false,false);
            lineDiscounts[i]=discount; if(discount>0)hasLineDiscount=true;
        }
        var grossTotal=grossByLine.Sum();
        if(!Number(string.IsNullOrWhiteSpace(documentValue)?"0":documentValue,out var docValue)||docValue<0)return new(0,0,0,0,false,false);
        var documentDiscount=documentMode switch
        {
            "NONE" when docValue==0m=>0m,
            "PERCENT" when docValue<=100m=>RoundVnd(grossTotal*docValue/100m),
            "TOTAL_AMOUNT"=>RoundVnd(docValue),
            _=>-1m
        };
        if(documentDiscount<0||documentDiscount>grossTotal)return new(grossTotal,0,0,0,false,false);
        var mixed=hasLineDiscount&&documentDiscount>0;
        if(mixed)return new(grossTotal,lineDiscounts.Sum()+documentDiscount,0,0,false,true);
        var effectiveDiscounts=documentDiscount>0?AllocateDocumentDiscount(grossByLine,documentDiscount):lineDiscounts;
        var taxTotal=0m; var total=0m;
        for(var i=0;i<lines.Count;i++)
        {
            var discounted=Math.Max(0m,grossByLine[i]-effectiveDiscounts[i]);
            if(!Number(string.IsNullOrWhiteSpace(lines[i].TaxRate)?"0":lines[i].TaxRate,out var taxRate)||taxRate<0)return new(grossTotal,effectiveDiscounts.Sum(),0,0,false,false);
            var tax=lines[i].TaxMode=="INCLUSIVE"?(taxRate==0?0:RoundVnd(discounted*taxRate/(100m+taxRate))):RoundVnd(discounted*taxRate/100m);
            taxTotal+=tax; total+=lines[i].TaxMode=="INCLUSIVE"?discounted:discounted+tax;
        }
        return new(grossTotal,effectiveDiscounts.Sum(),taxTotal,total,true,false);
    }

    private static decimal[] AllocateDocumentDiscount(IReadOnlyList<decimal> gross,decimal target)
    {
        var total=gross.Sum(); var result=new decimal[gross.Count]; if(target<=0||total<=0)return result;
        var ranked=new List<(int Index,decimal Remainder)>(); decimal allocated=0;
        for(var i=0;i<gross.Count;i++){if(gross[i]<=0)continue;var exact=gross[i]*target/total;var floor=decimal.Floor(exact);result[i]=floor;allocated+=floor;ranked.Add((i,exact-floor));}
        var remaining=Math.Max(0,decimal.ToInt32(target-allocated));
        foreach(var item in ranked.OrderByDescending(x=>x.Remainder).ThenBy(x=>x.Index)){if(remaining<=0)break;if(result[item.Index]<gross[item.Index]){result[item.Index]+=1;remaining--;}}
        return result;
    }

    public static string Quantity(string? value)=>OfficeNumberFormatting.Compact(value);

    public static string QuantityWithUnit(string? value,string? unit)
    {
        var quantity=Quantity(value);
        if(quantity=="—")return quantity;
        return string.IsNullOrWhiteSpace(unit)?quantity:$"{quantity} {unit}";
    }

    public static string Money(string? value)
    {
        if(!decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)) return value??"0";
        return amount.ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
    }

    public static string DateTimeText(string? value)=>
        DateTimeOffset.TryParse(value,out var parsed) ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "—";

    public static string Number(string? number)=>string.IsNullOrWhiteSpace(number) ? "Đơn nháp chưa cấp số" : number.TrimStart('#');

    public static string ListNumber(string? number)
    {
        if(string.IsNullOrWhiteSpace(number))return "NHÁP";
        var normalized=number.Trim().TrimStart('#');
        if(!normalized.StartsWith("SO",StringComparison.OrdinalIgnoreCase))return normalized;
        var digits=new string(normalized.Where(char.IsDigit).ToArray());
        if(digits.Length==0)return "SO";
        var suffix=digits.Length<=5?digits.PadLeft(5,'0'):digits[^5..];
        return $"SO{suffix}";
    }
}
