using System.Globalization;
using System.Text;
using CongTy.Contracts;
using System.Windows.Media.Imaging;

namespace CongTy.Desktop.Partners;

public sealed record PartnerLookupOption(string Id, string Label);

public sealed record CustomerRow(
    string Id,
    int Sequence,
    string Code,
    string Name,
    string Group,
    string ResponsibleEmployee,
    string Contact,
    string Payment,
    string Status,
    string UpdatedAt,
    CustomerData Source)
{
    public string TaxCode => string.IsNullOrWhiteSpace(Source.TaxCode) ? "Chưa có mã số thuế" : Source.TaxCode!;
    public string Phone => string.IsNullOrWhiteSpace(Source.Phone) ? "—" : Source.Phone!;
    public string Email => string.IsNullOrWhiteSpace(Source.Email) ? "—" : Source.Email!;
    public string CreditLimit => PartnerPresentation.FormatMoney(Source.CreditLimit);
    public string PaymentTerms => $"Thời hạn thanh toán: {Source.PaymentTermsDays} ngày · Hạn mức tín dụng";
    public bool IsActive => Source.IsActive;
    public string ToggleAction => Source.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng";
}

public sealed record CustomerGroupRow(
    string Id,
    int Sequence,
    string Code,
    string Name,
    string Description,
    string Status,
    string UpdatedAt,
    CustomerGroupData Source)
{
    public bool IsActive => Source.IsActive;
    public string ToggleAction => Source.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng";
}

