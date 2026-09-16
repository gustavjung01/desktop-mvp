using System.Globalization;
using System.Numerics;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public sealed record CodWarehouseOption(string Id, string Label);

public sealed record CodCustodyDriverRow(string Driver, string Currency, string CollectionCount, string RemainingAmount, string Oldest);
public sealed record CodCollectionActivityRow(string Currency, string Method, string Status, string Count, string Expected, string Received);
public sealed record CodHandoverActivityRow(string Currency, string Count, string Claimed, string Difference);
public sealed record CodAcceptanceActivityRow(string Currency, string Count, string Accepted, string Difference);
public sealed record CodPendingHandoverRow(string HandoverId, string Source, string Warehouse, string Currency, string PendingAmount, string HandedOverAt);
public sealed record CodOverduePromiseRow(string DeliveryOrder, string TripDriver, string ExpectedAmount, string PromisedBy, string Overdue);
public sealed record CodExceptionRow(string Type, string Source, string Warehouse, string Detail, string? HandoverId)
{
    public bool CanOpenAccounting => !string.IsNullOrWhiteSpace(HandoverId);
}
public sealed record CodHandoverListRow(CodHandoverData Data, string Trip, string Driver, string Warehouse, string HandedOver, string Difference, string Status, string HandedOverAt);
public sealed record CodHandoverLineRow(CodHandoverLineData Data, string DeliveryOrder, string Customer, string Expected, string HandedOver);

public static class CodAccountingPresentation
{
    private static readonly BigInteger Scale = BigInteger.Pow(10, 6);
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string CollectionMethod(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "cash" => "Tiền mặt",
        "cod" => "Thu khi giao hàng",
        "cash_on_delivery" => "Thu khi giao hàng",
        "bank_transfer" => "Chuyển khoản",
        "transfer" => "Chuyển khoản",
        null or "" => "—",
        _ => OfficeToken(value)
    };

