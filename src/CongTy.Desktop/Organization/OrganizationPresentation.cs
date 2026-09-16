using CongTy.Contracts;

namespace CongTy.Desktop.Organization;

public sealed record LookupOption(string Id, string Label);

public sealed record BranchRow(
    string Id,
    string Code,
    string Name,
    string Address,
    string Phone,
    string Email,
    string Status,
    string StatusAction,
    bool IsActive,
    string UpdatedAt,
    BranchData Source);

public sealed record WarehouseRow(
    string Id,
    string Code,
    string Name,
    string Branch,
    string Type,
    string LayoutMode,
    bool NegativeStockEnabled,
    string NegativeStockStatus,
    string Status,
    string StatusAction,
    bool IsActive,
    string UpdatedAt,
    WarehouseData Source);

public sealed record LocationRow(
    string Id,
    string Code,
    string Name,
    string Warehouse,
    string Branch,
    string Type,
    string Status,
    string StatusAction,
    bool IsActive,
    string UpdatedAt,
    WarehouseLocationData Source);

public sealed record EmployeeRow(
    string Id,
    string Code,
    string FullName,
    string JobTitle,
    string Branch,
    string Contact,
    string Status,
    string UpdatedAt,
    EmployeeData Source);

public sealed record LocationModeRunRow(
    string Id,
    string CompletedAt,
    string Action,
    string Transition,
    string SkuCount,
    string CompletedBy,
    WarehouseLocationModeRun Source);

public sealed record LocationModeLineRow(
    string Sku,
    string Lot,
    string From,
    string To,
    string Quantity);

public sealed record OrganizationOverviewHierarchyRow(
    string Code,
    string Name,
    string Address,
    string Status,
    int WarehouseCount,
    int ActiveWarehouseCount,
    int LocationCount,
    bool IsActive);

public sealed record OrganizationOverviewRecentRow(
    string Scope,
    string Code,
    string Name,
    string Relation,
    string Status,
    string UpdatedAt,
    bool IsActive);

