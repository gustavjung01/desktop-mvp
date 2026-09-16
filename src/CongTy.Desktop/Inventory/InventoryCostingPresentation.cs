using System.Globalization;
using System.Numerics;

namespace CongTy.Desktop.Inventory;

public static class InventoryCostingPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string DecimalText(string? value, int digits = 6)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var normalized = value.Trim();
        var parts = normalized.Split('.', 2);
        if (parts.Length == 1) return parts[0];

        var fraction = parts[1].Length > digits ? parts[1][..digits] : parts[1];
        fraction = fraction.TrimEnd('0');
        return fraction.Length == 0 ? parts[0] : $"{parts[0]}.{fraction}";
    }

    public static string MoneyVnd(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa xác định";

        var normalized = value.Trim();
        var negative = normalized.StartsWith("-", StringComparison.Ordinal);
        if (negative) normalized = normalized[1..];

        var parts = normalized.Split('.', 2);
        if (!BigInteger.TryParse(parts[0].Length == 0 ? "0" : parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole))
        {
            return value;
        }

        var fraction = parts.Length > 1 ? parts[1] : string.Empty;
        if (fraction.Length > 0 && fraction[0] >= '5') whole += BigInteger.One;

        var grouped = GroupThousands(whole.ToString(CultureInfo.InvariantCulture));
        return $"{(negative ? "-" : string.Empty)}{grouped} ₫";
    }

    public static string Status(string? value) => value switch
    {
        "COSTED" => "Đã tính giá",
        "ANOMALY" => "Thiếu nguồn giá",
        "OK" => "Khớp",
        "QUANTITY_MISMATCH" => "Lệch số lượng",
        "COST_ANOMALY" => "Có bất thường giá",
        "OPEN" => "Đang mở",
        "CLOSED" => "Đã khóa",
        "RESOLVED" => "Đã xử lý",
        _ => string.IsNullOrWhiteSpace(value) ? "—" : value
    };

    public static string AdjustmentType(string? value) => value switch
    {
        "LANDED_COST" => "Chi phí mua hàng bổ sung",
        "PURCHASE_PRICE_VARIANCE" => "Chênh lệch giá mua",
        "FORWARD_CORRECTION" => "Điều chỉnh kỳ hiện tại",
        _ => OfficeCode(value)
    };

    public static string IssueCode(string? value) => value switch
    {
        "COST_QUANTITY_INVALID" => "Số lượng không hợp lệ",
        "POOL_COST_BLOCKED" => "Nhóm giá vốn đang bị chặn",
        "COST_NEGATIVE_STOCK" => "Tồn âm cần kiểm tra giá vốn",
        "CLOSED_PERIOD_LATE_MOVEMENT" => "Phát sinh muộn vào kỳ đã khóa",
        "ADJUSTMENT_PROJECTION_ERROR" => "Chưa cập nhật được điều chỉnh giá",
        "COST_ANOMALY" => "Có bất thường giá vốn",
        _ => OfficeCode(value)
    };

    public static string DocumentSource(string? documentType, string? lineReference)
    {
        var source = documentType switch
        {
            "PURCHASE_ORDER" => "Đơn mua hàng",
            "GOODS_RECEIPT" => "Phiếu nhận hàng",
            "INVENTORY_TRANSFER" => "Chuyển kho",
            "INVENTORY_ADJUSTMENT" => "Điều chỉnh tồn",
            "STOCKTAKE" => "Kiểm kê kho",
            "MANUAL_INBOUND" => "Nhập kho thủ công",
            "OPENING_BALANCE" => "Tồn đầu kỳ",
            _ => OfficeCode(documentType)
        };

        return string.IsNullOrWhiteSpace(lineReference) ? source : $"{source} · {lineReference}";
    }

    public static string CostSource(string? value) => value switch
    {
        "PURCHASE_ORDER_NET" => "Giá mua sau chiết khấu",
        "TRANSFER_CARRYING_COST" => "Giá vốn chuyển kho",
        "INTERNAL_CARRYING_COST" => "Giá vốn chuyển nội bộ",
        "HISTORICAL_REVERSAL" => "Giá vốn lịch sử của giao dịch đảo",
        "OPENING_EXPLICIT_COST" => "Giá vốn tồn đầu kỳ",
        "ORIGINAL_ISSUE_COST" => "Giá vốn xuất kho gốc",
        "APPROVED_ZERO_COST" => "Giá 0 đã được duyệt",
        "APPROVED_EXPLICIT_COST" => "Giá vốn được duyệt",
        "UNRESOLVED" => "Chưa xác định nguồn giá",
        _ => OfficeCode(value)
    };

    public static string EventType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var normalized = value.Trim();
        if (normalized.EndsWith("_IN", StringComparison.Ordinal))
        {
            return $"{MovementType(normalized[..^3])} · Nhập";
        }
        if (normalized.EndsWith("_OUT", StringComparison.Ordinal))
        {
            return $"{MovementType(normalized[..^4])} · Xuất";
        }
        return MovementType(normalized);
    }

    public static string DateText(string? value)
    {
        if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return "—";
        return parsed.ToString("dd/MM/yyyy", Vietnamese);
    }

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) return "—";
        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    private static string MovementType(string value) => value switch
    {
        "PURCHASE_RECEIPT" => "Nhận hàng mua",
        "TRANSFER_RECEIPT" => "Nhận chuyển kho",
        "TRANSFER_ISSUE" => "Xuất chuyển kho",
        "OPENING_BALANCE" => "Tồn đầu kỳ",
        "SALES_ISSUE" => "Xuất bán hàng",
        "SALES_CUSTOMER_RETURN" => "Khách trả hàng",
        "MANUAL_INBOUND" => "Nhập kho thủ công",
        "INVENTORY_ADJUSTMENT" => "Điều chỉnh tồn",
        "STOCKTAKE_ADJUSTMENT" => "Điều chỉnh kiểm kê",
        _ => OfficeCode(value)
    };

    private static string OfficeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        var words = value.Trim().Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static string GroupThousands(string digits)
    {
        if (digits.Length <= 3) return digits;
        var first = digits.Length % 3;
        var pieces = new List<string>();
        var index = 0;
        if (first > 0)
        {
            pieces.Add(digits[..first]);
            index = first;
        }

        while (index < digits.Length)
        {
            pieces.Add(digits.Substring(index, 3));
            index += 3;
        }

        return string.Join('.', pieces);
    }
}
