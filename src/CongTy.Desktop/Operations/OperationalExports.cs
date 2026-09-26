using System.Globalization;
using CongTy.ApiClient;

namespace CongTy.Desktop.Operations
{
    internal static class OperationalExportBuild
    {
        public static ApiDownloadFile SingleSheet(
            string format,
            string baseFileName,
            string sheetName,
            IReadOnlyList<string> headers,
            IReadOnlyList<IReadOnlyList<string>> rows,
            bool allowCsv)
        {
            if (rows.Count == 0) throw new InvalidOperationException("Không có dữ liệu phù hợp bộ lọc để xuất.");
            if (allowCsv && string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
                return OfficeDataExportFile.Csv(baseFileName.Replace(".xlsx", ".csv", StringComparison.OrdinalIgnoreCase), headers, rows);
            return OfficeDataExportFile.Xlsx(baseFileName, new OfficeExportSheet(sheetName, headers, rows));
        }

        public static string Party(string? code, string? name)
        {
            var values = new[] { code?.Trim(), name?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return values.Length == 0 ? string.Empty : string.Join(" — ", values);
        }

        public static string DeliveryReason(string? code) => code switch
        {
            "CUSTOMER_CLOSED" => "Khách đóng cửa",
            "CUSTOMER_REFUSED" => "Khách từ chối nhận",
            "ADDRESS_ISSUE" => "Không xác định được địa chỉ",
            null or "" => string.Empty,
            _ => "Lý do khác"
        };
    }
}

namespace CongTy.Desktop.Sales
{
    public sealed partial class OrderManagementViewModel
    {
        public ApiDownloadFile ExportOperational(string format)
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Đơn bán hàng.");
            var rows = _filtered.Select(order => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                order.Number,
                OrderManagementPresentation.CreatedAt(order),
                order.CustomerMode == "WALK_IN" ? string.Empty : order.CustomerCode,
                OrderManagementPresentation.Customer(order),
                OrderManagementPresentation.OrderStatusLabel(order.Status),
                OrderManagementPresentation.PaymentLabel(OrderManagementPresentation.PaymentBucket(order)),
                order.Total ?? "0",
                OrderManagementPresentation.FulfillmentLabel(order.FulfillmentStatus),
                OrderManagementPresentation.LaneLabel(OrderManagementPresentation.DeliveryLane(order)),
                OrderManagementPresentation.DeliveryLabel(order),
                OrderManagementPresentation.SourceLabel(OrderManagementPresentation.SourceBucket(order)))).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                format,
                "don-ban-hang-theo-bo-loc.xlsx",
                "Đơn bán hàng",
                ["Số đơn", "Ngày tạo", "Mã khách hàng", "Khách hàng", "Trạng thái đơn", "Thanh toán", "Giá trị đơn", "Chuẩn bị hàng", "Luồng giao", "Trạng thái giao", "Nguồn đơn"],
                rows,
                allowCsv: true);
        }
    }
}