public sealed record CustomerAddressRow(
    string Id,
    string Label,
    string Recipient,
    string Address,
    string Phone,
    string Location,
    string DefaultText,
    string Status,
    CustomerAddressData Source)
{
    public bool CanSetDefault => Source.IsActive && !Source.IsDefault;

    public bool HasLocation =>
        Uri.TryCreate(Location, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public string ToggleAction => Source.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng";
}

public sealed record CustomerMediaRow(
    string Id,
    BitmapImage? Image,
    string Source,
    string Detail,
    CustomerMediaData SourceData);

public sealed record CustomerPurchasedItemRow(
    string Sku,
    string ProductName,
    string UnitCode,
    string TotalQuantity,
    string Revenue,
    string PurchaseCount,
    string LastUnitPrice,
    string LastPurchaseAt);

public sealed record CustomerOrderRow(
    string Id,
    string Number,
    string Date,
    string Total,
    string Status,
    string Settlement,
    string Delivery,
    string Receivable);

public sealed record CustomerReceivableRow(
    string Id,
    string DocumentNumber,
    string Date,
    string OriginalAmount,
    string AllocatedAmount,
    string RemainingAmount,
    string Status);

public sealed record CustomerPaymentRow(
    string Id,
    string DocumentNumber,
    string Date,
    string Method,
    string OriginalAmount,
    string AllocatedAmount,
    string RemainingAmount,
    string Status);

public sealed record CustomerDeliveryRow(
    string Id,
    string Number,
    string SalesOrderNumber,
    string Date,
    string HandoverMode,
    string Status,
    string Result,
    string LineCount);

public sealed record CustomerReturnRow(
    string Id,
    string Number,
    string Date,
    string Warehouse,
    string Status,
    string LineCount,
    string AcceptedLineCount,
    string Note);

public sealed record SupplierRow(
    int Stt,
    string Id,
    string Code,
    string Name,
    string TaxId,
    string Bank,
    string Delivery,
    string Status,
    string ToggleAction,
    SupplierData Source);

public sealed record SupplierContactRow(
    string Id,
    string Name,
    string Title,
    string Contact,
    string Primary,
    string Status,
    SupplierContactData Source);

public sealed record SupplierAddressRow(
    string Id,
    string Type,
    string Address,
    string Primary,
    string Status,
    SupplierAddressData Source);

public sealed record SupplierPaymentTermRow(
    string Id,
    string Method,
    string TermDays,
    string Description,
    string Primary,
    string Status,
    SupplierPaymentTermData Source);

public sealed class BulkColumnMappingRow
{
    private string _mapping = "IGNORE";

    public required int Index { get; init; }
    public required string SourceHeader { get; init; }
    public required string PreviewValue { get; init; }

    public string Mapping
    {
        get => _mapping;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "IGNORE" : value.Trim();
            if (string.Equals(_mapping, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _mapping = normalized;
            MappingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? MappingChanged;
}

public sealed record BulkPreviewRow(
    int RowNumber,
    string CustomerCode,
    string CustomerName,
    string Changes,
    string Result,
    CustomerBulkResultRow Source);

public static class PartnerPresentation
{
    public static readonly IReadOnlyList<PartnerLookupOption> StatusOptions =
    [
        new("all", "Tất cả"),
        new("active", "Đang hoạt động"),
        new("inactive", "Không hoạt động")
    ];

    public static readonly IReadOnlyList<PartnerLookupOption> SupplierStatusOptions =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng sử dụng")
    ];

    public static readonly IReadOnlyList<PartnerLookupOption> MappingOptions =
    [
        new("IGNORE", "Bỏ qua"),
        new("CUSTOMER_CODE", "Mã khách hàng"),
        new("NAME", "Tên khách hàng"),
        new("GROUP_CODE", "Nhóm khách hàng"),
        new("RESPONSIBLE_EMPLOYEE_CODE", "Nhân viên phụ trách"),
        new("PHONE", "Điện thoại"),
        new("EMAIL", "Email"),
        new("TAX_CODE", "Mã số thuế"),
        new("PAYMENT_TERMS_DAYS", "Thời hạn thanh toán"),
        new("CREDIT_LIMIT", "Hạn mức tín dụng"),
        new("NOTES", "Ghi chú")
    ];

    public static string Status(bool active) => active ? "Đang hoạt động" : "Không hoạt động";

    public static string SupplierStatus(bool active) => active ? "Đang hoạt động" : "Ngừng sử dụng";

    public static string YesNo(bool value) => value ? "Có" : "Không";

    public static string FormatDateTime(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed))
        {
            return "Chưa có";
        }

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public static string FormatMoney(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return value ?? "0";
        }

        return amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    }

    public static string FormatDecimal(string? value)=>OfficeNumberFormatting.Compact(value);

    public static string NormalizeSearch(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool MatchesStatus(bool active, string? filter) => filter switch
    {
        "active" => active,
        "inactive" => !active,
        _ => true
    };

    public static bool MatchesSearch(string search, params string?[] values)
    {
        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value)
            && value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    public static string CustomerAddress(CustomerAddressData address) =>
        string.Join(", ", new[]
        {
            address.AddressLine1,
            address.AddressLine2,
            address.Ward,
            address.District,
            address.Province,
            address.PostalCode,
            address.CountryCode
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public static string SupplierAddress(SupplierAddressData address) =>
        string.Join(", ", new[]
        {
            address.Street,
            address.City,
            address.Province,
            address.PostalCode,
            address.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public static string NormalizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasSpace = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    public static string MappingFromHeader(string header)
    {
        var value = NormalizeHeader(header);
        if (value.Length == 0) return "IGNORE";
        if (value.Contains("ma khach", StringComparison.Ordinal) || value is "ma kh" or "customer code") return "CUSTOMER_CODE";
        if (value.Contains("ten khach", StringComparison.Ordinal) || value is "ten kh" or "customer name") return "NAME";
        if (value.Contains("nhom khach", StringComparison.Ordinal) || value == "ma nhom") return "GROUP_CODE";
        if (value.Contains("nhan vien", StringComparison.Ordinal) || value.Contains("phu trach", StringComparison.Ordinal) || value == "ma nv") return "RESPONSIBLE_EMPLOYEE_CODE";
        if (value.Contains("dien thoai", StringComparison.Ordinal) || value.Contains("so dt", StringComparison.Ordinal) || value == "phone") return "PHONE";
        if (value.Contains("email", StringComparison.Ordinal)) return "EMAIL";
        if (value.Contains("ma so thue", StringComparison.Ordinal) || value.Contains("mst", StringComparison.Ordinal)) return "TAX_CODE";
        if (value.Contains("thoi han", StringComparison.Ordinal) || value.Contains("payment terms", StringComparison.Ordinal)) return "PAYMENT_TERMS_DAYS";
        if (value.Contains("han muc", StringComparison.Ordinal) || value.Contains("credit limit", StringComparison.Ordinal)) return "CREDIT_LIMIT";
        if (value.Contains("ghi chu", StringComparison.Ordinal) || value == "note") return "NOTES";
        return "IGNORE";
    }

    public static string OrderStatus(string value) => value switch
    {
        "draft" => "Nháp",
        "confirmed" => "Đã chốt",
        "closed" => "Hoàn thành",
        "cancelled" => "Đã hủy",
        _ => value
    };

    public static string DeliveryStatus(string value) => value switch
    {
        "pending" => "Chưa giao",
        "not_required" => "Không cần giao",
        "ready_to_dispatch" => "Sẵn sàng giao",
        "dispatched" => "Đang giao",
        "partially_delivered" => "Giao một phần",
        "delivered" => "Đã giao",
        "rescheduled" => "Hẹn lại",
        "failed" => "Giao chưa thành công",
        "returned" => "Đã trả hàng",
        "cancelled" => "Đã hủy",
        "handed_over" => "Đã bàn giao",
        _ => value
    };

    public static string SettlementStatus(string value) => value switch
    {
        "not_due" => "Chưa đến hạn",
        "pending" => "Chưa thanh toán",
        "partially_paid" => "Đã thanh toán một phần",
        "paid" => "Đã thanh toán",
        "overpaid" => "Thu thừa",
        "refunded" => "Đã hoàn tiền",
        "written_off" => "Đã xử lý công nợ",
        _ => value
    };

    public static string ReceivableStatus(string value) => value switch
    {
        "open" => "Còn phải thu",
        "partially_allocated" => "Đã thu một phần",
        "settled" => "Đã thu đủ",
        "reversed" => "Đã hủy",
        _ => value
    };

    public static string PaymentMethod(string value) => value switch
    {
        "CASH" => "Tiền mặt",
        "BANK_TRANSFER" => "Chuyển khoản",
        _ => value
    };

    public static string PaymentStatus(string value) => value switch
    {
        "open" => "Chưa phân bổ",
        "partially_allocated" => "Đã phân bổ một phần",
        "settled" => "Đã phân bổ đủ",
        "reversed" => "Đã hủy",
        _ => value
    };

    public static string HandoverMode(string value) => value switch
    {
        "DELIVERY" => "Giao khách",
        "PICKUP" => "Khách nhận tại kho",
        _ => value
    };

    public static string ReturnStatus(string value) => value switch
    {
        "draft" => "Chờ nhận hàng trả",
        "received" => "Đã nhận hàng trả",
        "cancelled" => "Đã hủy",
        _ => value
    };

    public static string AttemptSummary(CustomerDeliveryAttemptSummaryData? attempts)
    {
        if (attempts is null || string.IsNullOrWhiteSpace(attempts.LatestResult))
        {
            return "Chưa có kết quả";
        }

        var latest = attempts.LatestResult switch
        {
            "delivered_full" => "Giao đủ",
            "delivered_partial" => "Giao một phần",
            "failed" => "Giao chưa thành công",
            "rescheduled" => "Hẹn lại",
            _ => attempts.LatestResult
        };
        var notes = new List<string> { latest };
        if (int.TryParse(attempts.DeliveredPartialCount, out var partial) && partial > 0) notes.Add($"{partial} lần giao một phần");
        if (int.TryParse(attempts.FailedCount, out var failed) && failed > 0) notes.Add($"{failed} lần chưa thành công");
        if (int.TryParse(attempts.RescheduledCount, out var rescheduled) && rescheduled > 0) notes.Add($"{rescheduled} lần hẹn lại");
        return string.Join(" · ", notes);
    }

    public static string BulkResultText(CustomerBulkResultRow row, bool applied, bool importMode)
    {
        if (row.Errors.Length > 0)
        {
            return row.Errors[0].Message;
        }

        var warning = row.Warnings.FirstOrDefault()?.Message;
        if (!applied)
        {
            if (importMode && !string.IsNullOrWhiteSpace(warning))
            {
                return warning;
            }

            return row.Status == "unchanged"
                ? "Không thay đổi"
                : importMode
                    ? "Sẵn sàng nhập"
                    : "Sẽ cập nhật";
        }

        return importMode
            ? row.Status == "created" ? "Đã nhập" : row.Status
            : row.Status == "updated" ? "Đã cập nhật" : "Không thay đổi";
    }
}
