using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static class OrderManagementPresentation
{
    public static readonly IReadOnlyList<SalesLookupOption> StageOptions=
    [new("all","Tất cả"),new("active","Đang xử lý"),new("preparing","Đang chuẩn bị"),new("waiting_delivery","Chờ giao"),new("completed","Đã hoàn thành"),new("cancelled","Đã hủy")];

    public static readonly IReadOnlyList<SalesLookupOption> PaymentOptions=
    [new("all","Tất cả"),new("unpaid","Chưa thu"),new("partial","Thu một phần"),new("paid","Đã thu"),new("other","Đã xử lý khác")];

    public static readonly IReadOnlyList<SalesLookupOption> LaneOptions=
    [new("all","Tất cả"),new("counter","Tại quầy"),new("manual","Giao thủ công"),new("trip","Giao theo chuyến")];

    public static readonly IReadOnlyList<SalesLookupOption> SourceOptions=
    [new("all","Tất cả"),new("internal","Công Ty"),new("mcp","Nhân viên thị trường"),new("customer","Khách đặt hàng")];

    public static readonly IReadOnlyList<SalesLookupOption> PageSizeOptions=
    [new("20","20 dòng"),new("50","50 dòng"),new("100","100 dòng")];

    public static string WorkStage(SalesOrderData order)
    {
        if(order.Status=="cancelled"||order.DeliveryStatus=="cancelled")return "cancelled";
        if(order.Status=="closed"||order.DeliveryStatus=="delivered")return "completed";
        if(order.DeliveryStatus=="returned")return "active";
        if(order.DeliveryStatus is "ready_to_dispatch" or "dispatched" or "partially_delivered" or "failed" or "rescheduled"||order.FulfillmentStatus=="issued")return "waiting_delivery";
        if(order.Status=="confirmed"&&order.FulfillmentStatus is "reserved" or "partially_allocated" or "allocated" or "partially_fulfilled" or "fulfilled")return "preparing";
        return "active";
    }

    public static string StageLabel(string stage)=>stage switch
    {
        "active"=>"Đang xử lý",
        "preparing"=>"Đang chuẩn bị",
        "waiting_delivery"=>"Chờ giao",
        "completed"=>"Đã hoàn thành",
        "cancelled"=>"Đã hủy",
        _=>"Đang xử lý"
    };

    public static string DeliveryLane(SalesOrderData order)=>order.DeliveryMode=="PICKUP"
        ? "counter"
        : order.DeliveryExecutionMode=="MANUAL"?"manual":"trip";

    public static string LaneLabel(string lane)=>lane switch
    {
        "counter"=>"Tại quầy",
        "manual"=>"Giao thủ công",
        _=>"Giao theo chuyến"
    };

    public static string SourceBucket(SalesOrderData order)=>order.SourceType=="MCP"
        ? "mcp"
        : order.SourceType=="API"&&order.SourceId?.StartsWith("CUSTOMER_PORTAL:",StringComparison.Ordinal)==true
            ? "customer"
            : "internal";

    public static string SourceLabel(string source)=>source switch
    {
        "mcp"=>"Nhân viên thị trường",
        "customer"=>"Khách đặt hàng",
        _=>"Công Ty"
    };

    public static string PaymentBucket(SalesOrderData order)=>order.SettlementStatus switch
    {
        "partially_paid"=>"partial",
        "paid" or "overpaid"=>"paid",
        "refunded" or "written_off"=>"other",
        _=>"unpaid"
    };

    public static string PaymentLabel(string payment)=>payment switch
    {
        "partial"=>"Thu một phần",
        "paid"=>"Đã thu",
        "other"=>"Đã xử lý khác",
        _=>"Chưa thu"
    };

    public static string OrderStatusLabel(string status)=>status switch
    {
        "draft"=>"Nháp",
        "confirmed"=>"Đã xác nhận",
        "closed"=>"Đã hoàn thành",
        "cancelled"=>"Đã hủy",
        _=>string.IsNullOrWhiteSpace(status)?"—":status
    };

    public static string FulfillmentLabel(string status)=>status switch
    {
        "unallocated"=>"Chưa chuẩn bị",
        "backordered"=>"Chờ hàng",
        "partially_reserved"=>"Giữ một phần",
        "reserved"=>"Đã giữ hàng",
        "partially_allocated"=>"Phân bổ một phần",
        "allocated"=>"Đã phân bổ",
        "partially_fulfilled"=>"Xuất một phần",
        "fulfilled" or "issued"=>"Đã xuất kho",
        "cancelled"=>"Đã hủy",
        _=>string.IsNullOrWhiteSpace(status)?"—":status
    };

    public static string DeliveryLabel(SalesOrderData order)=>order.DeliveryStatus switch
    {
        "pending"=>"Chưa giao",
        "ready_to_dispatch"=>"Chờ xuất phát",
        "dispatched"=>"Đang giao",
        "partially_delivered"=>"Giao một phần",
        "delivered"=>"Đã giao",
        "failed"=>"Giao chưa thành công",
        "rescheduled"=>"Đã hẹn lại",
        "returned"=>"Có hàng trả về",
        "cancelled"=>"Đã hủy",
        _=>order.DeliveryMode=="PICKUP"?"Tại quầy":"Chưa giao"
    };

    public static string CompactNumber(string? number)
    {
        var value=(number??string.Empty).Trim().TrimStart('#');
        if(value.Length==0)return "—";
        var parts=value.Split('-',StringSplitOptions.RemoveEmptyEntries);
        return parts.Length==3&&parts[0].Equals("SO",StringComparison.OrdinalIgnoreCase)
            ? $"SO{parts[2]}"
            : value;
    }

    public static string CreatedAt(SalesOrderData order)
    {
        if(!DateTimeOffset.TryParse(order.CreatedAt,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var parsed))return "—";
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm",CultureInfo.GetCultureInfo("vi-VN"));
    }

    public static DateTimeOffset? CreatedAtValue(SalesOrderData order)=>
        DateTimeOffset.TryParse(order.CreatedAt,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var parsed)?parsed:null;

    public static string Customer(SalesOrderData order)=>order.CustomerMode=="WALK_IN"
        ? string.IsNullOrWhiteSpace(order.WalkInDisplayName)?"Khách vãng lai":order.WalkInDisplayName.Trim()
        : string.IsNullOrWhiteSpace(order.CustomerName)?"—":order.CustomerName.Trim();

    public static string Total(SalesOrderData order)=>SalesPresentation.Money(order.Total);

    public static bool IsPrintable(SalesOrderData order)=>!string.IsNullOrWhiteSpace(order.Number)&&order.Status is "confirmed" or "closed";

    public static SalesOrderVersionData? ActiveVersion(SalesOrderData order)=>SalesPresentation.ActiveVersion(order);
}
