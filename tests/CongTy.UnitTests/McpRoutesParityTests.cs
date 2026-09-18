using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class McpRoutesParityTests
{
    [TestMethod]
    public void Service_UsesCanonicalReadOnlyEmployeeMcpReport()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeMcpReportingService.cs");

        StringAssert.Contains(service, "/api/reporting/employee-mcp");
        StringAssert.Contains(service, "GetDataAsync<EmployeeMcpDashboardData>");
        StringAssert.Contains(service, "yyyy-MM-dd");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Put", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Delete", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_WiresMcpRoutesUnderCompanySettingsWithCanonicalPermission()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.McpRoutes.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.McpRoutes.cs");
        var bootstrap = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "core.reporting.employee-mcp.read");
        StringAssert.Contains(shell, "settings.mcp-routes");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 50");
        StringAssert.Contains(shell, "IsCompanySettingsOpen = true");
        StringAssert.Contains(host, "text.Text == \"MCP và tuyến\"");
        StringAssert.Contains(host, "workspaceTabs.Items[50]");
        StringAssert.Contains(host, "Hiệu suất nhân viên thị trường");
        StringAssert.Contains(bootstrap, "WireMcpRoutesWorkspace()");
        StringAssert.Contains(xaml, "Text=\"MCP và tuyến\"");
    }

    [TestMethod]
    public void View_PreservesFiveCurrentWebTabsAndPrimaryActions()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingView.xaml");

        foreach (var label in new[]
        {
            "Tổng quan",
            "Tuyến và phiên",
            "Điểm bán và lượt ghé",
            "Nhu cầu và đơn hàng",
            "Hiệu quả hoạt động",
            "Phiên / tuyến",
            "Điểm kế hoạch / đã ghé",
            "Có mặt / lượt ghé",
            "Mở mã khách thành công",
            "Đơn Công Ty chính thức",
            "Hiệu suất theo tuyến",
            "100 phiên gần nhất trong bộ lọc",
            "Chất lượng dữ liệu và đối soát",
            "Đề nghị mở mã khách",
            "Danh mục nhân sự",
            "Tháng hiện tại"
        })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "ItemsSource=\"{Binding RouteRows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding SessionRows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding ActorRows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding QualityRows}\"");
    }

    [TestMethod]
    public void ViewModel_UsesBackendScopeAndCanonicalErrorsWithoutRecalculatingBusinessMetrics()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingPresentation.cs");

        StringAssert.Contains(viewModel, "core.reporting.employee-mcp.read");
        StringAssert.Contains(viewModel, "_service.GetAsync(from, to");
        StringAssert.Contains(viewModel, "_accessGeneration");
        StringAssert.Contains(viewModel, "_loadGeneration");
        StringAssert.Contains(viewModel, "CanonicalApiException");
        StringAssert.Contains(viewModel, "_report.Scope.Basis == \"EMPLOYEE_CODE\"");
        StringAssert.Contains(presentation, "Chưa liên kết hồ sơ nhân viên");
        StringAssert.Contains(presentation, "Số liệu phiên cần đối soát");
        Assert.IsFalse(viewModel.Contains("branchId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(viewModel.Contains("territoryId", StringComparison.OrdinalIgnoreCase));
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
          "fieldActors":[{"salesLabel":"NV001","employeeId":"00000000-0000-4000-8000-000000000001","employeeCode":"NV001","employeeName":"Nguyễn Văn A","sessionCount":"3","routeCount":"2","plannedOutletCount":"10","plannedVisitedOutletCount":"8","visitedOutletCount":"8","checkedInOutletCount":"7","visitCount":"9","orderIntentCount":"4","onboardingSubmittedCount":"2","onboardingConvertedCount":"1","coreSalesOrderCount":"3","plannedVisitRatePercent":"80.00","orderIntentConversionPercent":"50.00","coreOrderConversionPercent":"75.00"}],
          "routes":[{"routeId":"r1","routeCode":"T01","routeName":"Tuyến 01","area":"Quận 1","salesLabel":"NV001","employeeId":"00000000-0000-4000-8000-000000000001","employeeCode":"NV001","employeeName":"Nguyễn Văn A","sessionCount":"3","plannedOutletCount":"10","plannedVisitedOutletCount":"8","visitedOutletCount":"8","checkedInOutletCount":"7","orderIntentCount":"4","coreSalesOrderCount":"3","plannedVisitRatePercent":"80.00"}],
          "sessions":[{"sessionId":"s1","sessionDate":"2026-09-18","routeId":"r1","routeCode":"T01","routeName":"Tuyến 01","area":"Quận 1","salesLabel":"NV001","employeeId":"00000000-0000-4000-8000-000000000001","employeeCode":"NV001","employeeName":"Nguyễn Văn A","status":"active","plannedOutletCount":"10","plannedVisitedOutletCount":"8","visitedOutletCount":"8","checkedInOutletCount":"7","visitCount":"9","orderIntentCount":"4","onboardingSubmittedCount":"2","onboardingConvertedCount":"1","coreSalesOrderCount":"3","storedCounterMismatch":false,"openedAt":null,"closedAt":null}],
          "dataQuality":{"unmappedActors":[],"counterMismatches":[]}
        }
        """;

        var report = JsonSerializer.Deserialize<EmployeeMcpDashboardData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được hợp đồng MCP và tuyến.");

        Assert.AreEqual("employee-mcp", report.Family);
        Assert.AreEqual("EMPLOYEE_CODE", report.Scope.Basis);
        Assert.AreEqual("NV001", report.Scope.EmployeeCode);
        Assert.AreEqual("3", report.Summary.SessionCount);
        Assert.HasCount(1, report.FieldActors);
        Assert.HasCount(1, report.Routes);
        Assert.HasCount(1, report.Sessions);
        Assert.AreEqual("T01", report.Routes[0].RouteCode);
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