namespace CongTy.Desktop.Purchasing
{
    public sealed partial class GoodsReceiptViewModel
    {
        public ApiDownloadFile ExportOperational(string format)
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Phiếu nhận hàng.");
            var rows = Rows.Select(row => row.Data).Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.DocumentNumber,
                item.PurchaseOrderNumber,
                item.SupplierCode,
                item.SupplierName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(item.WarehouseCode, item.WarehouseName),
                GoodsReceiptPresentation.Date(item.ReceiptDate),
                GoodsReceiptPresentation.Status(item.Status),
                item.LineCount.ToString(CultureInfo.InvariantCulture),
                item.ReceivedQuantityTotal,
                item.AcceptedQuantityTotal,
                item.RejectedQuantityTotal,
                item.SupplierDeliveryReference)).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                format,
                "phieu-nhan-hang-theo-bo-loc.xlsx",
                "Phiếu nhận hàng",
                ["Số phiếu", "Đơn mua hàng", "Mã Nhà cung cấp", "Nhà cung cấp", "Kho nhận", "Ngày nhận", "Trạng thái", "Số dòng", "Tổng thực nhận", "Tổng chấp nhận", "Tổng loại", "Tham chiếu giao"],
                rows,
                allowCsv: true);
        }
    }

    public sealed partial class SupplierReturnViewModel
    {
        public ApiDownloadFile ExportOperational(string format)
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Trả hàng Nhà cung cấp.");
            var rows = Rows.Select(row => row.Data).Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.DocumentNumber,
                item.SupplierCode,
                item.SupplierName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(item.WarehouseCode, item.WarehouseName),
                SupplierReturnPresentation.Date(item.ReturnDate),
                SupplierReturnPresentation.Status(item.Status),
                item.LineCount.ToString(CultureInfo.InvariantCulture),
                item.ReturnQuantityTotal,
                item.Note)).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                format,
                "tra-hang-nha-cung-cap-theo-bo-loc.xlsx",
                "Trả Nhà cung cấp",
                ["Số phiếu", "Mã Nhà cung cấp", "Nhà cung cấp", "Kho", "Ngày trả", "Trạng thái", "Số dòng", "Tổng số lượng", "Ghi chú"],
                rows,
                allowCsv: true);
        }
    }

    public sealed partial class PurchasePriceViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Giá mua Nhà cung cấp.");
            var rows = Rows.Select(row => row.Data).Select(price => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                price.SupplierCode,
                price.SupplierName,
                price.Sku,
                price.ProductName,
                price.UnitCode,
                price.UnitPrice,
                price.CurrencyCode,
                price.MinQuantity,
                PurchasePricePresentation.Date(price.EffectiveFrom),
                string.IsNullOrWhiteSpace(price.EffectiveTo) ? string.Empty : PurchasePricePresentation.Date(price.EffectiveTo),
                price.SupplierSku,
                price.SourceReference,
                price.IsActive ? "Đang dùng" : "Ngừng dùng")).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                "xlsx",
                "gia-mua-nha-cung-cap-theo-bo-loc.xlsx",
                "Giá mua Nhà cung cấp",
                ["Mã Nhà cung cấp", "Nhà cung cấp", "SKU", "Sản phẩm", "Đơn vị", "Giá mua", "Tiền tệ", "Từ số lượng", "Hiệu lực từ", "Hiệu lực đến", "Mã hàng NCC", "Tham chiếu nguồn", "Trạng thái"],
                rows,
                allowCsv: false);
        }
    }
}

namespace CongTy.Desktop.Logistics
{
    public sealed partial class CustomerReturnViewModel
    {
        public ApiDownloadFile ExportOperational(string format)
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Hàng khách trả.");
            var rows = Returns.Select(row => row.Data).Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.Number ?? "Phiếu nháp",
                item.CustomerCode,
                item.CustomerName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(item.WarehouseCode, item.WarehouseName),
                CustomerReturnPresentation.Status(item.Status),
                (item.LineCount ?? 0).ToString(CultureInfo.InvariantCulture),
                item.RequestedBaseQuantity ?? "0",
                item.AcceptedBaseQuantity ?? "0",
                item.Note)).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                format,
                "hang-khach-tra.xlsx",
                "Hàng khách trả",
                ["Số phiếu", "Mã khách hàng", "Khách hàng", "Kho nhận", "Trạng thái", "Số dòng", "Số lượng đề nghị", "Số lượng thực nhận", "Ghi chú"],
                rows,
                allowCsv: true);
        }
    }

    public sealed partial class DeliveryAttemptViewModel
    {
        public ApiDownloadFile ExportSelectedTrip()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Kết quả giao hàng.");
            if (SelectedTripRow is null) throw new InvalidOperationException("Chọn chuyến giao cần xuất kết quả.");
            var tripNumber = SelectedTripRow.Number;
            var rows = Attempts.Select(row => row.Data).Select(attempt => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                tripNumber,
                attempt.StopSequence.ToString(CultureInfo.InvariantCulture),
                attempt.DeliveryOrderNumber,
                attempt.CustomerCode,
                attempt.CustomerName,
                DeliveryAttemptPresentation.Result(attempt.Result),
                DeliveryAttemptPresentation.DateTimeText(attempt.AttemptedAt),
                CongTy.Desktop.Operations.OperationalExportBuild.DeliveryReason(attempt.ReasonCode),
                string.IsNullOrWhiteSpace(attempt.RescheduledFor) ? string.Empty : DeliveryAttemptPresentation.DateTimeText(attempt.RescheduledFor),
                attempt.Note)).ToArray();

            var safeTrip = string.Concat(tripNumber.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                "xlsx",
                $"ket-qua-giao-{safeTrip}.xlsx",
                "Kết quả giao",
                ["Chuyến", "Điểm", "Phiếu giao", "Mã khách hàng", "Khách hàng", "Kết quả", "Thời điểm", "Lý do", "Hẹn giao lại", "Ghi chú"],
                rows,
                allowCsv: false);
        }
    }
}

