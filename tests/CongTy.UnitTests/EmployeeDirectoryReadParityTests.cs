using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Access;

namespace CongTy.UnitTests;

[TestClass]
public sealed class EmployeeDirectoryReadParityTests
{
    [TestMethod]
    public void Lot1_Service_IsReadOnlyAndUsesCanonicalEmployeeAndBranchRoutes()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryReadService.cs");

        StringAssert.Contains(service, "ListAllAsync<EmployeeDirectoryData>(\"/api/employees\", cancellationToken)");
        StringAssert.Contains(service, "$\"{path}{separator}limit={limit}&offset={offset}\"");
        StringAssert.Contains(service, "\"/api/employees/{Uri.EscapeDataString(employeeId.Trim())}\"");
        StringAssert.Contains(service, "\"/api/branches?limit=1000&offset=0\"");
        StringAssert.Contains(service, "GetDataAsync<T[]>");
        StringAssert.Contains(service, "GetDataAsync<EmployeeDirectoryBranchData[]>");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot1_Shell_UsesWorkspace52AndEmployeeReadPermission()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.EmployeeDirectory.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.EmployeeDirectory.cs");
        var bootstrap = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        StringAssert.Contains(shell, "core.employee.read");
        StringAssert.Contains(shell, "SetSelectedNavigation(\"workforce.employees\")");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 52");
        StringAssert.Contains(shell, "IsWorkforceOpen = true");
        Assert.IsFalse(host.Contains("sidebarButton", StringComparison.Ordinal));
        StringAssert.Contains(host, "workspaceTabs.Items[52]");
        StringAssert.Contains(bootstrap, "WireEmployeeDirectoryWorkspace()");
        StringAssert.Contains(xaml, "Click=\"EmployeeDirectory_OnClick\"");
    }

    [TestMethod]
    public void Lot1_View_PreservesCurrentWebDirectorySurface()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");

        foreach (var text in new[]
        {
            "Tổng hồ sơ",
            "Đang làm việc",
            "Đã phân công",
            "Tìm kiếm nhân sự",
            "Trạng thái làm việc",
            "Đơn vị công tác",
            "Hồ sơ và đơn vị công tác",
            "Mã nhân sự",
            "Họ và tên",
            "Liên hệ",
            "Trạng thái",
            "Cập nhật",
            "Thao tác",
            "Vị trí công việc",
            "Phòng/Bộ phận",
            "Quản lý trực tiếp",
            "Cơ cấu tổ chức",
            "Chi nhánh công tác",
            "Số điện thoại",
            "Email công việc"
        })
            StringAssert.Contains(view, text);

        StringAssert.Contains(view, "IsEnabled=\"{Binding CanPersist}\"");
        StringAssert.Contains(view, "Click=\"Create_OnClick\"");
        StringAssert.Contains(view, "Click=\"Edit_OnClick\"");
        StringAssert.Contains(view, "Content=\"{Binding ToggleActionText}\"");

        Assert.IsFalse(
            view.Contains(
                "<ScrollViewer VerticalScrollBarVisibility=\"Auto\"\n                      HorizontalScrollBarVisibility=\"Disabled\">\n            <StackPanel Margin=\"0,0,4,12\">",
                StringComparison.Ordinal),
            "Danh mục nhân sự không được cuộn toàn bộ workspace.");

        StringAssert.Contains(view, "<RowDefinition Height=\"*\" />");
        StringAssert.Contains(view, "<Border Grid.Row=\"5\" Margin=\"0,10,0,0\" Style=\"{StaticResource OfficeCardStyle}\">");
        StringAssert.Contains(view, "<DataGrid Grid.Row=\"1\"");
        StringAssert.Contains(view, "ScrollViewer.VerticalScrollBarVisibility=\"Auto\"");
        StringAssert.Contains(view, "ScrollViewer.CanContentScroll=\"True\"");
    }

    [TestMethod]
    public void Lot1_ViewModel_SeparatesReadWriteAndBranchPermissionsWithoutMutation()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "core.employee.read");
        StringAssert.Contains(viewModel, "core.employee.write");
        StringAssert.Contains(viewModel, "core.branch.read");
        StringAssert.Contains(viewModel, "Tất cả trạng thái");
        StringAssert.Contains(viewModel, "Đang làm việc");
        StringAssert.Contains(viewModel, "Ngừng làm việc");
        StringAssert.Contains(viewModel, "Tất cả chi nhánh");
        StringAssert.Contains(viewModel, "Chưa phân công");
        StringAssert.Contains(viewModel, "Đưa trở lại làm việc");
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentEmployeeAndBranchWireShape()
    {
        const string employeeJson = """
        {
          "id":"00000000-0000-4000-8000-000000000001",
          "installation_id":"00000000-0000-4000-8000-000000000002",
          "code":"NV001",
          "full_name":"Nguyễn Văn An",
          "job_title":"Kế toán",
          "phone":"0900000000",
          "email":"an@example.com",
          "branch_id":"00000000-0000-4000-8000-000000000003",
          "is_active":true,
          "created_at":"2026-09-18T00:00:00.000Z",
          "updated_at":"2026-09-18T01:00:00.000Z",
          "created_by":"owner",
          "updated_by":"owner"
        }
        """;
        const string branchJson = """
        {
          "id":"00000000-0000-4000-8000-000000000003",
          "code":"CN01",
          "name":"Chi nhánh 01",
          "is_active":true
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var employee = JsonSerializer.Deserialize<EmployeeDirectoryData>(employeeJson, options)
            ?? throw new InvalidOperationException("Không đọc được hồ sơ nhân sự.");
        var branch = JsonSerializer.Deserialize<EmployeeDirectoryBranchData>(branchJson, options)
            ?? throw new InvalidOperationException("Không đọc được chi nhánh.");

        Assert.AreEqual("NV001", employee.Code);
        Assert.AreEqual("Nguyễn Văn An", employee.FullName);
        Assert.IsTrue(employee.IsActive);
        Assert.AreEqual("CN01 · Chi nhánh 01", EmployeeDirectoryPresentation.BranchLabel(branch));
        Assert.AreEqual("18/09/2026 08:00", EmployeeDirectoryPresentation.DateTimeText(employee.UpdatedAt));
    }

    [TestMethod]
    public void Lot1_Audit_KeepsEmployeeIdentitySeparateFromUserIdentityAndMutation()
    {
        var audit = ReadRepoFile("docs", "parity", "ACCESS_EMPLOYEES_LOT1_AUDIT.md");

        StringAssert.Contains(audit, "Nhân sự là hồ sơ nghiệp vụ, không phải tài khoản đăng nhập.");
        StringAssert.Contains(audit, "POST /api/employees");
        StringAssert.Contains(audit, "PATCH /api/employees/:id");
        StringAssert.Contains(audit, "expectedUpdatedAt");
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