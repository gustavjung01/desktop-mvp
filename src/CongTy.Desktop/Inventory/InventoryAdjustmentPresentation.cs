using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public static class InventoryAdjustmentPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Kind(string? value) => value switch
    {
        "MANUAL_ADJUSTMENT" => "Điều chỉnh thủ công",
        "QUARANTINE_TRANSFER" => "Chuyển cách ly",
        "DAMAGED_TRANSFER" => "Chuyển hư hỏng",
        "SCRAP" => "Tiêu hủy",
        _ => "Điều chỉnh tồn"
    };

    public static string Status(string? value) => value switch
    {
        "DRAFT" => "Lập phiếu",
        "SUBMITTED" => "Chờ duyệt",
        "APPROVED" => "Chờ cập nhật tồn",
        "POSTED" => "Hoàn tất",
        "CANCELLED" => "Đã hủy",
        "REVERSED" => "Đã hoàn tác",
        _ => "Đã cập nhật"
    };

    public static string Direction(string? value) => value switch
    {
        "IN" => "Tăng tồn",
        "OUT" => "Giảm tồn",
        "NONE" => "Không chênh lệch",
        _ => "—"
    };

    public static string WorkflowHint(string? status) => status switch
    {
        "DRAFT" => "Phiếu đang được lập. Kiểm tra số lượng và lý do rồi chọn Gửi duyệt.",
        "SUBMITTED" => "Phiếu đang chờ người có quyền duyệt.",
        "APPROVED" => "Tồn kho chưa thay đổi. Chọn Cập nhật tồn kho để hoàn tất.",
        "POSTED" => "Hoàn tất. Tồn kho đã được cập nhật theo phiếu đã duyệt.",
        "REVERSED" => "Phần cập nhật tồn kho của phiếu này đã được hoàn tác.",
        "CANCELLED" => "Phiếu đã hủy và không làm thay đổi tồn kho.",
        _ => string.Empty
    };

    public static string Source(InventoryAdjustmentData item)
    {
        if (!string.IsNullOrWhiteSpace(item.CorrectionOfAdjustmentId))
        {
            return "Phiếu điều chỉnh trước";
        }

        return item.DocumentKind == "MANUAL_ADJUSTMENT"
            ? "Điều chỉnh thủ công"
            : Kind(item.DocumentKind);
    }

    public static string Quantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        }

        return parsed.ToString("0.############", Vietnamese);
    }

    public static string SignedQuantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        var normalized = Quantity(parsed.ToString(CultureInfo.InvariantCulture));
        return parsed > 0 ? $"+{normalized}" : normalized;
    }

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return "—";
        }

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static string Warehouse(string? code, string? name)
    {
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name)) return $"{code} · {name}";
        return code ?? name ?? "Kho";
    }

    public static string ActorTime(string? actor, string? time, string fallback)
    {
        var person = string.IsNullOrWhiteSpace(actor) ? fallback : actor.Trim();
        return string.IsNullOrWhiteSpace(time) ? person : $"{person} · {DateTimeText(time)}";
    }

    public static string BalanceKey(InventoryBalanceData balance) =>
        $"{balance.WarehouseId}:{balance.LocationId ?? "<null>"}:{balance.BaseVariantId}:{balance.LotId ?? "<null>"}";
}
