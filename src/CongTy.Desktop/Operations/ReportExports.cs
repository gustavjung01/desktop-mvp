using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Operations
{
internal static class ReportExportLabels
{
    private static readonly IReadOnlyDictionary<string, string> PurchaseStatuses =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["draft"] = "Nháp",
            ["pending_approval"] = "Chờ duyệt",
            ["approved"] = "Đã duyệt",
            ["confirmed"] = "Đã xác nhận",
            ["partially_received"] = "Nhận một phần",
            ["fully_received"] = "Đã nhận đủ",
            ["posted"] = "Đã ghi sổ",
            ["reversed"] = "Đã hoàn tác",
            ["cancelled"] = "Đã hủy",
            ["closed"] = "Đã đóng",
        };

    private static readonly IReadOnlyDictionary<string, string> CodStatuses =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pending"] = "Chờ thu",
            ["promised"] = "Đã hẹn thu",
            ["collected"] = "Đã thu",
            ["partially_collected"] = "Thu một phần",
            ["submitted"] = "Chờ xác nhận",
            ["reconciled"] = "Đã khớp",
            ["discrepancy"] = "Có chênh lệch",
            ["reversed"] = "Đã hoàn tác",
            ["acceptance_reversed"] = "Đã hoàn tác xác nhận",
            ["matched"] = "Đã khớp",
            ["mismatch"] = "Cần kiểm tra",
            ["unresolved"] = "Chưa xử lý",
            ["waived"] = "Không thu",
        };

    public static string PurchaseStatus(string? value) =>
        value is not null && PurchaseStatuses.TryGetValue(value, out var label) ? label : "Trạng thái khác";

    public static string PurchaseDimension(string? value) => value switch
    {
        "purchase_order" => "Đơn mua hàng",
        "goods_receipt" => "Phiếu nhận hàng",
        _ => "Nghiệp vụ mua hàng",
    };

    public static string ReceivableBucket(string? value) => value switch
    {
        "AGE_0_30" => "0–30 ngày",
        "AGE_31_60" => "31–60 ngày",
        "AGE_61_90" => "61–90 ngày",
        "AGE_91_PLUS" => "Trên 90 ngày",
        _ => "Nhóm tuổi nợ khác",
    };

    public static string PayableBucket(string? value) => value switch
    {
        "NOT_DUE" => "Chưa đến hạn",
        "OVERDUE_1_30" => "Quá hạn 1–30 ngày",
        "OVERDUE_31_60" => "Quá hạn 31–60 ngày",
        "OVERDUE_61_90" => "Quá hạn 61–90 ngày",
        "OVERDUE_91_PLUS" => "Quá hạn trên 90 ngày",
        _ => "Trạng thái hạn khác",
    };

    public static string CodMethod(string? value) => (value ?? string.Empty).ToLowerInvariant() switch
    {
        "cash" => "Tiền mặt",
        "cod" or "cash_on_delivery" => "Thu khi giao hàng",
        "bank_transfer" or "transfer" => "Chuyển khoản",
        _ => "Phương thức khác",
    };

    public static string CodStatus(string? value) =>
        value is not null && CodStatuses.TryGetValue(value, out var label) ? label : "Trạng thái khác";

    public static string LogisticsStatus(string? value) => (value ?? string.Empty).ToLowerInvariant() switch
    {
        "draft" => "Nháp",
        "planned" => "Đã lập kế hoạch",
        "ready" => "Sẵn sàng",
        "dispatched" => "Đang giao",
        "in_progress" => "Đang thực hiện",
        "completed" => "Hoàn tất",
        "closed" => "Đã đóng",
        "cancelled" => "Đã hủy",
        _ => "Trạng thái khác",
    };

    public static string DeliveryResult(string? value) => value switch
    {
        "delivered_full" => "Giao đủ",
        "delivered_partial" => "Giao một phần",
        "failed" => "Giao thất bại",
        "rescheduled" => "Hẹn giao lại",
        _ => "Kết quả khác",
    };

    public static string DeliveryReason(string? value) => value switch
    {
        "CUSTOMER_ABSENT" => "Khách vắng mặt",
        "CUSTOMER_REFUSED" => "Khách từ chối nhận",
        "ADDRESS_NOT_FOUND" => "Không tìm thấy địa chỉ",
        "WRONG_ADDRESS" => "Sai địa chỉ",
        "DAMAGED_GOODS" => "Hàng bị hư hỏng",
        "INSUFFICIENT_STOCK" => "Không đủ hàng giao",
        "DELIVERY_WINDOW_MISSED" => "Không kịp khung giờ giao",
        "PAYMENT_NOT_READY" => "Khách chưa sẵn sàng thanh toán",
        null or "" => string.Empty,
        _ => "Lý do khác",
    };

    public static string LogisticsException(string? value) => value switch
    {
        "MISSING_PLANNED_ARRIVAL" => "Thiếu giờ dự kiến tại điểm giao",
        "PENDING_DELIVERY_RESULT" => "Phiếu đã xuất chuyến nhưng chưa có kết quả giao",
        _ => "Dữ liệu cần kiểm tra",
    };

    public static string EmployeeSessionStatus(string? value) => (value ?? string.Empty).ToLowerInvariant() switch
    {
        "open" or "active" or "in_progress" => "Đang thực hiện",
        "completed" or "closed" => "Hoàn tất",
        "cancelled" => "Đã hủy",
        _ => "Trạng thái khác",
    };

    public static string EmployeeException(string? value) => value switch
    {
        "MISSING_FIELD_ACTOR_CODE" => "Thiếu thông tin nhân viên",
        "UNMAPPED_EMPLOYEE_CODE" => "Chưa liên kết hồ sơ nhân viên",
        "SESSION_COUNTER_MISMATCH" => "Số liệu phiên cần đối soát",
        _ => "Dữ liệu cần kiểm tra",
    };

    public static string Pair(string? left, string? right, string fallback = "")
    {
        var values = new[] { left?.Trim(), right?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return values.Length == 0 ? fallback : string.Join(" — ", values);
    }

    public static string Actor(string? salesLabel, string? employeeCode, string? employeeName)
    {
        if (!string.IsNullOrWhiteSpace(employeeCode))
            return $"{employeeCode.Trim()} — {(string.IsNullOrWhiteSpace(employeeName) ? salesLabel ?? "Nhân viên" : employeeName.Trim())}";
        return !string.IsNullOrWhiteSpace(salesLabel)
            ? $"{salesLabel.Trim()} — chưa liên kết hồ sơ nhân viên"
            : "Chưa xác định nhân viên";
    }

    public static string CodWarehouse(string? warehouseId, IReadOnlyList<CodWarehouseOptionData> warehouses)
    {
        if (string.IsNullOrWhiteSpace(warehouseId)) return "Tất cả kho được cấp quyền";
        var match = warehouses.FirstOrDefault(item => string.Equals(item.WarehouseId, warehouseId, StringComparison.OrdinalIgnoreCase));
        return match is null ? "Kho đang chọn" : $"{match.WarehouseCode} — {match.WarehouseName}";
    }

    public static string LogisticsWarehouse(string? warehouseId, IReadOnlyList<LogisticsReportingWarehouseData> warehouses)
    {
        if (string.IsNullOrWhiteSpace(warehouseId)) return "Tất cả kho được cấp quyền";
        var match = warehouses.FirstOrDefault(item => string.Equals(item.WarehouseId, warehouseId, StringComparison.OrdinalIgnoreCase));
        return match is null ? "Kho đang chọn" : $"{match.WarehouseCode} — {match.WarehouseName}";
    }
}
}