public static class OrganizationPresentation
{
    public static readonly IReadOnlyList<LookupOption> BranchStatusOptions =
    [
        new("all", "Tất cả"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng hoạt động")
    ];

    public static readonly IReadOnlyList<LookupOption> WarehouseStatusOptions =
    [
        new("all", "Tất cả"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng hoạt động")
    ];

    public static readonly IReadOnlyList<LookupOption> NegativeStockOptions =
    [
        new("deny", "Không cho phép xuất vượt tồn"),
        new("allow", "Cho phép xuất vượt tồn khả dụng")
    ];

    public static readonly IReadOnlyList<LookupOption> StatusOptions =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang hoạt động"),
        new("inactive", "Ngừng hoạt động")
    ];

    public static readonly IReadOnlyList<LookupOption> WarehouseTypeOptions =
    [
        new("main", "Kho chính"),
        new("distribution", "Kho phân phối"),
        new("vehicle", "Kho xe"),
        new("quarantine", "Kho cách ly"),
        new("returns", "Kho hàng trả"),
        new("transit", "Kho trung chuyển"),
        new("other", "Loại khác")
    ];

    public static readonly IReadOnlyList<LookupOption> LocationTypeOptions =
    [
        new("storage", "Khu lưu trữ"),
        new("receiving", "Khu nhận hàng"),
        new("shipping", "Khu xuất hàng"),
        new("quarantine", "Khu cách ly"),
        new("returns", "Khu hàng trả"),
        new("damaged", "Khu hư hỏng"),
        new("other", "Loại khác")
    ];

    public static readonly IReadOnlyList<LookupOption> WarehouseLocationTypeOptions =
    [
        new("storage", "Khu lưu trữ"),
        new("receiving", "Khu nhận hàng"),
        new("shipping", "Khu xuất hàng"),
        new("quarantine", "Khu cách ly"),
        new("returns", "Khu hàng trả"),
        new("damaged", "Khu hư hỏng"),
        new("other", "Khu vực khác")
    ];

    public static readonly IReadOnlyList<LookupOption> LayoutModeOptions =
    [
        new("MANAGED", "Có sơ đồ kho"),
        new("UNMANAGED", "Không dùng sơ đồ")
    ];

    public static IReadOnlyList<OrganizationOverviewHierarchyRow> BuildOverviewHierarchy(
        IReadOnlyList<BranchData> branches,
        IReadOnlyList<WarehouseData> warehouses,
        IReadOnlyList<WarehouseLocationData> locations) =>
        branches
            .OrderBy(branch => branch.Code)
            .Select(branch =>
            {
                var branchWarehouses = warehouses
                    .Where(warehouse => warehouse.BranchId == branch.Id)
                    .ToArray();
                var warehouseIds = branchWarehouses
                    .Select(warehouse => warehouse.Id)
                    .ToHashSet(StringComparer.Ordinal);
                return new OrganizationOverviewHierarchyRow(
                    branch.Code,
                    branch.Name,
                    string.IsNullOrWhiteSpace(branch.Address) ? "Chưa có địa chỉ" : branch.Address!,
                    Status(branch.IsActive),
                    branchWarehouses.Length,
                    branchWarehouses.Count(warehouse => warehouse.IsActive),
                    locations.Count(location => warehouseIds.Contains(location.WarehouseId)),
                    branch.IsActive);
            })
            .ToArray();

    public static IReadOnlyList<OrganizationOverviewRecentRow> BuildOverviewRecent(
        IReadOnlyList<BranchData> branches,
        IReadOnlyList<WarehouseData> warehouses,
        IReadOnlyList<WarehouseLocationData> locations,
        int limit = 8)
    {
        var branchMap = branches.ToDictionary(branch => branch.Id, StringComparer.Ordinal);
        var warehouseMap = warehouses.ToDictionary(warehouse => warehouse.Id, StringComparer.Ordinal);
        var rows = new List<(OrganizationOverviewRecentRow Row, DateTimeOffset UpdatedAt)>();

        rows.AddRange(branches.Select(branch =>
        {
            var relation = !string.IsNullOrWhiteSpace(branch.Address)
                ? branch.Address!
                : !string.IsNullOrWhiteSpace(branch.Email)
                    ? branch.Email!
                    : "Không có thông tin liên hệ";
            return (
                new OrganizationOverviewRecentRow(
                    "branches",
                    branch.Code,
                    branch.Name,
                    relation,
                    Status(branch.IsActive),
                    FormatDateTime(branch.UpdatedAt),
                    branch.IsActive),
                SortDate(branch.UpdatedAt));
        }));

        rows.AddRange(warehouses.Select(warehouse =>
        {
            branchMap.TryGetValue(warehouse.BranchId, out var branch);
            var relation = branch is null
                ? "Chưa xác định chi nhánh"
                : $"{branch.Code} · {branch.Name}";
            return (
                new OrganizationOverviewRecentRow(
                    "warehouses",
                    warehouse.Code,
                    warehouse.Name,
                    relation,
                    Status(warehouse.IsActive),
                    FormatDateTime(warehouse.UpdatedAt),
                    warehouse.IsActive),
                SortDate(warehouse.UpdatedAt));
        }));

        rows.AddRange(locations.Select(location =>
        {
            warehouseMap.TryGetValue(location.WarehouseId, out var warehouse);
            var branch = warehouse is not null && branchMap.TryGetValue(warehouse.BranchId, out var parentBranch)
                ? parentBranch
                : null;
            var relation = string.Join(
                " · ",
                new[] { branch?.Code, warehouse?.Code, LocationType(location.LocationType) }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            return (
                new OrganizationOverviewRecentRow(
                    "locations",
                    location.Code,
                    location.Name,
                    string.IsNullOrWhiteSpace(relation) ? "Chưa xác định đơn vị liên quan" : relation,
                    Status(location.IsActive),
                    FormatDateTime(location.UpdatedAt),
                    location.IsActive),
                SortDate(location.UpdatedAt));
        }));

        return rows
            .OrderByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Row.Code, StringComparer.Ordinal)
            .Take(Math.Max(0, limit))
            .Select(item => item.Row)
            .ToArray();
    }

    private static DateTimeOffset SortDate(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;

    public static string StatusAction(bool active) =>
        active ? "Ngừng sử dụng" : "Đưa vào sử dụng";

    public static string WarehouseNegativeStockStatus(bool enabled) =>
        enabled ? "Đang bật" : "Đang tắt";

    public static string WarehouseLocationType(string value) =>
        value == "other" ? "Khu vực khác" : LocationType(value);

    public static string BranchStatusAction(bool active) =>
        active ? "Ngừng sử dụng" : "Đưa vào sử dụng";

    public static string Status(bool active) => active ? "Đang hoạt động" : "Ngừng hoạt động";

    public static string WarehouseType(string value) =>
        WarehouseTypeOptions.FirstOrDefault(item => item.Id == value)?.Label ?? "Loại khác";

    public static string LocationType(string value) =>
        LocationTypeOptions.FirstOrDefault(item => item.Id == value)?.Label ?? "Loại khác";

    public static string LayoutMode(string? value) => value switch
    {
        "MANAGED" => "Có sơ đồ kho",
        "UNMANAGED" => "Không dùng sơ đồ",
        _ => "Chưa thiết lập"
    };

    public static string FormatDateTime(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed))
        {
            return "Chưa có";
        }

        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
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

    public static string LocationLabel(string? id, string? code, string? name) =>
        id is null
            ? "Tồn chung"
            : string.Join(" · ", new[] { code, name }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public static string ModeTransition(string? fromMode, string targetMode) =>
        $"{LayoutMode(fromMode)} → {LayoutMode(targetMode)}";
}
