using System.Globalization;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public static class CustomerOnboardingPresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static string StatusLabel(string? status) => status switch
    {
        "submitted" => "Mới gửi",
        "under_review" => "Đang xem xét",
        "need_more_info" => "Chờ bổ sung",
        "approved" => "Đã tạo khách mới",
        "linked_existing" => "Đã liên kết khách có sẵn",
        "rejected" => "Đã từ chối",
        "cancelled" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(status) ? "Chưa xác định" : status.Trim()
    };

    public static string SourceLabel(CustomerOnboardingRequestData request) =>
        string.Equals(request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal)
            ? "Ordering · Khách trực tiếp"
            : "MCP Field";

    public static string BroughtByLabel(CustomerOnboardingRequestData request, IReadOnlyDictionary<string, EmployeeData> employees)
    {
        if (string.Equals(request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal))
            return "Khách tự đăng ký";
        if (!string.IsNullOrWhiteSpace(request.RequestedByEmployeeId)
            && employees.TryGetValue(request.RequestedByEmployeeId, out var employee))
            return $"{employee.Code} — {employee.FullName}";
        return "Nhân viên MCP";
    }

    public static string ReasonLabel(CustomerOnboardingRequestData request)
    {
        if (string.Equals(request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal))
            return "Đăng ký tài khoản đặt hàng";
        return string.Equals(request.SourceDemandReference, "FIELD_PROFILE_VERIFICATION", StringComparison.Ordinal)
            ? "Đề nghị mở / liên kết mã khách hàng"
            : "Đề nghị tạo mã để phục vụ đơn hàng";
    }

    public static string OutletLabel(CustomerOnboardingRequestData request)
    {
        if (string.Equals(request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal)) return string.Empty;
        var routeName = MetadataString(request, "routeName");
        return string.IsNullOrWhiteSpace(routeName) ? request.ProposedCustomer.Name : routeName;
    }

    public static string BusinessType(CustomerOnboardingRequestData request) =>
        string.Equals(request.SourceSystem, "CUSTOMER_PORTAL", StringComparison.Ordinal)
            ? MetadataString(request, "businessType")
            : string.Empty;

    public static string Address(CustomerOnboardingAddressData address) =>
        string.Join(", ", new[] { address.AddressLine1, address.Ward, address.District, address.Province }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    public static string UpdatedAt(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) return value;
        var local = TimeZoneInfo.ConvertTime(parsed, VietnamTimeZone);
        return local.ToString("dd/MM/yyyy HH:mm:ss", Vietnamese);
    }

    public static string CustomerLabel(CustomerData customer) => $"{customer.Code} — {customer.Name}";

    public static string AddressLabel(CustomerAddressData address)
    {
        var value = string.Join(", ", new[] { address.AddressLine1, address.Ward, address.District, address.Province }
            .Where(item => !string.IsNullOrWhiteSpace(item)));
        return string.IsNullOrWhiteSpace(value) ? address.Label : $"{address.Label} — {value}";
    }

    private static string MetadataString(CustomerOnboardingRequestData request, string key)
    {
        if (request.SourceMetadata is null || !request.SourceMetadata.TryGetValue(key, out var value)) return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : string.Empty;
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
    }
}
