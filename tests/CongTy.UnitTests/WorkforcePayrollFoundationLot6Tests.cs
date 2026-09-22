using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforcePayrollFoundationLot6Tests
{
    [TestMethod]
    public void Lot6_Contracts_KeepMoneyAsExactStringsAndDeserializeFoundation()
    {
        const string json = """
        {
          "attendanceSources": [{
            "id":"00000000-0000-4000-8000-000000000001",
            "branch_id":null,
            "scope_key":"COMPANY",
            "period_start":"2026-09-01",
            "period_end":"2026-09-30",
            "revision":2,
            "source_fingerprint":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "branch_name":null
          }],
          "periods": [{
            "id":"00000000-0000-4000-8000-000000000002",
            "attendance_period_id":"00000000-0000-4000-8000-000000000001",
            "attendance_revision":2,
            "attendance_source_fingerprint":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "branch_id":null,
            "scope_key":"COMPANY",
            "period_start":"2026-09-01",
            "period_end":"2026-09-30",
            "status":"AGGREGATING",
            "currency_code":"VND"
          }],
          "selectedPeriod": null,
          "employees": [{
            "id":"00000000-0000-4000-8000-000000000003",
            "code":"NV001",
            "full_name":"Nguyễn Văn An",
            "branch_id":null
          }],
          "periodEmployees": [],
          "componentTypes": [],
          "salaryProfiles": [{
            "id":"00000000-0000-4000-8000-000000000004",
            "employee_id":"00000000-0000-4000-8000-000000000003",
            "employee_code":"NV001",
            "employee_name":"Nguyễn Văn An",
            "monthly_salary":"12345678.90",
            "currency_code":"VND",
            "effective_from":"2026-09-01",
            "effective_to":null,
            "note":null
          }],
          "fixedComponents": [],
          "periodComponents": [],
          "asOfDate":"2026-09-22",
          "capabilities":{"canManage":true,"canClose":false,"canAdjust":false,"canExport":false}
        }
        """;

        var value = JsonSerializer.Deserialize<PayrollFoundationData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được contract payroll.");

        Assert.HasCount(1, value.Periods);
        Assert.HasCount(1, value.SalaryProfiles);
        Assert.AreEqual("12345678.90", value.SalaryProfiles[0].MonthlySalary);
        Assert.AreEqual("12.345.678,9 ₫", PayrollFoundationPresentation.MoneyText(value.SalaryProfiles[0].MonthlySalary));
        Assert.IsTrue(value.Capabilities.CanManage);
    }

    [TestMethod]
    public void Lot6_ApiClient_UsesSinglePayrollRouteAndCanonicalIdempotentPost()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "PayrollFoundationService.cs");

        StringAssert.Contains(service, "/api/workforce/payroll");
        StringAssert.Contains(service, "?periodId=");
        StringAssert.Contains(service, "PostIdempotentDataAsync<TRequest, TResponse>");

        foreach (var request in new[]
        {
            "CreatePayrollPeriodRequest",
            "SavePayrollSalaryRequest",
            "CreatePayrollComponentTypeRequest",
            "AssignPayrollFixedComponentRequest",
            "AddPayrollPeriodComponentRequest"
        })
            StringAssert.Contains(service, request);
    }

    [TestMethod]
    public void Lot6_Desktop_ActivatesExactlyOnePayrollWorkspaceFromWorkforceSidebar()
    {
        var shellXaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.PayrollFoundation.cs");
        var wire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.PayrollFoundation.cs");
        var hostWire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");

        Assert.AreEqual(1, Count(shellXaml, "<TextBlock Text="Tính lương""));
        StringAssert.Contains(shellXaml, "Click="PayrollFoundation_OnClick"");
        StringAssert.Contains(shellXaml, "Tag="{Binding IsPayrollFoundationSelected}"");
        StringAssert.Contains(shell, "SetSelectedNavigation("workforce.payroll")");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == 56");
        StringAssert.Contains(wire, "new PayrollFoundationService(");
        StringAssert.Contains(wire, "new PayrollFoundationView(new PayrollFoundationViewModel(");
        StringAssert.Contains(hostWire, "WirePayrollFoundationWorkspace();");
    }

    [TestMethod]
    public void Lot6_Workspace_ContainsSixInnerTabsAndKeepsLaterActionsOut()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationView.xaml");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Actions.cs");

        foreach (var label in new[]
        {
            "Header="Bảng lương"",
            "Header="Đối soát"",
            "Header="Thiết lập lương"",
            "Header="Khoản thu &amp; khấu trừ"",
            "Header="Phiếu lương"",
            "Header="Lịch sử kỳ lương""
        })
            StringAssert.Contains(view, label);

        foreach (var command in new[]
        {
            "CREATE_PERIOD",
            "SAVE_SALARY",
            "CREATE_COMPONENT_TYPE",
            "ASSIGN_FIXED_COMPONENT",
            "ADD_PERIOD_COMPONENT"
        })
        {
            var contracts = ReadRepoFile("src", "CongTy.Contracts", "PayrollFoundationContracts.cs");
            StringAssert.Contains(contracts, command);
        }

        Assert.IsFalse(actions.Contains(""AGGREGATE"", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains(""RECONCILE"", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains(""CLOSE"", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains(""ADJUST"", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("CHỐT LƯƠNG", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("XUẤT PDF", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("XUẤT EXCEL", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot6_Foundation_PreservesEffectiveDatesCategoriesAndClosedAttendanceSource()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "PayrollFoundationContracts.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationView.xaml");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.cs");

        StringAssert.Contains(contracts, "AttendanceRevision");
        StringAssert.Contains(contracts, "AttendanceSourceFingerprint");
        StringAssert.Contains(contracts, "MonthlySalary");
        StringAssert.Contains(contracts, "EffectiveFrom");
        StringAssert.Contains(view, "Tạo kỳ lương từ kỳ công đã chốt");
        StringAssert.Contains(view, "Mức lương theo ngày áp dụng");
        StringAssert.Contains(view, "Khoản áp dụng định kỳ theo nhân sự");
        StringAssert.Contains(view, "Khoản linh động theo kỳ");
        StringAssert.Contains(view, "Thu nhập lương · Khấu trừ · Hoàn chi phí");
        StringAssert.Contains(view, "Hoàn chi phí được quản lý riêng với thu nhập lương.");
        Assert.IsFalse(vm.Contains(".Sum(", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("decimal.Parse", StringComparison.Ordinal));
        Assert.IsFalse(vm.Contains("double.Parse", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot6_Mutations_ReuseCanonicalKeyUntilLogicalPayloadSucceeds()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.cs");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Actions.cs");

        StringAssert.Contains(vm, "if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;");
        StringAssert.Contains(vm, "_idempotencyKeys.Create("payroll-foundation")");
        StringAssert.Contains(vm, "JsonSerializer.Serialize(payload)");
        StringAssert.Contains(actions, "_mutationKeys.Remove(slot);");

        Assert.AreEqual(5, Count(actions, "_mutationKeys.Remove(slot);"));
    }

    [TestMethod]
    public void Lot6_Permissions_KeepReadBroadButFoundationMutationManageOnly()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.cs");

        foreach (var permission in new[]
        {
            "core.payroll.read",
            "core.payroll.manage",
            "core.payroll.close",
            "core.payroll.adjust",
            "core.payroll.export"
        })
            StringAssert.Contains(nav, permission);

        StringAssert.Contains(vm, "_access.HasPermission(PayrollManagePermission)");
        StringAssert.Contains(vm, "_capabilities.CanManage");
    }

    [TestMethod]
    public void Lot6_Audit_LocksCurrentWebContractAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT6_PAYROLL_FOUNDATION_AUDIT.md");

        foreach (var marker in new[]
        {
            "5974a4850a8560d47d1074fbfbb1fc38361a2009",
            "9cd5ed9c52932d3b078647e8e754b927af0bd27c",
            "Issue #1140",
            "PR #1150",
            "CREATE_PERIOD",
            "ADD_PERIOD_COMPONENT",
            "numeric(18,2)",
            "không sửa Web/backend/DB/migration",
            "không deploy production"
        })
            StringAssert.Contains(audit, marker);
    }

    private static int Count(string value, string fragment)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += fragment.Length;
        }
        return count;
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
