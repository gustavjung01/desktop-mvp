using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static class SalesOperationsPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string SalesOrderSourceLabel(string? sourceType, string? sourceId)
    {
        var normalizedType = (sourceType ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedId = (sourceId ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedType == "MCP") return "Nhân viên thị trường";
        if (normalizedType == "API" && normalizedId.StartsWith("CUSTOMER_PORTAL", StringComparison.Ordinal))
            return "Khách hàng";
        return "Công Ty";
    }

    public static string CustomerName(SalesOrderData order)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerName)) return order.CustomerName.Trim();
        if (!string.IsNullOrWhiteSpace(order.WalkInDisplayName)) return order.WalkInDisplayName.Trim();
        return "Khách chưa đặt tên";
    }

    public static string OnboardingStatusLabel(string? status) => status?.Trim() switch
    {
        "submitted" => "Mới gửi",
        "under_review" => "Đang xem xét",
        "need_more_info" => "Cần bổ sung",
        _ => "Trạng thái khác"
    };

    public static string Address(CustomerOnboardingAddressData address)
    {
        var parts = new[] { address.AddressLine1, address.Ward, address.Province }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        return string.Join(", ", parts);
    }

    public static string UpdatedAt(string? value)
    {
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return "—";

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static DateTimeOffset SortTime(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;

    public static SalesOperationsOrderRow OrderRow(SalesOrderData order, int index) =>
        new(index + 1, CustomerName(order), "Chờ xác nhận",
            $"{order.WarehouseName} · {SalesOrderSourceLabel(order.SourceType, order.SourceId)}",
            $"Cập nhật {UpdatedAt(order.UpdatedAt)}");

    public static SalesOperationsOnboardingRow OnboardingRow(CustomerOnboardingRequestData request, int index) =>
        new(index + 1,
            string.IsNullOrWhiteSpace(request.ProposedCustomer.Name) ? "Khách chưa đặt tên" : request.ProposedCustomer.Name.Trim(),
            OnboardingStatusLabel(request.Status),
            Address(request.ProposedCustomer.Address),
            $"Cập nhật {UpdatedAt(request.UpdatedAt)}");
}

public sealed record SalesOperationsOrderRow(int Number, string CustomerName, string Status, string Meta, string UpdatedAt);
public sealed record SalesOperationsOnboardingRow(int Number, string CustomerName, string Status, string Meta, string UpdatedAt);