    public static string CollectionStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Chờ thu",
        "promised" => "Đã hẹn thu",
        "collected" => "Đã thu",
        "partially_collected" => "Thu một phần",
        "reversed" => "Đã hoàn tác",
        "waived" => "Không thu",
        "not_collected" => "Chưa thu",
        null or "" => "—",
        _ => OfficeToken(value)
    };

    public static string ReconciliationStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "submitted" => "Chờ xác nhận",
        "reconciled" => "Đã khớp",
        "discrepancy" => "Có chênh lệch",
        "reversed" => "Đã hoàn tác",
        "acceptance_reversed" => "Đã hoàn tác xác nhận",
        "matched" => "Đã khớp",
        "mismatch" => "Cần kiểm tra",
        "unresolved" => "Chưa xử lý",
        null or "" => "—",
        _ => OfficeToken(value)
    };

    public static string HandoverStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "submitted" => "Chờ xác nhận",
        "reconciled" => "Đã khớp",
        "discrepancy" => "Có chênh lệch",
        "reversed" => "Đã đảo bàn giao",
        "acceptance_reversed" => "Đã đảo xác nhận",
        null or "" => "—",
        _ => OfficeToken(value)
    };

    public static string Count(string? value)
    {
        var raw = value?.Trim() ?? "0";
        return BigInteger.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("N0", Vietnamese)
            : raw;
    }

    public static bool TryScaled(string? value, out BigInteger scaled)
    {
        scaled = BigInteger.Zero;
        var raw = value?.Trim() ?? string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"^(-?)(\d+)(?:\.(\d{1,6}))?$");
        if (!match.Success) return false;
        var whole = BigInteger.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var fraction = match.Groups[3].Success ? match.Groups[3].Value.PadRight(6, '0') : "000000";
        scaled = whole * Scale + BigInteger.Parse(fraction, CultureInfo.InvariantCulture);
        if (match.Groups[1].Value == "-") scaled = -scaled;
        return true;
    }

    public static string Decimal(BigInteger scaled)
    {
        var negative = scaled < 0;
        var absolute = BigInteger.Abs(scaled);
        var fraction = (absolute % Scale).ToString().PadLeft(6, '0').TrimEnd('0');
        return $"{(negative ? "-" : string.Empty)}{absolute / Scale}{(fraction.Length == 0 ? string.Empty : "." + fraction)}";
    }

    public static string Decimal(string? value)
    {
        if (!TryScaled(value, out var scaled)) return value?.Trim() ?? "0";
        return Decimal(scaled);
    }

    public static string Money(string? value, string? currencyCode = "VND")
    {
        if (!TryScaled(value, out var scaled)) return $"{value?.Trim() ?? "0"} {currencyCode}".Trim();
        var negative = scaled < 0;
        var absolute = BigInteger.Abs(scaled);
        var whole = (absolute / Scale).ToString();
        var grouped = GroupThousands(whole);
        var fraction = (absolute % Scale).ToString().PadLeft(6, '0').TrimEnd('0');
        var code = string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : " " + currencyCode.Trim().ToUpperInvariant();
        return $"{(negative ? "-" : string.Empty)}{grouped}{(fraction.Length == 0 ? string.Empty : "," + fraction)}{code}";
    }

    public static string Timestamp(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        return parsed.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static CodWarehouseOption Warehouse(CodWarehouseOptionData row) =>
        new(row.WarehouseId, $"{row.WarehouseCode} · {row.WarehouseName}");

    public static CodCustodyDriverRow Custody(CodCustodyDriverData row) =>
        new(
            $"{row.DriverCode} · {row.DriverName}",
            row.CurrencyCode,
            Count(row.CollectionCount),
            Money(row.CustodyRemainingAmount, row.CurrencyCode),
            $"{Timestamp(row.OldestCollectedAt)} · {Count(row.OldestAgeDays)} ngày");

    public static CodCollectionActivityRow Collection(CodCollectionActivityData row) =>
        new(row.CurrencyCode, CollectionMethod(row.CollectionMethod), CollectionStatus(row.CollectionStatus), Count(row.CollectionCount), Money(row.ExpectedAmount, row.CurrencyCode), Money(row.ReceivedAmount, row.CurrencyCode));

    public static CodHandoverActivityRow HandoverActivity(CodHandoverActivityData row) =>
        new(row.CurrencyCode, Count(row.HandoverCount), Money(row.ClaimedAmount, row.CurrencyCode), Money(row.HandoverDifferenceAmount, row.CurrencyCode));

    public static CodAcceptanceActivityRow AcceptanceActivity(CodAcceptanceActivityData row) =>
        new(row.CurrencyCode, Count(row.AcceptanceCount), Money(row.AcceptedAmount, row.CurrencyCode), Money(row.VarianceAmount, row.CurrencyCode));

    public static CodPendingHandoverRow Pending(CodHandoverQueueData row) =>
        new(row.HandoverId, $"{row.TripNumber} · {row.DriverCode}", row.WarehouseCode, row.CurrencyCode ?? "—", Money(row.PendingAcceptanceAmount, row.CurrencyCode), Timestamp(row.HandedOverAt));

    public static CodOverduePromiseRow Promise(CodOverduePromiseData row) =>
        new(string.IsNullOrWhiteSpace(row.DeliveryOrderNumber) ? "Thiếu mã phiếu giao" : row.DeliveryOrderNumber.Trim(), $"{row.TripNumber} · {row.DriverCode}", Money(row.ExpectedAmount, row.CurrencyCode), row.PromisedBy, $"{Count(row.OverdueDays)} ngày");

    public static CodHandoverListRow Handover(CodHandoverData row) =>
        new(
            row,
            string.IsNullOrWhiteSpace(row.TripNumber) ? "Chuyến giao" : row.TripNumber.Trim(),
            $"{row.DriverCode ?? "—"} · {row.DriverName ?? "Chưa rõ tài xế"}",
            $"{row.WarehouseCode ?? "—"} · {row.WarehouseName ?? "Chưa rõ kho"}",
            Money(row.HandedOverTotal),
            Money(row.DifferenceAmount),
            HandoverStatus(row.Status),
            Timestamp(row.HandedOverAt));

    public static CodHandoverLineRow HandoverLine(CodHandoverLineData row) =>
        new(
            row,
            string.IsNullOrWhiteSpace(row.DeliveryOrderNumber) ? "Thiếu mã phiếu giao" : row.DeliveryOrderNumber.Trim(),
            $"{row.CustomerCode ?? "—"} · {row.CustomerName ?? "Khách hàng"}",
            Money(row.ExpectedAmount),
            Money(row.HandedOverAmount));

    public static string CustodyAmountList(IEnumerable<CodCurrencyAmountData> rows)
    {
        var values = rows.Select(row => Money(row.CustodyRemainingAmount, row.CurrencyCode)).ToArray();
        return values.Length == 0 ? "0" : string.Join(" · ", values);
    }

    public static string OfficeToken(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "—"
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Trim().Replace('_', ' ').Replace('-', ' ').ToLowerInvariant());

    private static string GroupThousands(string value)
    {
        if (value.Length <= 3) return value;
        var first = value.Length % 3;
        if (first == 0) first = 3;
        var groups = new List<string> { value[..first] };
        for (var index = first; index < value.Length; index += 3)
            groups.Add(value.Substring(index, Math.Min(3, value.Length - index)));
        return string.Join(".", groups);
    }
}
