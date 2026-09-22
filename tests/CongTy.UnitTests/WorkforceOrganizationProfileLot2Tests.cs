using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceOrganizationProfileLot2Tests
{
    [TestMethod]
    public void Lot2_Contracts_DeserializeCanonicalOrganizationAndAssignmentFields()
    {
        const string json = """
        {
          "departments":[{
            "id":"00000000-0000-4000-8000-000000000101",
            "installation_id":"demo",
            "code":"KT",
            "name":"Kế toán",
            "parent_department_id":null,
            "parent_code":null,
            "parent_name":null,
            "is_active":true,
            "created_at":"2026-09-20T00:00:00.000Z",
            "updated_at":"2026-09-20T00:00:00.000Z"
          }],
          "positions":[{
            "id":"00000000-0000-4000-8000-000000000102",
            "installation_id":"demo",
            "code":"KT01",
            "name":"Kế toán viên",
            "department_id":"00000000-0000-4000-8000-000000000101",
            "department_code":"KT",
            "department_name":"Kế toán",
            "is_active":true,
            "created_at":"2026-09-20T00:00:00.000Z",
            "updated_at":"2026-09-20T00:00:00.000Z"
          }],
          "managers":[{
            "id":"00000000-0000-4000-8000-000000000103",
            "code":"NVQL",
            "full_name":"Nguyễn Quản Lý",
            "is_active":true,
            "department_id":"00000000-0000-4000-8000-000000000101",
            "department_code":"KT",
            "department_name":"Kế toán",
            "position_id":"00000000-0000-4000-8000-000000000102",
            "position_code":"KT01",
            "position_name":"Kế toán viên"
          }]
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var catalog = JsonSerializer.Deserialize<EmployeeOrganizationCatalogData>(json, options)
            ?? throw new InvalidOperationException("Không đọc được cơ cấu tổ chức.");

        Assert.HasCount(1, catalog.Departments);
        Assert.HasCount(1, catalog.Positions);
        Assert.HasCount(1, catalog.Managers);
        Assert.AreEqual("Kế toán", catalog.Departments[0].Name);
        Assert.AreEqual(catalog.Departments[0].Id, catalog.Positions[0].DepartmentId);
        Assert.AreEqual("Nguyễn Quản Lý", catalog.Managers[0].FullName);
    }

    [TestMethod]
    public void Lot2_ApiClient_UsesCanonicalEmployeeOrganizationRouteAndIdempotency()
    {
        var read = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryReadService.cs");
        var mutation = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryMutationService.cs");

        StringAssert.Contains(read, "GetDataAsync<EmployeeOrganizationCatalogData>");
        StringAssert.Contains(read, "\"/api/employees/organization\"");
        StringAssert.Contains(mutation, "PostIdempotentDataAsync<EmployeeOrganizationMutationRequest, EmployeeOrganizationMutationResult>");
        StringAssert.Contains(mutation, "\"/api/employees/organization\"");
        StringAssert.Contains(mutation, "idempotencyKey");
    }

    [TestMethod]
    public void Lot2_EmployeeMutation_CarriesDepartmentPositionManagerInsideEffectiveDatedAssignment()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "EmployeeDirectoryContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");
        var history = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.WorkforceHistory.cs");
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.Organization.cs");

        foreach (var field in new[] { "departmentId", "positionId", "managerEmployeeId" })
            StringAssert.Contains(contracts, $"JsonPropertyName(\"{field}\")");

        StringAssert.Contains(viewModel, "EmptyToNull(DraftDepartmentId)");
        StringAssert.Contains(viewModel, "EmptyToNull(DraftPositionId)");
        StringAssert.Contains(viewModel, "EmptyToNull(DraftManagerEmployeeId)");
        StringAssert.Contains(viewModel, "OrganizationAssignmentChanged(_editingEmployee)");
        StringAssert.Contains(history, "organizationChanged || DraftConfirmAssignment");
        StringAssert.Contains(organization, "employee-organization-save");
        StringAssert.Contains(organization, "ExpectedUpdatedAt: item.UpdatedAt");
    }

    [TestMethod]
    public void Lot2_View_KeepsOrganizationInsideEmployeeWorkspace()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        foreach (var label in new[]
        {
            "CƠ CẤU TỔ CHỨC",
            "Cơ cấu tổ chức",
            "Phòng/Bộ phận",
            "Vị trí công việc",
            "Quản lý trực tiếp",
            "Lịch sử điều chuyển",
            "Tên Phòng/Bộ phận",
            "Tên Vị trí",
            "Thuộc Phòng/Bộ phận",
            "THÊM PHÒNG/BỘ PHẬN",
            "THÊM VỊ TRÍ"
        })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "DraftDepartmentId");
        StringAssert.Contains(view, "DraftPositionId");
        StringAssert.Contains(view, "DraftManagerEmployeeId");
        StringAssert.Contains(view, "DepartmentRows");
        StringAssert.Contains(view, "PositionRows");
        StringAssert.Contains(view, "Chưa có Phòng/Bộ phận.");
        StringAssert.Contains(view, "Chưa có Vị trí công việc.");
        StringAssert.Contains(view, "OrganizationBusyText");

        Assert.IsFalse(shell.Contains("workforce.organization", StringComparison.Ordinal));
        Assert.IsFalse(shell.Contains("Cơ cấu tổ chức", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot2_History_ShowsDepartmentPositionAndManagerFromCanonicalAssignmentSnapshot()
    {
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryPresentation.cs");
        var history = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.WorkforceHistory.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");

        StringAssert.Contains(presentation, "DepartmentText");
        StringAssert.Contains(presentation, "PositionText");
        StringAssert.Contains(presentation, "ManagerText");
        StringAssert.Contains(history, "item.DepartmentName");
        StringAssert.Contains(history, "item.PositionName");
        StringAssert.Contains(history, "item.ManagerName");
        StringAssert.Contains(view, "Header=\"Phòng/Bộ phận\" Binding=\"{Binding DepartmentText}\"");
        StringAssert.Contains(view, "Header=\"Vị trí\" Binding=\"{Binding PositionText}\"");
        StringAssert.Contains(view, "Header=\"Quản lý trực tiếp\" Binding=\"{Binding ManagerText}\"");
    }

    [TestMethod]
    public void Lot2_Position_IsCanonicalWhileLegacyJobTitleIsOnlyCompatibilityHint()
    {
        var organization = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.Organization.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");

        StringAssert.Contains(organization, "DraftJobTitle = selected.Name");
        StringAssert.Contains(organization, "Chức danh cũ:");
        StringAssert.Contains(view, "LegacyJobTitleHint");
        Assert.IsFalse(view.Contains("Text=\"{Binding DraftJobTitle, Mode=TwoWay", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot2_Audit_LocksWebContractAndNoBackendDuplication()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT2_ORGANIZATION_PROFILE_AUDIT.md");

        StringAssert.Contains(audit, "shared.hr_departments");
        StringAssert.Contains(audit, "shared.hr_positions");
        StringAssert.Contains(audit, "cùng shared.employee_assignments");
        StringAssert.Contains(audit, "manager_employee_id");
        StringAssert.Contains(audit, "Không tạo lịch sử tổ chức song song");
        StringAssert.Contains(audit, "không sửa Web/backend/DB/migration");
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