namespace CongTy.Desktop.Accounting
{
    public sealed partial class CustomerPaymentViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Thu tiền khách hàng.");
            var rows = _payments.Select(payment => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                payment.DocumentNumber,
                CustomerPaymentPresentation.Date(payment.PaymentDate),
                payment.CustomerCode,
                payment.CustomerName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(payment.RemittingEmployeeCode, payment.RemittingEmployeeName),
                string.Join(", ", payment.RelatedSalesOrderNumbers),
                payment.OriginalAmount,
                payment.AllocatedAmount,
                payment.RemainingAmount,
                payment.RelatedRemainingAmount,
                CustomerPaymentPresentation.Status(payment.Status),
                CustomerPaymentPresentation.PaymentMethod(payment.PaymentMethod),
                payment.ExternalReference)).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                "xlsx",
                "lich-su-thu-tien-khach-hang.xlsx",
                "Thu tiền khách hàng",
                ["Số phiếu", "Ngày thu", "Mã khách hàng", "Khách hàng", "Nhân viên nộp", "Đơn hàng liên quan", "Số tiền thu", "Đã ghi vào đơn", "Tiền chưa gắn với đơn", "Còn phải thu liên quan", "Trạng thái", "Phương thức", "Tham chiếu"],
                rows,
                allowCsv: false);
        }
    }

    public sealed partial class SupplierPaymentViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Thanh toán Nhà cung cấp.");
            var rows = _payments.Select(payment => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                payment.DocumentNumber,
                SupplierPaymentPresentation.Date(payment.PaymentDate),
                payment.SupplierCode,
                payment.SupplierName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(payment.WarehouseCode, payment.WarehouseName),
                payment.OriginalAmount,
                payment.AllocatedAmount,
                payment.RemainingAmount,
                SupplierPaymentPresentation.Status(payment.Status),
                SupplierPaymentPresentation.PaymentMethod(payment.PaymentMethod),
                payment.ExternalReference,
                payment.Note)).ToArray();

            return CongTy.Desktop.Operations.OperationalExportBuild.SingleSheet(
                "xlsx",
                "lich-su-thanh-toan-nha-cung-cap.xlsx",
                "Thanh toán Nhà cung cấp",
                ["Số phiếu", "Ngày thanh toán", "Mã Nhà cung cấp", "Nhà cung cấp", "Kho", "Số tiền", "Đã phân bổ", "Chưa phân bổ", "Trạng thái", "Phương thức", "Tham chiếu", "Ghi chú"],
                rows,
                allowCsv: false);
        }
    }

    public sealed partial class CustomerReturnCreditViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Khoản giảm công nợ hàng trả.");
            if (_credits.Length == 0) throw new InvalidOperationException("Không có dữ liệu phù hợp để xuất.");

            var credits = _credits.Select(credit => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                credit.ReturnNumber,
                credit.DocumentNumber,
                credit.CustomerCode,
                credit.CustomerName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(credit.WarehouseCode, credit.WarehouseName),
                credit.OriginalAmount,
                credit.AllocatedAmount,
                credit.RemainingAmount,
                CustomerReturnCreditPresentation.Status(credit.Status))).ToArray();

            var refunds = _credits.SelectMany(credit => credit.Refunds.Select(refund => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                refund.RefundNumber,
                credit.ReturnNumber,
                refund.CustomerCode,
                refund.CustomerName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(refund.WarehouseCode, refund.WarehouseName),
                refund.Amount,
                CustomerReturnCreditPresentation.RefundMethod(refund.RefundMethod),
                refund.DestinationReference,
                refund.ExternalReference,
                refund.Reason,
                string.IsNullOrWhiteSpace(refund.ReversalId) ? "Đã hoàn" : "Đã đảo"))).ToArray();

            return CongTy.Desktop.Operations.OfficeDataExportFile.Xlsx(
                "giam-cong-no-va-hoan-tien-khach.xlsx",
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Giảm công nợ",
                    ["Phiếu trả", "Số chứng từ", "Mã khách hàng", "Khách hàng", "Kho", "Giá trị ban đầu", "Đã sử dụng", "Còn sử dụng", "Trạng thái"],
                    credits),
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Hoàn tiền",
                    ["Phiếu hoàn", "Phiếu trả", "Mã khách hàng", "Khách hàng", "Kho", "Số tiền", "Phương thức", "Nơi nhận", "Tham chiếu", "Lý do", "Trạng thái"],
                    refunds));
        }
    }

    public sealed partial class ReceivablesViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Công nợ khách hàng.");
            if (_balances.Length == 0 && _documents.Length == 0) throw new InvalidOperationException("Không có dữ liệu công nợ phù hợp để xuất.");

            var balances = _balances.Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.CustomerCode,
                item.CustomerName,
                item.CurrencyCode,
                item.Balance,
                item.OpenAmount,
                item.OpenDocumentCount.ToString(CultureInfo.InvariantCulture),
                ReceivablesPresentation.DateTimeText(item.UpdatedAt))).ToArray();

            var documents = _documents.Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.SourceDocumentNumber,
                ReceivablesPresentation.Date(item.SourceDocumentDate),
                item.CustomerCode,
                item.CustomerName,
                item.SalesOrderNumber,
                item.DeliveryOrderNumber,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(item.WarehouseCode, item.WarehouseName),
                ReceivablesPresentation.CollectionPolicy(item.CollectionPolicy),
                item.CurrencyCode,
                item.OriginalAmount,
                item.AllocatedAmount,
                item.RemainingAmount,
                ReceivablesPresentation.Status(item.Status))).ToArray();

            return CongTy.Desktop.Operations.OfficeDataExportFile.Xlsx(
                "cong-no-phai-thu.xlsx",
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Số dư phải thu",
                    ["Mã khách hàng", "Khách hàng", "Tiền tệ", "Số dư", "Còn mở", "Số chứng từ", "Cập nhật"],
                    balances),
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Chứng từ phải thu",
                    ["Chứng từ nguồn", "Ngày", "Mã khách hàng", "Khách hàng", "Đơn bán", "Phiếu giao", "Kho", "Chính sách thu", "Tiền tệ", "Giá trị", "Đã thu", "Còn phải thu", "Trạng thái"],
                    documents));
        }
    }

    public sealed partial class PayablesViewModel
    {
        public ApiDownloadFile ExportOperational()
        {
            if (!CanRead) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Công nợ Nhà cung cấp.");
            if (_balances.Length == 0 && _documents.Length == 0) throw new InvalidOperationException("Không có dữ liệu công nợ phù hợp để xuất.");

            var balances = _balances.Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.SupplierCode,
                item.SupplierName,
                item.CurrencyCode,
                item.Balance,
                item.OpenAmount,
                item.OverdueAmount,
                item.OpenDocumentCount.ToString(CultureInfo.InvariantCulture),
                PayablesPresentation.DateTimeText(item.UpdatedAt))).ToArray();

            var documents = _documents.Select(item => CongTy.Desktop.Operations.OfficeDataExportFile.Row(
                item.SourceDocumentNumber,
                PayablesPresentation.Date(item.SourceDocumentDate),
                item.SupplierCode,
                item.SupplierName,
                CongTy.Desktop.Operations.OperationalExportBuild.Party(item.WarehouseCode, item.WarehouseName),
                PayablesPresentation.Date(item.DueDate),
                item.PaymentTermDays.ToString(CultureInfo.InvariantCulture),
                item.CurrencyCode,
                item.SignedOriginalAmount,
                item.AllocatedAmount,
                item.RemainingAmount,
                PayablesPresentation.Status(item.Status))).ToArray();

            return CongTy.Desktop.Operations.OfficeDataExportFile.Xlsx(
                "cong-no-phai-tra.xlsx",
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Số dư phải trả",
                    ["Mã Nhà cung cấp", "Nhà cung cấp", "Tiền tệ", "Số dư", "Còn mở", "Quá hạn", "Số chứng từ", "Cập nhật"],
                    balances),
                new CongTy.Desktop.Operations.OfficeExportSheet(
                    "Chứng từ phải trả",
                    ["Chứng từ nguồn", "Ngày", "Mã Nhà cung cấp", "Nhà cung cấp", "Kho", "Hạn thanh toán", "Điều khoản ngày", "Tiền tệ", "Giá trị", "Đã phân bổ", "Còn phải trả", "Trạng thái"],
                    documents));
        }
    }
}
