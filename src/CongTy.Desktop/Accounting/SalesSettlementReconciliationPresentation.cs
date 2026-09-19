using System.Globalization;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed record SalesSettlementStatusOption(string Key, string Label);

public sealed record SalesSettlementCustomerRow(
    string CustomerWarehouse,
    string Posted,
    string Outstanding,
    string Credit,
    string Calculated,
    string Ledger,
    string Result,
    SalesSettlementCustomerData Data);

public sealed record SalesSettlementOrderRow(
    string OrderCustomer,
    string OrderStatus,
    string FulfillmentStatus,
    string DeliveryStatus,
    string SettlementStatus,
    string Outstanding,
    string CodCustody,
    string Result,
    SalesSettlementOrderData Data);

public sealed record SalesSettlementDocumentRow(
    string DocumentCustomer,
    string Type,
    string Original,
    string Allocated,
    string Remaining,
    string Ledger,
    string Result,
    SalesSettlementDocumentData Data);

public sealed record SalesSettlementCollectionRow(
    string DeliveryCustomer,
    string TripDriver,
    string Method,
    string Received,
    string HandedOver,
    string Custody,
    string Lifecycle,
    SalesSettlementCodCollectionData Data);

public sealed record SalesSettlementHandoverRow(
    string TripDriver,
    string Claimed,
    string Pending,
    string Accepted,
    string Variance,
    string Status,
    string Result,
    SalesSettlementCodHandoverData Data);

public sealed record SalesSettlementAnomalyRow(
    string Type,
    string Source,
    string Result,
    string Details,
    string Destination,
    SalesSettlementAnomalyData Data);

public static class SalesSettlementReconciliationPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static IReadOnlyList<SalesSettlementStatusOption> StatusOptions { get; } =
    [
        new("all", "Tất cả"),
        new("matched", "Chỉ số khớp"),
        new("mismatch", "Chỉ số lệch")
    ];

    public static string Status(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "matched" => "Khớp",
            "mismatch" => "Lệch",
            "draft" => "Nháp",
            "confirmed" => "Đã xác nhận",
            "cancelled" => "Đã hủy",
            "closed" => "Đã đóng",
            "unallocated" => "Chưa giữ hàng",
            "partially_allocated" => "Giữ một phần",
            "allocated" => "Đã giữ hàng",
            "partially_fulfilled" => "Hoàn tất một phần",
            "fulfilled" => "Đã hoàn tất",
            "not_required" => "Không cần giao",
            "pending" => "Đang chờ",
            "ready_to_dispatch" => "Sẵn sàng giao",
            "dispatched" => "Đang giao",
            "partially_delivered" => "Giao một phần",
            "delivered" => "Đã giao",
            "failed" => "Giao thất bại",
            "rescheduled" => "Hẹn giao lại",
            "returned" => "Đã trả hàng",
            "not_due" => "Chưa phát sinh nợ",
            "partially_paid" => "Đã trả một phần",
            "paid" => "Đã thanh toán",
            "overpaid" => "Trả thừa",
            "refunded" => "Đã hoàn tiền",
            "written_off" => "Đã xóa nợ",
            "driver_custody" => "Tài xế đang giữ",
            "handed_over" => "Đã bàn giao",
            "settled_non_cash" => "Đã thu chuyển khoản",
            "not_collected" => "Chưa thu",
            "reversed" => "Đã đảo",
            "submitted" => "Chờ xác nhận",
            "acceptance_reversed" => "Đã đảo xác nhận",
            "reconciled" => "Đã khớp",
            "discrepancy" => "Có chênh lệch",
            _ => Humanize(normalized)
        };
    }

    public static string Money(string? value, string? currencyCode = "VND") =>
        AgingReportingPresentation.Money(value, currencyCode);

    public static string Number(string? value) =>
        AgingReportingPresentation.Number(value);

    public static string Date(string? value) =>
        AgingReportingPresentation.Date(value);

    public static string DateTimeText(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese)
            : string.IsNullOrWhiteSpace(value) ? "Chưa ghi nhận" : value.Trim();

    public static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(
            value?.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;

    public static string ActiveFilterText(DateTime? from, DateTime? to, string? search, string? status)
    {
        var parts = new List<string>();
        if (from is not null) parts.Add($"từ {from.Value:dd/MM/yyyy}");
        if (to is not null) parts.Add($"đến {to.Value:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(search)) parts.Add($"“{search.Trim()}”");
        if (!string.IsNullOrWhiteSpace(status) && status != "all") parts.Add(Status(status));
        return parts.Count == 0
            ? "Toàn bộ dữ liệu trong phạm vi kho được cấp"
            : string.Join(" · ", parts);
    }

    public static SalesSettlementCustomerRow Customer(SalesSettlementCustomerData row) =>
        new(
            $"{row.CustomerCode} · {row.CustomerName}\n{row.WarehouseCode} · {Date(row.LatestDocumentDate)}",
            Money(row.DebitPostedAmount, row.CurrencyCode),
            Money(row.DebitOutstandingAmount, row.CurrencyCode),
            Money(row.UnappliedCreditAmount, row.CurrencyCode),
            Money(row.CalculatedOpenBalance, row.CurrencyCode),
            Money(row.LedgerBalance, row.CurrencyCode),
            Status(row.ReconciliationStatus),
            row);

    public static SalesSettlementOrderRow Order(SalesSettlementOrderData row) =>
        new(
            $"{DisplayOrderNumber(row)}\n{row.CustomerCode} · {row.CustomerName} · {row.WarehouseCode}",
            Status(row.OrderStatus),
            Status(row.FulfillmentStatus),
            Status(row.DeliveryStatus),
            $"{Status(row.SettlementStatus)}\nTính lại: {Status(row.CalculatedSettlementStatus)}",
            Money(row.ReceivableRemainingAmount, row.CurrencyCode),
            Money(row.CodCustodyAmount, row.CurrencyCode),
            Status(row.ReconciliationStatus),
            row);

    public static SalesSettlementDocumentRow Document(SalesSettlementDocumentData row) =>
        new(
            $"{row.SourceDocumentNumber}\n{Date(row.SourceDocumentDate)} · {row.CustomerCodeSnapshot} · {row.CustomerNameSnapshot}",
            $"{Status(row.DocumentType)}\n{row.Direction} · {Status(row.DocumentStatus)}",
            Money(row.OriginalAmount, row.CurrencyCode),
            Money(row.ProjectedAllocatedAmount, row.CurrencyCode),
            Money(row.ProjectedRemainingAmount, row.CurrencyCode),
            $"{Money(row.LedgerAmount, row.CurrencyCode)}\nKỳ vọng {Money(row.ExpectedLedgerAmount, row.CurrencyCode)}",
            Status(row.ReconciliationStatus),
            row);

    public static SalesSettlementCollectionRow Collection(SalesSettlementCodCollectionData row) =>
        new(
            $"{DisplayDeliveryOrderNumber(row)}\n{row.CustomerCode} · {row.CustomerName}",
            $"{row.TripNumber}\n{row.DriverCode} · {row.DriverName}",
            $"{Status(row.CollectionMethod)}\n{Status(row.CollectionStatus)}",
            Money(row.ReceivedAmount, row.CurrencyCode),
            Money(row.HandedOverAmount, row.CurrencyCode),
            Money(row.CustodyRemainingAmount, row.CurrencyCode),
            $"{Status(row.LifecycleStatus)}\n{(row.LifecycleMatches ? "Khớp" : "Lệch")}",
            row);

    public static SalesSettlementHandoverRow Handover(SalesSettlementCodHandoverData row) =>
        new(
            $"{row.TripNumber}\n{row.DriverCode} · {row.DriverName}\n{DateTimeText(row.HandedOverAt)}",
            Money(row.ClaimedAmount),
            Money(row.PendingAcceptanceAmount),
            Money(row.AcceptedAmount),
            Money(row.VarianceAmount),
            Status(row.ProjectionStatus),
            row.LifecycleMatches ? "Khớp" : "Lệch",
            row);

    public static SalesSettlementAnomalyRow Anomaly(SalesSettlementAnomalyData row) =>
        new(
            Status(row.AnomalyType),
            row.SourceNumber,
            Status(row.ReconciliationStatus),
            JsonSerializer.Serialize(row.Details, IndentedJson),
            AnomalyDestination(row.AnomalyType),
            row);

    public static string DisplayOrderNumber(SalesSettlementOrderData row) =>
        string.IsNullOrWhiteSpace(row.OrderNumber)
            ? ShortId(row.SalesOrderId)
            : row.OrderNumber.Trim();

    public static string DisplayDeliveryOrderNumber(SalesSettlementCodCollectionData row) =>
        string.IsNullOrWhiteSpace(row.DeliveryOrderNumber)
            ? ShortId(row.CollectionId)
            : row.DeliveryOrderNumber.Trim();

    public static string AnomalyDestination(string? anomalyType) =>
        anomalyType switch
        {
            "sales_order_status" => "sales-orders",
            "cod_collection" or "cod_handover" => "cod",
            _ => "receivables"
        };

    private static string ShortId(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= 8 ? normalized : normalized[..8];
    }

    private static string Humanize(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "—"
            : value.Replace('_', ' ').Trim();
}