namespace CongTy.Desktop.Purchasing
{
    public sealed partial class PurchasingReportingViewModel
    {
        public ApiDownloadFile ExportReport()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Báo cáo mua hàng.");
            var report = _report ?? throw new InvalidOperationException("Báo cáo mua hàng chưa có dữ liệu để xuất.");

            return OfficeDataExportFile.Xlsx(
                "bao-cao-mua-hang.xlsx",
                new OfficeExportSheet("Tổng hợp", ["Nội dung", "Giá trị"],
                [
                    OfficeDataExportFile.Row("Từ ngày", report.Filters.From),
                    OfficeDataExportFile.Row("Đến ngày", report.Filters.To),
                    OfficeDataExportFile.Row("Tổng đơn mua trong kỳ", report.Summary.AllOrderCount),
                    OfficeDataExportFile.Row("Đơn mua có hiệu lực", report.Summary.EffectiveOrderCount),
                    OfficeDataExportFile.Row("Đã hủy", report.Summary.CancelledOrderCount),
                    OfficeDataExportFile.Row("Chờ duyệt", report.Summary.PendingApprovalCount),
                    OfficeDataExportFile.Row("Phiếu nhận đã ghi sổ", report.Summary.PostedReceiptCount),
                    OfficeDataExportFile.Row("Phiếu nhận đã hoàn tác", report.Summary.ReversedReceiptCount),
                ]),
                new OfficeExportSheet("Giá trị theo tiền tệ", ["Loại tiền", "Số chứng từ", "Giá trị"],
                    report.CurrencyTotals.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, row.DocumentCount, row.TotalValue)).ToArray()),
                new OfficeExportSheet("Trạng thái", ["Nhóm nghiệp vụ", "Trạng thái", "Số chứng từ"],
                    report.StatusBreakdown.Select(row => OfficeDataExportFile.Row(
                        ReportExportLabels.PurchaseDimension(row.Dimension),
                        ReportExportLabels.PurchaseStatus(row.State),
                        row.DocumentCount)).ToArray()),
                new OfficeExportSheet("Xu hướng theo ngày", ["Ngày", "Loại tiền", "Số chứng từ", "Giá trị"],
                    report.DailyTrend.Select(row => OfficeDataExportFile.Row(row.BusinessDate, row.CurrencyCode, row.DocumentCount, row.TotalValue)).ToArray()),
                new OfficeExportSheet("Nhà cung cấp", ["Mã Nhà cung cấp", "Nhà cung cấp", "Loại tiền", "Số chứng từ", "Giá trị"],
                    report.TopEntities.Select(row => OfficeDataExportFile.Row(row.EntityCode, row.EntityName, row.CurrencyCode, row.DocumentCount, row.TotalValue)).ToArray()),
                new OfficeExportSheet("Sản phẩm", ["SKU", "Sản phẩm", "Số lượng cơ sở", "Loại tiền", "Giá trị", "Chứng từ tham khảo"],
                    report.TopSkus.Select(row => OfficeDataExportFile.Row(row.Sku, row.ItemName, row.BaseQuantity, row.CurrencyCode, row.TotalValue, row.SampleDocumentNumber)).ToArray()));
        }
    }
}

