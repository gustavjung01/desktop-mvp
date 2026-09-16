using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public static class StocktakePresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Status(string? status) => status switch
    {
        "draft" => "Đang đếm",
        "counted" => "Chờ gửi duyệt",
        "submitted" => "Chờ duyệt",
        "recount_required" => "Yêu cầu đếm lại",
        "approved" => "Chờ cập nhật tồn",
        "posted" => "Hoàn tất",
        "cancelled" => "Đã hủy",
        "reversed" => "Đã hoàn tác",
        _ => "Đã cập nhật"
    };

    public static string WorkflowHint(string? status) => status switch
    {
        "draft" or "recount_required" => "Nhập số đếm thực tế cho toàn bộ phạm vi. Số hệ thống được ẩn trong lúc đếm.",
        "counted" => "Đã ghi nhận số đếm. Chọn Gửi duyệt để chuyển phiếu sang người có quyền duyệt.",
        "submitted" => "Đã gửi kiểm kê chờ duyệt. Phiếu đang chờ người có quyền duyệt.",
        "approved" => "Đã duyệt kết quả. Tồn kho chưa thay đổi. Chọn Cập nhật tồn kho để hoàn tất.",
        "posted" => "Hoàn tất. Tồn kho đã được cập nhật theo kết quả kiểm kê đã duyệt.",
        "reversed" => "Phần cập nhật tồn kho của phiếu này đã được hoàn tác.",
        "cancelled" => "Phiếu đã hủy và không làm thay đổi tồn kho.",
        _ => string.Empty
    };

    public static string Quantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        }

        return parsed.ToString("0.############", Vietnamese);
    }

    public static string SignedDifference(string? counted, string? expected, string? finalDelta = null)
    {
        if (decimal.TryParse(finalDelta, NumberStyles.Number, CultureInfo.InvariantCulture, out var final))
        {
            return Signed(final);
        }

        if (!decimal.TryParse(counted, NumberStyles.Number, CultureInfo.InvariantCulture, out var countedValue)
            || !decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedValue))
        {
            return "—";
        }

        return Signed(countedValue - expectedValue);
    }

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return "—";
        }

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static string DateText(string? value)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return "—";
        }

        return parsed.ToString("dd/MM/yyyy", Vietnamese);
    }

    public static string ScopeKey(InventoryBalanceData balance) =>
        $"{balance.LocationId ?? "<null>"}:{balance.BaseVariantId}:{balance.LotId ?? "<null>"}";

    private static string Signed(decimal value) =>
        value > 0
            ? $"+{Quantity(value.ToString(CultureInfo.InvariantCulture))}"
            : Quantity(value.ToString(CultureInfo.InvariantCulture));
}
