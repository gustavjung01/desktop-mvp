using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Access;

namespace CongTy.UnitTests;

[TestClass]
public sealed class UserDirectoryReadParityTests
{
    [TestMethod]
    public void EmployeeBranchLookup_UsesCanonicalBackendRoute()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryReadService.cs");

        StringAssert.Contains(service, "\"/api/branches?limit=1000&offset=0\"");
        Assert.IsFalse(service.Contains("/api/organization/branches", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot1_UserService_IsReadOnlyAndUsesCanonicalRoutes()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "UserDirectoryReadService.cs");

        StringAssert.Contains(service, "\"/api/access/users?limit=1000&offset=0\"");
        StringAssert.Contains(service, "\"/api/access/users/{Uri.EscapeDataString(userId.Trim())}\"");
        StringAssert.Contains(service, "GetDataAsync<AccessUserData[]>");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Put", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot1_Shell_UsesDedicatedWorkspaceAndUserReadPermission()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.UserDirectory.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.UserDirectory.cs");
        var bootstrap = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");

        StringAssert.Contains(shell, "core.user.read");
        StringAssert.Contains(shell, "SetSelectedNavigation(\"access.users\")");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = WorkspaceSlots.UserDirectory");
        Assert.IsFalse(host.Contains("sidebarButton", StringComparison.Ordinal));
        StringAssert.Contains(host, "workspaceTabs.Items[WorkspaceSlots.UserDirectory]");
        StringAssert.Contains(bootstrap, "WireUserDirectoryWorkspace()");
    }

    [TestMethod]
    public void Lot1_View_PreservesWebAccountSurfaceAndChildTabs()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryView.xaml");

        foreach (var text in new[]
        {
            "Tài khoản",
            "Phạm vi chi nhánh &amp; kho",
            "Tổng tài khoản",
            "Đang hoạt động",
            "Ngừng sử dụng",
            "Tìm kiếm",
            "Trạng thái",
            "Tên đăng nhập",
            "Nhân sự",
            "Vai trò",
            "Cập nhật",
            "Hành động"
        })
            StringAssert.Contains(view, text);

        StringAssert.Contains(view, "IsEnabled=\"{Binding CanOpenCreate}\"");
        StringAssert.Contains(view, "Click=\"Save_OnClick\"");
        StringAssert.Contains(view, "Click=\"Toggle_OnClick\"");
    }

    [TestMethod]
    public void Lot1_ViewModel_SeparatesRequiredReadPermissionsAndHasNoMutation()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "core.user.read");
        StringAssert.Contains(viewModel, "core.user.write");
        StringAssert.Contains(viewModel, "core.user-role.write");
        StringAssert.Contains(viewModel, "core.employee.read");
        StringAssert.Contains(viewModel, "core.role.read");
        StringAssert.Contains(viewModel, "CanOpenCreate");
        StringAssert.Contains(viewModel, "Không có người dùng phù hợp.");
    }

    [TestMethod]
    public void Contract_DeserializesCurrentAccessUserWireShape()
    {
        const string json = """
        {
          "id":"00000000-0000-4000-8000-000000000001",
          "installation_id":"00000000-0000-4000-8000-000000000002",
          "employee_id":"00000000-0000-4000-8000-000000000003",
          "employee_code":"NV001",
          "employee_full_name":"Nguyễn Văn An",
          "login_name":"an.nguyen",
          "is_active":true,
          "created_at":"2026-09-18T00:00:00.000Z",
          "updated_at":"2026-09-18T01:00:00.000Z",
          "created_by":"owner",
          "updated_by":"owner",
          "role_ids":["00000000-0000-4000-8000-000000000004"],
          "branch_ids":[],
          "warehouse_ids":[],
          "owner_kind":null
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var user = JsonSerializer.Deserialize<AccessUserData>(json, options)
            ?? throw new InvalidOperationException("Không đọc được người dùng.");

        Assert.AreEqual("an.nguyen", user.LoginName);
        Assert.AreEqual("NV001", user.EmployeeCode);
        Assert.IsTrue(user.IsActive);
        Assert.HasCount(1, user.RoleIds);
    }

    [TestMethod]
    public void Lot1_Audit_SeparatesAccountMutationAndScopes()
    {
        var audit = ReadRepoFile("docs", "parity", "ACCESS_USERS_LOT1_AUDIT.md");

        StringAssert.Contains(audit, "Mutation tài khoản thuộc Lô 2");
        StringAssert.Contains(audit, "phạm vi chi nhánh & kho thuộc Lô 3");
        StringAssert.Contains(audit, "Lô 1 không gọi POST/PATCH/PUT");
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