namespace CongTy.Desktop.Accounting
{
    public sealed partial class AgingReportingViewModel
    {
        public ApiDownloadFile ExportReport()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Tuổi nợ.");
            var report = _report ?? throw new InvalidOperationException("Báo cáo Tuổi nợ chưa có dữ liệu để xuất.");

            return OfficeDataExportFile.Xlsx(
                "bao-cao-tuoi-no.xlsx",
                new OfficeExportSheet("Phải thu tổng hợp", ["Loại tiền", "Tuổi khoản phải thu", "Số chứng từ", "Còn phải thu"],
                    report.Receivable.Summary.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, ReportExportLabels.ReceivableBucket(row.AgeBucket), row.DocumentCount, row.RemainingAmount)).ToArray()),
                new OfficeExportSheet("Phải thu khách hàng", ["Mã khách hàng", "Khách hàng", "Loại tiền", "Số chứng từ", "Còn phải thu", "Chứng từ cũ nhất", "Tuổi lớn nhất (ngày)"],
                    report.Receivable.Customers.Select(row => OfficeDataExportFile.Row(row.CustomerCode, row.CustomerName, row.CurrencyCode, row.DocumentCount, row.RemainingAmount, row.OldestDocumentDate, row.OldestAgeDays ?? "0")).ToArray()),
                new OfficeExportSheet("Phải thu chứng từ", ["Số chứng từ", "Ngày chứng từ", "Mã khách hàng", "Khách hàng", "Kho", "Loại tiền", "Giá trị ban đầu", "Đã thu", "Còn phải thu", "Tuổi nợ (ngày)", "Nhóm tuổi nợ"],
                    report.Receivable.Documents.Select(row => OfficeDataExportFile.Row(row.SourceDocumentNumber, row.SourceDocumentDate, row.CustomerCode, row.CustomerName, row.WarehouseCode, row.CurrencyCode, row.OriginalAmount, row.AllocatedAmount, row.RemainingAmount, row.AgeDays ?? "0", ReportExportLabels.ReceivableBucket(row.AgeBucket))).ToArray()),
                new OfficeExportSheet("Phải trả tổng hợp", ["Loại tiền", "Trạng thái hạn", "Số chứng từ", "Còn phải trả"],
                    report.Payable.Summary.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, ReportExportLabels.PayableBucket(row.AgeBucket), row.DocumentCount, row.RemainingAmount)).ToArray()),
                new OfficeExportSheet("Phải trả Nhà cung cấp", ["Mã Nhà cung cấp", "Nhà cung cấp", "Loại tiền", "Số chứng từ", "Còn phải trả", "Hạn sớm nhất", "Quá hạn lớn nhất (ngày)"],
                    report.Payable.Suppliers.Select(row => OfficeDataExportFile.Row(row.SupplierCode, row.SupplierName, row.CurrencyCode, row.DocumentCount, row.RemainingAmount, row.EarliestDueDate, row.MaxOverdueDays ?? "0")).ToArray()),
                new OfficeExportSheet("Phải trả chứng từ", ["Số chứng từ", "Ngày chứng từ", "Mã Nhà cung cấp", "Nhà cung cấp", "Kho", "Hạn thanh toán", "Loại tiền", "Giá trị ban đầu", "Đã thanh toán", "Còn phải trả", "Quá hạn (ngày)", "Trạng thái hạn"],
                    report.Payable.Documents.Select(row => OfficeDataExportFile.Row(row.SourceDocumentNumber, row.SourceDocumentDate, row.SupplierCode, row.SupplierName, row.WarehouseCode, row.DueDate, row.CurrencyCode, row.OriginalAmount, row.AllocatedAmount, row.RemainingAmount, row.OverdueDays ?? "0", ReportExportLabels.PayableBucket(row.AgeBucket))).ToArray()));
        }
    }

    public sealed partial class CodAccountingViewModel
    {
        public ApiDownloadFile ExportReport()
        {
            if (!CanReadReport) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Báo cáo COD.");
            var report = _report ?? throw new InvalidOperationException("Báo cáo COD chưa có dữ liệu để xuất.");

            var exceptionRows = new List<IReadOnlyList<string>>();
            exceptionRows.AddRange(report.CurrentSnapshot.Discrepancies.Select(row => OfficeDataExportFile.Row(
                "Chênh lệch bàn giao", row.TripNumber, row.DriverCode, row.WarehouseCode, row.CurrencyCode, row.VarianceAmount, ReportExportLabels.CodStatus(row.ProjectionStatus))));
            exceptionRows.AddRange(report.Exceptions.Lifecycle.Select(row => OfficeDataExportFile.Row(
                ReportExportLabels.CodStatus(row.AnomalyType), row.SourceNumber, string.Empty,
                ReportExportLabels.CodWarehouse(row.WarehouseId, report.Warehouses), string.Empty, string.Empty,
                ReportExportLabels.CodStatus(row.ReconciliationStatus))));
            exceptionRows.AddRange(report.Exceptions.CurrencyLineage.Select(row => OfficeDataExportFile.Row(
                "Bàn giao có nhiều loại tiền", row.TripNumber, row.DriverCode, row.WarehouseCode,
                $"{row.CurrencyCount} loại tiền", string.Empty, ReportExportLabels.CodStatus(row.ProjectionStatus))));

            return OfficeDataExportFile.Xlsx(
                "bao-cao-cod.xlsx",
                new OfficeExportSheet("Thông tin báo cáo", ["Nội dung", "Giá trị"],
                [
                    OfficeDataExportFile.Row("Từ ngày", report.Filters.From),
                    OfficeDataExportFile.Row("Đến ngày", report.Filters.To),
                    OfficeDataExportFile.Row("Kho", ReportExportLabels.CodWarehouse(report.Filters.WarehouseId, report.Warehouses)),
                    OfficeDataExportFile.Row("Ghi chú", "Tiền tài xế đang giữ là số liệu hiện tại; kỳ báo cáo áp dụng cho hoạt động thu, bàn giao và kế toán tiếp nhận."),
                ]),
                new OfficeExportSheet("Tiền tài xế", ["Tài xế", "Loại tiền", "Số khoản thu", "Số tiền đang giữ", "Khoản cũ nhất", "Số ngày giữ lâu nhất"],
                    report.CurrentSnapshot.CustodyByDriver.Select(row => OfficeDataExportFile.Row($"{row.DriverCode} — {row.DriverName}", row.CurrencyCode, row.CollectionCount, row.CustodyRemainingAmount, row.OldestCollectedAt, row.OldestAgeDays)).ToArray()),
                new OfficeExportSheet("Thu trong kỳ", ["Loại tiền", "Phương thức", "Trạng thái", "Số lượt", "Phải thu", "Đã nhận"],
                    report.Activity.Collections.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, ReportExportLabels.CodMethod(row.CollectionMethod), ReportExportLabels.CodStatus(row.CollectionStatus), row.CollectionCount, row.ExpectedAmount, row.ReceivedAmount)).ToArray()),
                new OfficeExportSheet("Bàn giao", ["Loại tiền", "Số bàn giao", "Đã khai bàn giao", "Chênh lệch bàn giao"],
                    report.Activity.Handovers.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, row.HandoverCount, row.ClaimedAmount, row.HandoverDifferenceAmount)).ToArray()),
                new OfficeExportSheet("Kế toán tiếp nhận", ["Loại tiền", "Số lần tiếp nhận", "Đã tiếp nhận", "Chênh lệch"],
                    report.Activity.Acceptances.Select(row => OfficeDataExportFile.Row(row.CurrencyCode, row.AcceptanceCount, row.AcceptedAmount, row.VarianceAmount)).ToArray()),
                new OfficeExportSheet("Hẹn thu quá hạn", ["Phiếu giao", "Chuyến", "Tài xế", "Kho", "Loại tiền", "Số phải thu", "Hẹn bởi", "Hạn thu", "Quá hạn (ngày)"],
                    report.CurrentSnapshot.OverduePromises.Select(row => OfficeDataExportFile.Row(row.DeliveryOrderNumber ?? "Chưa có số phiếu", row.TripNumber, $"{row.DriverCode} — {row.DriverName}", row.WarehouseCode, row.CurrencyCode, row.ExpectedAmount, row.PromisedBy, row.DueAt, row.OverdueDays)).ToArray()),
                new OfficeExportSheet("Cần kiểm tra", ["Loại", "Nguồn", "Tài xế", "Kho", "Loại tiền", "Chênh lệch", "Trạng thái"], exceptionRows));
        }
    }
}

