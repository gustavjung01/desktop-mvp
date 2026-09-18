using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AuditHistoryParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalAuditHistoryRoute()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "AuditHistoryService.cs");
        StringAssert.Contains(source, "\"/api/reporting/audit-history\"");
        StringAssert.Contains(source, "from=");
        StringAssert.Contains(source, "to=");
        StringAssert.Contains(source, "cursor=");
        Assert.IsFalse(source.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Idempotency", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesExactPermissionAndCurrentWebLabels()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "AuditHistoryViewModel.cs");
        StringAssert.Contains(source, "\"core.reporting.audit-history.read\"");
        foreach (var text in new[]
        {
            "Tạo mới", "Xác nhận", "Hủy", "Xóa", "Đưa vào sử dụng", "Ngừng sử dụng",
            "Nhập dữ liệu", "Xuất dữ liệu", "Cập nhật", "Thao tác hệ thống",
            "Khách hàng", "Nhà cung cấp", "Sản phẩm", "Đơn bán hàng", "Đơn mua hàng",
            "Kho hàng", "Tồn kho", "Công nợ và thanh toán", "Vai trò và phân quyền",
            "Người dùng và nhân sự", "Dữ liệu nghiệp vụ",
            "Ứng dụng nhân viên thị trường", "Ứng dụng giao hàng", "Ứng dụng quản trị",
            "Hệ thống điều hành", "Hệ thống nội bộ",
            "Nhân viên nội bộ", "Tài khoản hệ thống",
            "Có nội dung thay đổi", "Ghi nhận thao tác"
        }) StringAssert.Contains(source, text);

        Assert.AreEqual("Ngừng sử dụng", AuditHistoryPresentation.Action("user_deactivate"));
        Assert.AreEqual("Đưa vào sử dụng", AuditHistoryPresentation.Action("user_activate"));
    }

    [TestMethod]
    public void View_PreservesCurrentWebFiltersTableAndCollapsedTechnicalDetails()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Operations", "AuditHistoryView.xaml");
        foreach (var text in new[]
        {
            "Lịch sử nhập/xuất dữ liệu", "Làm mới",
            "Từ ngày", "Đến ngày", "Lọc lịch sử", "Xóa lọc",
            "Thời điểm", "Thao tác", "Loại dữ liệu", "Người thực hiện",
            "Nguồn thao tác", "Nội dung thay đổi", "Thông tin kỹ thuật",
            "Mã thao tác:", "Loại dữ liệu:", "Mã bản ghi:", "Mã người thực hiện:",
            "Nguồn hệ thống:", "Mã truy vết:", "Chưa có thay đổi nào trong phạm vi lọc.",
            "Trang tiếp"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "{Binding TechnicalAction, Mode=OneWay}");
        StringAssert.Contains(view, "{Binding TechnicalRequestId, Mode=OneWay}");
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains(">API<", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("NPP Core", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Pagination_UsesCursorAndBlocksNextWhenFilterChanges()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "AuditHistoryViewModel.cs");
        StringAssert.Contains(source, "HasMore && !FiltersDirty");
        StringAssert.Contains(source, "LoadPageAsync(_nextCursor, PageNumber + 1)");
        StringAssert.Contains(source, "_activeFrom = FromDate");
        StringAssert.Contains(source, "_activeTo = ToDate");
        StringAssert.Contains(source, "if (FiltersDirty)");
        StringAssert.Contains(source, "await ApplyFilterAsync();");
    }

    [TestMethod]
    public void Shell_WiresAuditHistoryAfterDataExchangeAtWorkspace45()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.AuditHistory.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.AuditHistory.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "\"operations.audit-history\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 45");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 45");
        StringAssert.Contains(host, "workspaceTabs.Items[45]");
        StringAssert.Contains(host, "\"LỊCH SỬ VẬN HÀNH\"");
        StringAssert.Contains(host, "\"Lịch sử thay đổi hệ thống\"");
        StringAssert.Contains(hook, "WireDataExchangeWorkspace();");
        StringAssert.Contains(hook, "WireAuditHistoryWorkspace();");
        StringAssert.Contains(xaml, "Text=\"Lịch sử thay đổi\"");
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentAuditHistoryShape()
    {
        const string json = """
        {
          "generatedAt":"2026-09-18T01:00:00.000Z",
          "timezone":"Asia/Ho_Chi_Minh",
          "rows":[{
            "auditId":"11111111-1111-1111-1111-111111111111",
            "actorId":"22222222-2222-2222-2222-222222222222",
            "employeeId":null,
            "sourceApp":"core-web",
            "requestId":"req_123",
            "action":"product_update",
            "resourceType":"product",
            "resourceId":"33333333-3333-3333-3333-333333333333",
            "occurredAt":"2026-09-18T00:55:00.000Z",
            "hasBeforeData":true,
            "hasAfterData":true,
            "hasMetadata":true
          }],
          "page":{"hasMore":true,"nextCursor":"cursor_abc"}
        }
        """;

        var data = JsonSerializer.Deserialize<AuditHistoryData>(json)
            ?? throw new InvalidOperationException("Không đọc được hợp đồng lịch sử thay đổi.");

        Assert.AreEqual("Asia/Ho_Chi_Minh", data.Timezone);
        Assert.HasCount(1, data.Rows);
        Assert.AreEqual("product_update", data.Rows[0].Action);
        Assert.IsTrue(data.Rows[0].HasBeforeData);
        Assert.IsTrue(data.Page.HasMore);
        Assert.AreEqual("cursor_abc", data.Page.NextCursor);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        Assert.Fail($"Không tìm thấy tệp trong repo: {string.Join("/", parts)}");
        return string.Empty;
    }
}
