using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Pricing;

public static class PricingPresentation
{
    public static IReadOnlyList<PricingOption> ListTypes { get; } =
    [
        new("BASE", "Giá nền"),
        new("CHANNEL", "Theo kênh"),
        new("CUSTOMER_GROUP", "Theo nhóm khách"),
        new("CUSTOMER", "Theo khách hàng"),
        new("PROMOTION", "Khuyến mãi"),
        new("CUSTOM", "Điều kiện khác")
    ];

    public static IReadOnlyList<PricingOption> StackingModes { get; } =
    [
        new("EXCLUSIVE", "Chỉ áp dụng một mức"),
        new("STACKABLE", "Có thể kết hợp")
    ];

    public static IReadOnlyList<PricingOption> AdjustmentTypes { get; } =
    [
        new("FIXED_PRICE", "Đặt giá trực tiếp"),
        new("PERCENT_DISCOUNT", "Giảm phần trăm"),
        new("AMOUNT_DISCOUNT", "Giảm số tiền"),
        new("PERCENT_MARKUP", "Tăng phần trăm"),
        new("AMOUNT_MARKUP", "Tăng số tiền")
    ];

    public static int DefaultPriority(string listType) => listType switch
    {
        "BASE" => 100,
        "CHANNEL" => 200,
        "CUSTOMER_GROUP" => 300,
        "PROMOTION" => 400,
        "CUSTOMER" => 500,
        "CUSTOM" => 600,
        _ => 100
    };

    public static string ListTypeLabel(string? value) =>
        ListTypes.FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.Ordinal))?.Label ?? value ?? "—";

    public static string AdjustmentLabel(string? value) =>
        AdjustmentTypes.FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.Ordinal))?.Label ?? value ?? "—";

    public static string SourceLabel(string? value) => value switch
    {
        "ADMIN" => "Nhập trực tiếp",
        "IMPORT" => "Nhập từ tệp",
        "CODE" => "Thiết lập tự động",
        _ => value ?? "—"
    };

    public static string Status(bool active) => active ? "Hoạt động" : "Ngừng";

    public static string ToggleAction(bool active) => active ? "Ngừng sử dụng" : "Đưa vào sử dụng";

    public static string Scope(PriceListData value) =>
        !string.IsNullOrWhiteSpace(value.CustomerName) ? value.CustomerName! :
        !string.IsNullOrWhiteSpace(value.CustomerGroupName) ? value.CustomerGroupName! :
        !string.IsNullOrWhiteSpace(value.ChannelName) ? value.ChannelName! : "Mặc định";

    public static string ApplyMode(PriceListData value) =>
        $"{(value.StackingMode == "STACKABLE" ? "Có thể kết hợp" : "Chỉ áp dụng một mức")}{(value.StopProcessing ? " · Không xét tiếp" : string.Empty)}";

    public static string AdjustmentValue(PriceListItemData item) =>
        item.RateBps is null ? Money(item.AmountMinor) : $"{BpsToPercent(item.RateBps.Value)}%";

    public static string QuantityRange(PriceListItemData item) =>
        $"{item.MinQuantity} → {(string.IsNullOrWhiteSpace(item.MaxQuantity) ? "∞" : item.MaxQuantity)}";

    public static string ItemSource(PriceListItemData item) =>
        $"{SourceLabel(item.SourceKind)}{(string.IsNullOrWhiteSpace(item.ExternalRuleCode) ? string.Empty : $" · {item.ExternalRuleCode}")}";

    public static string Money(string? value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor)) return string.IsNullOrWhiteSpace(value) ? "—" : value!;
        return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", minor);
    }

    public static string BpsToPercent(int value)
    {
        var number = value / 100m;
        return number.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static int PercentToBps(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var percent)
            || percent < 0 || decimal.Round(percent, 2) != percent)
        {
            throw new InvalidOperationException("Phần trăm chỉ nhận tối đa 2 chữ số thập phân.");
        }

        return checked((int)(percent * 100m));
    }

    public static string ResolutionStepLabel(string? kind) => kind switch
    {
        "BASE" => "Giá nền",
        "RULE" => "Mức giá được áp dụng",
        "SKIPPED" => "Mức giá không được áp dụng",
        "MANUAL_OVERRIDE" => "Giá điều chỉnh thủ công",
        _ => kind ?? "—"
    };

    public static string ResolutionDetail(PricingResolutionStepData step)
    {
        var parts = new List<string> { ResolutionStepLabel(step.Kind) };
        if (!string.IsNullOrWhiteSpace(step.PriceListCode)) parts.Add(step.PriceListCode!);
        if (!string.IsNullOrWhiteSpace(step.AdjustmentType)) parts.Add(AdjustmentLabel(step.AdjustmentType));
        if (!string.IsNullOrWhiteSpace(step.Reason)) parts.Add(step.Reason!);
        return string.Join(" · ", parts);
    }

    public static string ResolutionPrices(PricingResolutionStepData step)
    {
        var before = string.IsNullOrWhiteSpace(step.BeforeUnitPriceMinor) ? string.Empty : Money(step.BeforeUnitPriceMinor);
        var after = string.IsNullOrWhiteSpace(step.AfterUnitPriceMinor) ? string.Empty : Money(step.AfterUnitPriceMinor);
        return before.Length > 0 && after.Length > 0 ? $"{before} → {after}" : before.Length > 0 ? before : after;
    }

    public static string SummaryRule(IReadOnlyList<PriceListItemData> rules)
    {
        var active = rules.Where(row => row.IsActive).ToArray();
        if (active.Length == 0) return "—";
        if (active.Length != 1) return "Nhiều mức giá";
        var rule = active[0];
        if (rule.AdjustmentType != "FIXED_PRICE"
            || NormalizeDecimal(rule.MinQuantity) != "0"
            || !string.IsNullOrWhiteSpace(rule.MaxQuantity)
            || !string.IsNullOrWhiteSpace(rule.EffectiveFrom)
            || !string.IsNullOrWhiteSpace(rule.EffectiveTo))
        {
            return "Nhiều mức giá";
        }

        return Money(rule.AmountMinor);
    }

    private static string NormalizeDecimal(string? value)
    {
        var input = (value ?? string.Empty).Trim();
        if (input.Length == 0) return string.Empty;
        if (!input.Contains('.', StringComparison.Ordinal)) return input;
        return input.TrimEnd('0').TrimEnd('.') is { Length: > 0 } normalized ? normalized : "0";
    }
}