namespace CongTy.Desktop.Logistics
{
    public sealed partial class LogisticsReportingViewModel
    {
        public ApiDownloadFile ExportReport()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Báo cáo Giao vận.");
            var report = _report ?? throw new InvalidOperationException("Báo cáo Giao vận chưa có dữ liệu để xuất.");

            return OfficeDataExportFile.Xlsx(
                "bao-cao-giao-van.xlsx",
                new OfficeExportSheet("Tổng hợp", ["Nội dung", "Giá trị"],
                [
                    OfficeDataExportFile.Row("Từ ngày", report.Filters.From),
                    OfficeDataExportFile.Row("Đến ngày", report.Filters.To),
                    OfficeDataExportFile.Row("Kho", ReportExportLabels.LogisticsWarehouse(report.Filters.WarehouseId, report.Warehouses)),
                    OfficeDataExportFile.Row("Chuyến trong kỳ", report.Summary.TripCount ?? "0"),
                    OfficeDataExportFile.Row("Điểm giao", report.Summary.StopCount ?? "0"),
                    OfficeDataExportFile.Row("Phiếu giao", report.Summary.DeliveryOrderCount ?? "0"),
                    OfficeDataExportFile.Row("Giao đủ", report.Summary.DeliveredFullCount ?? "0"),
                    OfficeDataExportFile.Row("Giao một phần", report.Summary.DeliveredPartialCount ?? "0"),
                    OfficeDataExportFile.Row("Giao thất bại", report.Summary.FailedCount ?? "0"),
                    OfficeDataExportFile.Row("Hẹn giao lại", report.Summary.RescheduledCount ?? "0"),
                    OfficeDataExportFile.Row("Tỷ lệ giao đủ đúng giờ", report.Summary.OnTimeFullRatePercent),
                    OfficeDataExportFile.Row("Tỷ lệ có giờ giao dự kiến", report.Summary.SlaCoveragePercent),
                ]),
                new OfficeExportSheet("Tài xế", ["Tài xế", "Chuyến", "Điểm giao", "Phiếu giao", "Giao đủ", "Giao một phần", "Thất bại", "Hẹn lại", "Đúng giờ (%)", "Thời lượng chuyến đóng (phút)"],
                    report.Drivers.Select(row => OfficeDataExportFile.Row(
                        ReportExportLabels.Pair(row.DriverCode, row.DriverName, "Chưa gán"),
                        row.TripCount, row.StopCount, row.DeliveryOrderCount, row.DeliveredFullCount, row.DeliveredPartialCount, row.FailedCount, row.RescheduledCount, row.OnTimeFullRatePercent, row.AverageClosedTripDurationMinutes)).ToArray()),
                new OfficeExportSheet("Phương tiện", ["Phương tiện", "Biển số", "Loại xe", "Chuyến", "Điểm giao", "Phiếu giao", "Giao đủ", "Giao một phần", "Thất bại", "Hẹn lại", "Đúng giờ (%)"],
                    report.Vehicles.Select(row => OfficeDataExportFile.Row(row.VehicleCode ?? "Chưa gán", row.LicensePlate, row.VehicleType, row.TripCount, row.StopCount, row.DeliveryOrderCount, row.DeliveredFullCount, row.DeliveredPartialCount, row.FailedCount, row.RescheduledCount, row.OnTimeFullRatePercent)).ToArray()),
                new OfficeExportSheet("Kết quả lần giao", ["Chuyến", "Phiếu giao", "Khách hàng", "Tài xế", "Kết quả", "Lý do", "Giờ dự kiến", "Thời điểm giao", "Hẹn lại", "Đúng giờ"],
                    report.Attempts.Select(row => OfficeDataExportFile.Row(
                        row.TripNumber,
                        row.DeliveryOrderNumber ?? "Chưa có số phiếu",
                        $"{row.CustomerCodeSnapshot} — {row.CustomerNameSnapshot}",
                        $"{row.DriverCode} — {row.DriverName}",
                        ReportExportLabels.DeliveryResult(row.Result),
                        ReportExportLabels.DeliveryReason(row.ReasonCode),
                        row.PlannedArrivalAt,
                        row.AttemptedAt,
                        row.RescheduledFor,
                        row.OnTime is null ? "Chưa xác định" : row.OnTime.Value ? "Đúng giờ" : "Trễ giờ")).ToArray()),
                new OfficeExportSheet("Chuyến giao", ["Chuyến", "Kho", "Tuyến", "Tài xế", "Phương tiện", "Bắt đầu dự kiến", "Xuất chuyến", "Đóng chuyến", "Trạng thái", "Điểm giao", "Phiếu giao", "Giao đủ", "Giao một phần", "Thất bại", "Hẹn lại", "Đúng giờ (%)", "Chưa có kết quả"],
                    report.Trips.Select(row => OfficeDataExportFile.Row(
                        row.TripNumber,
                        $"{row.WarehouseCode} — {row.WarehouseName}",
                        ReportExportLabels.Pair(row.RouteCode, row.RouteName),
                        ReportExportLabels.Pair(row.DriverCode, row.DriverName),
                        ReportExportLabels.Pair(row.VehicleCode, row.LicensePlate),
                        row.PlannedStartAt, row.DispatchedAt, row.ClosedAt, ReportExportLabels.LogisticsStatus(row.Status), row.StopCount, row.DeliveryOrderCount,
                        row.DeliveredFullCount, row.DeliveredPartialCount, row.FailedCount, row.RescheduledCount, row.OnTimeFullRatePercent, row.PendingResultCount)).ToArray()),
                new OfficeExportSheet("Cần kiểm tra", ["Nội dung", "Số trường hợp"],
                    report.DataQuality.Exceptions.Select(row => OfficeDataExportFile.Row(ReportExportLabels.LogisticsException(row.ExceptionCode), row.ExceptionCount)).ToArray()));
        }
    }
}

