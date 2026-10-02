using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class McpRoutesParityTests
{
    [TestMethod]
    public void EmployeePerformance_RemainsCanonicalReadOnlyReport()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeMcpReportingService.cs");

        StringAssert.Contains(service, "/api/reporting/employee-mcp");
        StringAssert.Contains(service, "GetDataAsync<EmployeeMcpDashboardData>");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Put", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Delete", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_SeparatesEmployeePerformanceFromCompanyRouteSettings()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.McpRoutes.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.McpRoutes.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "core.reporting.employee-mcp.read");
        StringAssert.Contains(shell, "workforce.employee-mcp-performance");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 50");
        StringAssert.Contains(shell, "mcp.route.write");
        StringAssert.Contains(shell, "settings.mcp-routes");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 91");
        StringAssert.Contains(host, "workspaceTabs.Items[50]");
        StringAssert.Contains(host, "workspaceTabs.Items[91]");
        StringAssert.Contains(host, "McpRouteSettingsBoundaryView");
        StringAssert.Contains(xaml, "Click=\"EmployeeMcpReporting_OnClick\"");
        StringAssert.Contains(xaml, "Click=\"McpRouteSettings_OnClick\"");
        StringAssert.Contains(xaml, "Hiệu suất nhân viên thị trường");
        StringAssert.Contains(xaml, "MCP và tuyến");
    }

    [TestMethod]
    public void DesktopRouteSettings_FailClosedWithoutServerCredential()
    {
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.McpRoutes.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.McpRoutes.cs");
        var boundary = ReadRepoFile("src", "CongTy.Desktop", "Settings", "McpRouteSettingsBoundaryView.xaml");

        foreach (var source in new[] { host, shell, boundary })
        {
            Assert.IsFalse(source.Contains("MCP_API_SERVER_TOKEN", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("X-Backend-Token", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("BACKEND_API_TOKEN", StringComparison.Ordinal));
        }

        StringAssert.Contains(boundary, "Chưa khả dụng trên Desktop");
        StringAssert.Contains(boundary, "Desktop không lưu hoặc sử dụng thông tin truy cập máy chủ riêng");
        StringAssert.Contains(boundary, "contract dành cho workforce client");
        Assert.IsFalse(boundary.Contains("Tạo tuyến", StringComparison.Ordinal));
        Assert.IsFalse(boundary.Contains("Xóa tuyến", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmployeePerformanceView_PreservesCanonicalReportTabs()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingView.xaml");
        foreach (var label in new[]
        {
            "Tổng quan",
            "Tuyến và phiên",
            "Điểm bán và lượt ghé",
            "Nhu cầu và đơn hàng",
            "Hiệu quả hoạt động",
            "Chất lượng dữ liệu và đối soát",
            "Đề nghị mở mã khách",
            "Danh mục nhân sự"
        })
            StringAssert.Contains(view, label);
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentEmployeeMcpWireShape()
    {
        const string json = """
        {
          "family":"employee-mcp",
          "generatedAt":"2026-09-18T04:00:00.000Z",
          "timezone":"Asia/Ho_Chi_Minh",
          "filters":{"from":"2026-09-01","to":"2026-09-18"},
          "scope":{"basis":"EMPLOYEE_CODE","employeeId":"00000000-0000-4000-8000-000000000001","employeeCode":"NV001"},
          "basis":{"identity":"i","territory":"t","visits":"v","conversion":"c","customerBoundary":"b","adminReuse":"a"},
          "summary":{"sessionCount":"3","routeCount":"2","plannedOutletCount":"10","visitedOutletCount":"8","plannedVisitRatePercent":"80.00"},
          "fieldActors":[],
          "routes":[],
          "sessions":[],
          "dataQuality":{"unmappedActors":[],"counterMismatches":[]}
        }
        """;

        var report = JsonSerializer.Deserialize<EmployeeMcpDashboardData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được hợp đồng báo cáo MCP.");

        Assert.AreEqual("employee-mcp", report.Family);
        Assert.AreEqual("EMPLOYEE_CODE", report.Scope.Basis);
        Assert.AreEqual("NV001", report.Scope.EmployeeCode);
        Assert.AreEqual("3", report.Summary.SessionCount);
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
