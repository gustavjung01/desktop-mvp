using System.Globalization;

namespace CongTy.Desktop.Inventory;

public static class ManualInboundPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string InboundType(string? value) => value switch
    {
        "MANUAL_RECEIPT" => "Nhập hàng thủ công",
        "OFF_DOCUMENT_CUSTOMER_RETURN" => "Khách trả ngoài chứng từ",
        "RECOVERY" => "Hàng thu hồi",
        "OTHER" => "Khác",
        _ => "Nhập kho thủ công"
    };

    public static string HistoryStatus(string? value) => value == "REVERSED" ? "Đã đảo" : "Đã nhập";

    public static string Quantity(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        }

        return parsed.ToString("0.############", Vietnamese);
    }

    public static string Cost(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        return parsed.ToString("#,0.##", Vietnamese);
    }

    public static string Date(string? value)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return "—";
        }

        return parsed.ToString("dd/MM/yyyy", Vietnamese);
    }

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return "—";
        }

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }
}