namespace CongTy.Desktop.Settings
{
    public sealed partial class EmployeeMcpReportingViewModel
    {
        public ApiDownloadFile ExportReport()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Báo cáo Nhân viên & MCP.");
            var report = _report ?? throw new InvalidOperationException("Báo cáo Nhân viên & MCP chưa có dữ liệu để xuất.");

            var reconciliationRows = new List<IReadOnlyList<string>>();
            reconciliationRows.AddRange(report.DataQuality.UnmappedActors.Select(row => OfficeDataExportFile.Row(
                ReportExportLabels.EmployeeException(row.ExceptionCode),
                row.SalesLabel ?? "Chưa xác định nhân viên",
                $"{row.FirstSessionDate} → {row.LastSessionDate}",
                $"{row.SessionCount} phiên",
                string.Empty)));
            reconciliationRows.AddRange(report.DataQuality.CounterMismatches.Select(row => OfficeDataExportFile.Row(
                ReportExportLabels.EmployeeException(row.ExceptionCode),
                row.SalesLabel ?? "Chưa xác định nhân viên",
                $"{row.SessionDate} · {row.RouteCode ?? row.RouteName}",
                $"KH {row.StoredPlannedCustomers} · ghé {row.StoredVisitedCustomers} · nhu cầu {row.StoredOrderCount}",
                $"KH {row.DerivedPlannedOutletCount} · ghé {row.DerivedVisitedOutletCount} · nhu cầu {row.DerivedOrderIntentCount}")));