public sealed record PricingOption(string Value, string Label);

public sealed record PricingChannelRow(SalesChannelData Data)
{
    public string Id => Data.Id;
    public string Code => Data.Code;
    public string Name => Data.Name;
    public string Status => PricingPresentation.Status(Data.IsActive);
    public string ToggleAction => PricingPresentation.ToggleAction(Data.IsActive);
}

public sealed record PricingListRow(PriceListData Data)
{
    public string Id => Data.Id;
    public string Code => Data.Code;
    public string Name => Data.Name;
    public string CodeAndName => $"{Data.Code}\n{Data.Name}";
    public string Type => PricingPresentation.ListTypeLabel(Data.ListType);
    public string Scope => PricingPresentation.Scope(Data);
    public int Priority => Data.Priority;
    public string ApplyMode => PricingPresentation.ApplyMode(Data);
    public string Status => PricingPresentation.Status(Data.IsActive);
    public string ToggleAction => PricingPresentation.ToggleAction(Data.IsActive);
}

public sealed record PricingItemRow(PriceListItemData Data)
{
    public string Id => Data.Id;
    public string Sku => Data.Sku;
    public string ProductName => Data.ProductName;
    public string SkuAndProduct => $"{Data.Sku}\n{Data.ProductName}";
    public string Adjustment => PricingPresentation.AdjustmentLabel(Data.AdjustmentType);
    public string Value => PricingPresentation.AdjustmentValue(Data);
    public string Quantity => PricingPresentation.QuantityRange(Data);
    public string Source => PricingPresentation.ItemSource(Data);
    public string Status => PricingPresentation.Status(Data.IsActive);
    public string ToggleAction => PricingPresentation.ToggleAction(Data.IsActive);
}

public sealed record PricingLookup(string Id, string Label, object? Data = null);

public sealed record OverviewPriceListOption(string Key, string Label, PriceListData? PriceList);

public sealed class PricingOverviewRow
{
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public required string Sku { get; init; }
    public required string VariantName { get; init; }
    public required string UnitName { get; init; }
    public required string BasePrice { get; init; }
    public Dictionary<string, string> PriceCells { get; } = new(StringComparer.Ordinal);
}

public sealed record PricingResolutionStepRow(string Detail, string Prices);
