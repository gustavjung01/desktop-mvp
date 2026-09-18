using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed record RolePresetOption(string Id, string Label, string Description)
{
    public override string ToString() => Label;
}

public static class RolePresetCatalog
{
    private static readonly HashSet<string> InternalVerificationPermissions =
        new(StringComparer.Ordinal)
        {
            "core.audit-outbox.test.write",
            "core.idempotency.test.write",
            "core.storage.r2.test.write"
        };

    public static IReadOnlyList<RolePresetOption> Options { get; } =
    [
        new("", "Không dùng mẫu — tự chọn quyền", "Mẫu chỉ tích sẵn quyền để tham khảo. Anh/chị vẫn thêm hoặc bỏ từng quyền trước khi lưu và tự đặt tên vai trò."),
        new("owner-admin", "Quản trị hệ thống", "Gợi ý toàn bộ quyền nghiệp vụ hiện có; vẫn có thể bỏ từng quyền trước khi lưu."),
        new("manager-auditor", "Quản lý / Kiểm soát", "Gợi ý quyền đọc và báo cáo để quan sát, đối soát, không mặc định cấp quyền ghi."),
        new("sales-manager", "Quản lý bán hàng", "Đơn bán hàng, khách hàng, giá, đề nghị mở mã khách và báo cáo bán hàng."),
        new("sales-rep", "Nhân viên bán hàng", "Đọc dữ liệu bán hàng, tạo/sửa đơn nháp và gửi đề nghị mở mã khách."),
        new("purchasing", "Mua hàng", "Nhà cung cấp, bảng giá mua, đơn mua hàng, nhận hàng và trả nhà cung cấp."),
        new("warehouse-manager", "Quản lý kho", "Tồn kho, chuyển kho, kiểm kê, điều chỉnh, giá vốn và chuẩn bị hàng."),
        new("warehouse-operator", "Nhân viên kho", "Đọc kho, soạn/đóng gói, nhận chuyển kho và ghi số đếm kiểm kê."),
        new("accounting", "Kế toán phải thu / phải trả", "Phải thu, phải trả, thanh toán, phân bổ, thu hộ khi giao hàng (COD) và báo cáo công nợ."),
        new("dispatcher", "Điều phối giao hàng", "Tuyến, xe, tài xế, lập/xếp chuyến và điều phối xuất phát."),
        new("driver-delivery", "Tài xế / Giao hàng", "Chuyến được giao, kết quả giao, bằng chứng giao hàng và thu/bàn giao COD."),
        new("mcp-field", "Nhân viên thị trường", "Đi tuyến, ghi nhận thị trường, mở mã khách và tạo đơn Công Ty theo phạm vi được giao; không có quyền cấu hình MCP."),
        new("logistics-manager", "Quản lý giao vận", "Toàn bộ điều phối, giao nhận, đối soát chuyến và báo cáo giao vận/COD.")
    ];

    public static IReadOnlySet<string> Resolve(
        string presetId,
        IEnumerable<AccessPermissionData> permissions)
    {
        var available = permissions
            .Where(permission => !InternalVerificationPermissions.Contains(permission.PermissionKey))
            .ToArray();

        bool Predicate(AccessPermissionData permission)
        {
            var key = permission.PermissionKey;
            return presetId switch
            {
                "owner-admin" => true,
                "manager-auditor" => IsReadLike(key) || key.StartsWith("core.reporting.", StringComparison.Ordinal),
                "sales-manager" => MatchesPrefix(key, "core.sales-order.", "core.customer-onboarding.")
                    || MatchesKey(key,
                        "core.customer.read", "core.customer.write", "core.product.read", "core.price.read",
                        "core.fulfillment.read", "core.fulfillment.configure-backorder", "core.reporting.sales.read"),
                "sales-rep" => MatchesKey(key,
                    "core.customer.read", "core.product.read", "core.price.read",
                    "core.sales-order.read", "core.sales-order.create", "core.sales-order.update-draft",
                    "core.customer-onboarding.read", "core.customer-onboarding.submit"),
                "purchasing" => MatchesPrefix(key,
                        "core.supplier.", "core.supplier-purchase-price.", "core.purchase-order.",
                        "core.goods-receipt.", "core.supplier-return.")
                    || key == "core.reporting.purchasing.read",
                "warehouse-manager" => MatchesPrefix(key,
                        "core.inventory.", "core.inventory-transfer.", "core.inventory-adjustment.",
                        "core.inventory-cost.", "core.stocktake.", "core.fulfillment.", "core.delivery-order.")
                    || key == "core.reporting.inventory.read",
                "warehouse-operator" => MatchesKey(key,
                    "core.inventory.read", "core.inventory-tracking-policy.read", "core.inventory-lot.read",
                    "core.inventory-transfer.read", "core.inventory-transfer.receive",
                    "core.stocktake.read", "core.stocktake.count",
                    "core.fulfillment.read", "core.fulfillment.allocate", "core.fulfillment.pick", "core.fulfillment.pack",
                    "core.delivery-order.read"),
                "accounting" => MatchesPrefix(key,
                        "core.receivable.", "core.receivable-allocation.", "core.customer-payment.",
                        "core.customer-return-credit.", "core.customer-refund.", "core.payable.",
                        "core.payable-allocation.", "core.supplier-payment.", "core.cod-")
                    || MatchesKey(key, "core.reporting.aging.read", "core.reporting.cod.read"),
                "dispatcher" => MatchesPrefix(key, "core.logistics-route.", "core.vehicle.", "core.driver-profile.")
                    || MatchesKey(key,
                        "core.delivery-trip.read", "core.delivery-trip.create", "core.delivery-trip.plan",
                        "core.delivery-trip.assign", "core.delivery-trip.lock", "core.delivery-trip.dispatch",
                        "core.delivery-order.read", "core.reporting.logistics.read"),
                "driver-delivery" => MatchesKey(key,
                    "core.delivery-trip.driver-read", "core.delivery-attempt.read", "core.delivery-attempt.record",
                    "core.pod.read", "core.pod.attach", "core.cod-collection.read", "core.cod-collection.record",
                    "core.cod-handover.read", "core.cod-handover.create"),
                "mcp-field" => MatchesKey(key,
                    "core.customer.read", "core.product.read", "core.price.read",
                    "core.customer-onboarding.read", "core.customer-onboarding.submit",
                    "core.sales-order.read", "core.sales-order.create",
                    "mcp.session.write", "mcp.session-customer.write",
                    "mcp.order.write", "mcp.test.write", "mcp.report.write", "mcp.followup.write",
                    "mcp.sales-order.read", "mcp.sales-order.create"),
                "logistics-manager" => MatchesPrefix(key,
                        "core.logistics-route.", "core.vehicle.", "core.driver-profile.", "core.delivery-trip.",
                        "core.delivery-attempt.", "core.pod.", "core.delivery-order.", "core.cod-")
                    || MatchesKey(key, "core.reporting.logistics.read", "core.reporting.cod.read"),
                _ => false
            };
        }

        return available.Where(Predicate)
            .Select(permission => permission.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool IsReadLike(string key) =>
        key.EndsWith(".read", StringComparison.Ordinal)
        || key.EndsWith(".read-all", StringComparison.Ordinal)
        || key.EndsWith(".driver-read", StringComparison.Ordinal)
        || key.EndsWith(".reconciliation-read", StringComparison.Ordinal);

    private static bool MatchesPrefix(string key, params string[] prefixes) =>
        prefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));

    private static bool MatchesKey(string key, params string[] keys) =>
        keys.Contains(key, StringComparer.Ordinal);
}