            return OfficeDataExportFile.Xlsx(
                "bao-cao-nhan-vien-mcp.xlsx",
                new OfficeExportSheet("Tổng hợp", ["Nội dung", "Giá trị"],
                [
                    OfficeDataExportFile.Row("Từ ngày", report.Filters.From),
                    OfficeDataExportFile.Row("Đến ngày", report.Filters.To),
                    OfficeDataExportFile.Row("Phiên đi thị trường", report.Summary.SessionCount ?? "0"),
                    OfficeDataExportFile.Row("Tuyến có hoạt động", report.Summary.RouteCount ?? "0"),
                    OfficeDataExportFile.Row("Điểm kế hoạch", report.Summary.PlannedOutletCount ?? "0"),
                    OfficeDataExportFile.Row("Điểm đã ghé", report.Summary.VisitedOutletCount ?? "0"),
                    OfficeDataExportFile.Row("Điểm đã ghi nhận có mặt", report.Summary.CheckedInOutletCount ?? "0"),
                    OfficeDataExportFile.Row("Nhu cầu mua", report.Summary.OrderIntentCount ?? "0"),
                    OfficeDataExportFile.Row("Đề nghị mở mã khách", report.Summary.OnboardingSubmittedCount ?? "0"),
                    OfficeDataExportFile.Row("Mở mã khách thành công", report.Summary.OnboardingConvertedCount ?? "0"),
                    OfficeDataExportFile.Row("Đơn Công Ty chính thức", report.Summary.CoreSalesOrderCount ?? "0"),
                ]),
                new OfficeExportSheet("Nhân viên", ["Nhân viên", "Phiên", "Tuyến", "Điểm kế hoạch", "Đã ghé", "Có mặt", "Lượt ghé", "Nhu cầu mua", "Đề nghị mở mã", "Mở mã thành công", "Đơn Công Ty", "Hoàn thành kế hoạch (%)", "Nhu cầu trên lượt ghé (%)", "Đơn trên nhu cầu (%)"],
                    report.FieldActors.Select(row => OfficeDataExportFile.Row(
                        ReportExportLabels.Actor(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
                        row.SessionCount, row.RouteCount, row.PlannedOutletCount, row.VisitedOutletCount, row.CheckedInOutletCount, row.VisitCount,
                        row.OrderIntentCount, row.OnboardingSubmittedCount, row.OnboardingConvertedCount, row.CoreSalesOrderCount,
                        row.PlannedVisitRatePercent, row.OrderIntentConversionPercent, row.CoreOrderConversionPercent)).ToArray()),
                new OfficeExportSheet("Tuyến", ["Mã tuyến", "Tên tuyến", "Khu vực", "Nhân viên", "Phiên", "Điểm kế hoạch", "Đã ghé", "Có mặt", "Nhu cầu mua", "Đơn Công Ty", "Hoàn thành kế hoạch (%)"],
                    report.Routes.Select(row => OfficeDataExportFile.Row(
                        row.RouteCode, row.RouteName, row.Area,
                        ReportExportLabels.Actor(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
                        row.SessionCount, row.PlannedOutletCount, row.VisitedOutletCount, row.CheckedInOutletCount, row.OrderIntentCount, row.CoreSalesOrderCount, row.PlannedVisitRatePercent)).ToArray()),
                new OfficeExportSheet("Phiên đi thị trường", ["Ngày", "Tuyến", "Khu vực", "Nhân viên", "Trạng thái", "Điểm kế hoạch", "Đã ghé", "Có mặt", "Lượt ghé", "Nhu cầu mua", "Đề nghị mở mã", "Mở mã thành công", "Đơn Công Ty"],
                    report.Sessions.Select(row => OfficeDataExportFile.Row(
                        row.SessionDate,
                        ReportExportLabels.Pair(row.RouteCode, row.RouteName),
                        row.Area,
                        ReportExportLabels.Actor(row.SalesLabel, row.EmployeeCode, row.EmployeeName),
                        ReportExportLabels.EmployeeSessionStatus(row.Status),
                        row.PlannedOutletCount, row.VisitedOutletCount, row.CheckedInOutletCount, row.VisitCount, row.OrderIntentCount,
                        row.OnboardingSubmittedCount, row.OnboardingConvertedCount, row.CoreSalesOrderCount)).ToArray()),
                new OfficeExportSheet("Cần đối soát", ["Loại", "Nhân viên", "Khoảng ngày / tuyến", "Số liệu ghi nhận", "Số liệu đối chiếu"], reconciliationRows));
        }
    }
}
