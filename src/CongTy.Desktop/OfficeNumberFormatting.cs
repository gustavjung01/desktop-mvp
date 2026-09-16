using System.Globalization;

namespace CongTy.Desktop;

public static class OfficeNumberFormatting
{
    public static string Compact(string? value,string empty="—")
    {
        if(string.IsNullOrWhiteSpace(value))return empty;
        return decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)?Compact(amount):value.Trim();
    }
    public static string Compact(decimal value)=>value.ToString("0.############################",CultureInfo.GetCultureInfo("vi-VN"));
    public static string CompactInvariant(string? value,string empty="0")
    {
        if(string.IsNullOrWhiteSpace(value))return empty;
        return decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)
            ? amount.ToString("0.############################",CultureInfo.InvariantCulture)
            : value.Trim();
    }
    public static string Percent(string? value)=>$"{Compact(value,"0")}%";
}
