using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ImportExportHistoryParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalImportExportHistoryRouteAndReadOnlyFilters()
    {
        var source = ReadRepoFile("src", "CongTy.ApiClient", "ImportExportHistoryService.cs");

        StringAssert.Contains(source, "\"/api/reporting/import-export-history\"");
        foreach (var filter in new[] { "from=", "to=", "direction=", "status=", "definitionKey=", "cursor=" })
            StringAssert.Contains(source, filter);

        StringAssert.Contains(source, "\"IMPORT\"");
        StringAssert.Contains(source, "\"EXPORT\"");
        foreach (var status in new[] { "queued", "running", "completed", "failed", "cancelled" })
            StringAssert.Contains(source, $"\"{status}\"");

        Assert.IsFalse(source.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Idempotency", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_UsesSharedHistoryPermissionAndCurrentWebLabels()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "ImportExportHistoryViewModel.cs");

        StringAssert.Contains(source, "\"core.reporting.audit-history.read\"");
        foreach (var text in new[]
        {
            "Nhập dữ liệu", "Xuất dữ liệu", "Xử lý dữ liệu",
            "Đang chờ", "Đang xử lý", "Hoàn tất", "Thất bại", "Đã hủy", "Trạng thái khác",
            "Sản phẩm", "Khách hàng", "Nhà cung cấp", "Loại sản phẩm", "Nhãn hàng",
            "Tồn kho", "Kho hàng", "Đơn bán hàng", "Đơn mua hàng", "Giá bán",
            "Công nợ khách hàng", "Dữ liệu nghiệp vụ",
            "Nhân viên nội bộ", "Tài khoản hệ thống",
            "Có lỗi cần xử lý", "Có kết quả", "Chưa có kết quả", "Chưa có số liệu"
        }) StringAssert.Contains(source, text);

        Assert.AreEqual("Nhập dữ liệu", ImportExportHistoryPresentation.Direction("IMPORT"));
        Assert.AreEqual("Xuất dữ liệu", ImportExportHistoryPresentation.Direction("EXPORT"));
        Assert.AreEqual("Hoàn tất", ImportExportHistoryPresentation.Status("completed"));
        Assert.AreEqual("Giá bán", ImportExportHistoryPresentation.Definition("pricing"));
    }

    [TestMethod]
    public void View_PreservesCurrentWebFiltersTableAndCollapsedTechnicalDetails()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Operations", "ImportExportHistoryView.xaml");

        foreach (var text in new[]
        {
            "Nhập/xuất dữ liệu và báo giá", "Lịch sử thay đổi", "Làm mới",
            "Từ ngày", "Đến ngày", "Loại thao tác", "Trạng thái", "Lọc lịch sử", "Xóa lọc",
            "Thời gian", "Nhóm dữ liệu", "Người thực hiện", "Kết quả",
            "Thông tin kỹ thuật", "Mã tác vụ:", "Mã truy vết:", "Nhóm dữ liệu hệ thống:",
            "Phiên bản cấu hình:", "Mã người thực hiện:", "Nguồn hệ thống:", "Mã lỗi:",
            "Chưa có lần nhập/xuất nào trong phạm vi lọc.", "Trang tiếp"
        }) StringAssert.Contains(view, text);

        StringAssert.Contains(view, "{Binding JobId, Mode=OneWay}");
        StringAssert.Contains(view, "{Binding TechnicalRequestId, Mode=OneWay}");
        StringAssert.Contains(view, "{Binding TechnicalFailureCode, Mode=OneWay}");
        Assert.IsFalse(view.Contains("backend", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains(">API<", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("NPP Core", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Pagination_ReusesCursorOnlyForAppliedFilter()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Operations", "ImportExportHistoryViewModel.cs");

        StringAssert.Contains(source, "HasMore && !FiltersDirty");
        StringAssert.Contains(source, "LoadPageAsync(_nextCursor, PageNumber + 1)");
        StringAssert.Contains(source, "_activeDirection = EmptyToNull(SelectedDirection.Value)");
        StringAssert.Contains(source, "_activeStatus = EmptyToNull(SelectedStatus.Value)");
        StringAssert.Contains(source, "definitionKey: null");
        StringAssert.Contains(source, "cursor: cursor");
        StringAssert.Contains(source, "if (FiltersDirty)");
        StringAssert.Contains(source, "await ApplyFilterAsync();");
    }

    [TestMethod]
    public void Shell_WiresImportExportHistoryAtWorkspace46AndEnablesCrossNavigation()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.ImportExportHistory.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.ImportExportHistory.cs");
        var hook = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var dataView = ReadRepoFile("src", "CongTy.Desktop", "Operations", "DataExchangeView.xaml");
        var auditView = ReadRepoFile("src", "CongTy.Desktop", "Operations", "AuditHistoryView.xaml");

        StringAssert.Contains(shell, "\"operations.import-export-history\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 46");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 46");
        StringAssert.Contains(host, "workspaceTabs.Items[46]");
        StringAssert.Contains(host, "\"LỊCH SỬ VẬN HÀNH\"");
        StringAssert.Contains(host, "\"Lịch sử nhập/xuất dữ liệu\"");
        StringAssert.Contains(hook, "WireAuditHistoryWorkspace();");
        StringAssert.Contains(hook, "WireImportExportHistoryWorkspace();");
        StringAssert.Contains(xaml, "Text=\"Lịch sử nhập/xuất\"");
        StringAssert.Contains(dataView, "Click=\"OpenImportExportHistory_OnClick\"");
        StringAssert.Contains(auditView, "Click=\"OpenImportExportHistory_OnClick\"");
        Assert.IsFalse(dataView.Contains("Lịch sử nhập/xuất chưa có trên bản máy tính.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentImportExportHistoryShape()
    {
        const string json = """
        {
          "generatedAt":"2026-09-18T01:00:00.000Z",
          "timezone":"Asia/Ho_Chi_Minh",
          "rows":[{
            "jobId":"11111111-1111-1111-1111-111111111111",
            "direction":"IMPORT",
            "definitionKey":"pricing",
            "definitionVersion":"2026-09-01",
            "format":"xlsx",
            "status":"completed",
            "actorId":"22222222-2222-2222-2222-222222222222",
            "employeeId":null,
            "sourceApp":"core-web",
            "requestId":"req_123",
            "rowCount":"25",
            "hasResult":true,
            "failureCode":null,
            "requestedAt":"2026-09-18T00:55:00.000Z",
            "startedAt":"2026-09-18T00:55:01.000Z",
            "completedAt":"2026-09-18T00:55:03.000Z"
          }],
          "page":{"hasMore":true,"nextCursor":"cursor_abc"}
        }
        """;

        var data = JsonSerializer.Deserialize<ImportExportHistoryData>(json)
            ?? throw new InvalidOperationException("Không đọc được hợp đồng lịch sử nhập/xuất.");

        Assert.AreEqual("Asia/Ho_Chi_Minh", data.Timezone);
        Assert.HasCount(1, data.Rows);
        Assert.AreEqual("IMPORT", data.Rows[0].Direction);
        Assert.AreEqual("pricing", data.Rows[0].DefinitionKey);
        Assert.AreEqual("25", data.Rows[0].RowCount);
        Assert.IsTrue(data.Rows[0].HasResult);
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
