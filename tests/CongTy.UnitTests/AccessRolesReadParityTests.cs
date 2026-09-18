using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Access;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AccessRolesReadParityTests
{
    [TestMethod]
    public void Lot1_Service_IsStrictlyReadOnlyAndUsesCanonicalRoutes()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AccessRoleReadService.cs");

        StringAssert.Contains(service, "\"/api/access/permissions\"");
        StringAssert.Contains(service, "\"/api/access/roles?limit=1000&offset=0\"");
        StringAssert.Contains(service, "\"/api/access/roles/{Uri.EscapeDataString(roleId.Trim())}\"");
        StringAssert.Contains(service, "GetDataAsync<AccessPermissionData[]>");
        StringAssert.Contains(service, "GetDataAsync<AccessRoleData[]>");
        Assert.IsFalse(service.Contains("Post", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Patch", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot1_Shell_UsesWorkspace51AndRoleReadPermission()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.AccessRoles.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.AccessRoles.cs");
        var bootstrap = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");

        StringAssert.Contains(shell, "core.role.read");
        StringAssert.Contains(shell, "SetSelectedNavigation(\"access.roles\")");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 51");
        StringAssert.Contains(shell, "IsAccessOpen = true");
        StringAssert.Contains(host, "text.Text == \"Vai trò và phân quyền\"");
        StringAssert.Contains(host, "workspaceTabs.Items[51]");
        StringAssert.Contains(bootstrap, "WireAccessRolesWorkspace()");
    }

    [TestMethod]
    public void Lot1_View_PreservesCurrentWebReadSurfaceAndEditorMatrix()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesView.xaml");

        foreach (var text in new[]
        {
            "Tổng vai trò",
            "Đang sử dụng",
            "Danh mục quyền",
            "Tìm kiếm vai trò",
            "Tất cả trạng thái",
            "Vai trò và tập quyền",
            "Mã vai trò",
            "Tên vai trò",
            "Quyền",
            "Trạng thái",
            "Cập nhật",
            "Thao tác",
            "Mẫu quyền gợi ý",
            "Mã vai trò",
            "Tên vai trò",
            "Mô tả",
            "Yêu cầu mã xác nhận khi đăng nhập trên web/ứng dụng",
            "Chọn quyền theo nhóm chức năng"
        })
            StringAssert.Contains(view, text);

        StringAssert.Contains(view, "IsEnabled=\"{Binding CanPersist}\"");
        Assert.IsFalse(view.Contains("Lô 1", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Lô 2", StringComparison.Ordinal));
        StringAssert.Contains(view, "Thay đổi trên form này chưa được lưu.");
    }

    [TestMethod]
    public void Lot1_Presets_MatchCurrentWebCatalogAndExcludeVerificationPermissions()
    {
        var labels = RolePresetCatalog.Options.Skip(1).Select(item => item.Label).ToArray();

        Assert.HasCount(12, labels);
        CollectionAssert.Contains(labels, "Quản trị hệ thống");
        CollectionAssert.Contains(labels, "Quản lý / Kiểm soát");
        CollectionAssert.Contains(labels, "Quản lý bán hàng");
        CollectionAssert.Contains(labels, "Nhân viên bán hàng");
        CollectionAssert.Contains(labels, "Mua hàng");
        CollectionAssert.Contains(labels, "Quản lý kho");
        CollectionAssert.Contains(labels, "Nhân viên kho");
        CollectionAssert.Contains(labels, "Kế toán phải thu / phải trả");
        CollectionAssert.Contains(labels, "Điều phối giao hàng");
        CollectionAssert.Contains(labels, "Tài xế / Giao hàng");
        CollectionAssert.Contains(labels, "Nhân viên thị trường");
        CollectionAssert.Contains(labels, "Quản lý giao vận");

        var permissions = new[]
        {
            new AccessPermissionData { PermissionKey = "core.role.read" },
            new AccessPermissionData { PermissionKey = "core.audit-outbox.test.write" },
            new AccessPermissionData { PermissionKey = "core.idempotency.test.write" },
            new AccessPermissionData { PermissionKey = "core.storage.r2.test.write" }
        };
        var owner = RolePresetCatalog.Resolve("owner-admin", permissions);

        CollectionAssert.Contains(owner.ToArray(), "core.role.read");
        CollectionAssert.DoesNotContain(owner.ToArray(), "core.audit-outbox.test.write");
        CollectionAssert.DoesNotContain(owner.ToArray(), "core.idempotency.test.write");
        CollectionAssert.DoesNotContain(owner.ToArray(), "core.storage.r2.test.write");
    }

    [TestMethod]
    public void Contracts_DeserializeCurrentRoleAndPermissionWireShape()
    {
        const string roleJson = """
        {
          "id":"00000000-0000-4000-8000-000000000001",
          "installation_id":"00000000-0000-4000-8000-000000000002",
          "code":"SALES",
          "name":"Bán hàng",
          "description":"Vai trò bán hàng",
          "is_active":true,
          "web_login_challenge_required":false,
          "created_at":"2026-09-18T00:00:00.000Z",
          "updated_at":"2026-09-18T01:00:00.000Z",
          "created_by":null,
          "updated_by":null,
          "permission_keys":["core.sales-order.read"]
        }
        """;
        const string permissionJson = """
        {
          "permission_key":"core.sales-order.read",
          "module":"sales",
          "label":"Xem đơn bán hàng",
          "description":"Cho phép đọc đơn bán hàng.",
          "is_system":true,
          "created_at":"2026-09-18T00:00:00.000Z"
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var role = JsonSerializer.Deserialize<AccessRoleData>(roleJson, options)
            ?? throw new InvalidOperationException("Không đọc được vai trò.");
        var permission = JsonSerializer.Deserialize<AccessPermissionData>(permissionJson, options)
            ?? throw new InvalidOperationException("Không đọc được quyền.");

        Assert.AreEqual("SALES", role.Code);
        Assert.IsTrue(role.IsActive);
        Assert.IsFalse(role.WebLoginChallengeRequired);
        Assert.HasCount(1, role.PermissionKeys);
        Assert.AreEqual("core.sales-order.read", permission.PermissionKey);
        Assert.AreEqual("sales", permission.Module);
    }

    [TestMethod]
    public void Lot1_ViewModel_SeparatesReadAndWritePermissionsWithoutMutation()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesViewModel.cs");

        StringAssert.Contains(viewModel, "core.permission.read");
        StringAssert.Contains(viewModel, "core.role.read");
        StringAssert.Contains(viewModel, "core.role.write");
        StringAssert.Contains(viewModel, "CanPersist => false");
        StringAssert.Contains(viewModel, "RolePresetCatalog.Resolve");
        StringAssert.Contains(viewModel, "Đang sử dụng");
        StringAssert.Contains(viewModel, "Ngừng sử dụng");
        Assert.IsFalse(viewModel.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
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
