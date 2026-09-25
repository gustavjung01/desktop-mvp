using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceOvertimeCloseoutLot5Tests
{
    [TestMethod]
    public void Lot5_Contracts_DeserializeCanonicalOvertimeAndClosedPayrollInput()
    {
        const string overtimeJson = """
        {
          "id":"00000000-0000-4000-8000-000000000001",
          "installation_id":"demo",
          "employee_id":"00000000-0000-4000-8000-000000000002",
          "work_date":"2026-09-20",
          "requested_minutes":120,
          "reason":"Hoàn thành kiểm kê",
          "policy_id_snapshot":"00000000-0000-4000-8000-000000000003",
          "policy_code_snapshot":"VP",
          "policy_version_snapshot":2,
          "overtime_requires_approval_snapshot":true,
          "status":"CONFIRMED",
          "requested_by_actor_id":"actor",
          "requested_by_employee_id":"00000000-0000-4000-8000-000000000002",
          "actual_minutes":105,
          "confirmed_minutes":90,
          "version":4,
          "request_id":"req",
          "created_at":"2026-09-20T10:00:00Z",
          "updated_at":"2026-09-20T12:00:00Z",
          "employee_code":"NV001",
          "employee_name":"Nguyễn Văn An",
          "branch_name":"Chi nhánh 01"
        }
        """;
        const string payrollJson = """
        {
          "period":{
            "id":"00000000-0000-4000-8000-000000000010",
            "installation_id":"demo",
            "branch_id":null,
            "scope_key":"COMPANY",
            "period_start":"2026-09-01",
            "period_end":"2026-09-20",
            "status":"CLOSED",
            "issue_summary":{"blockers":{},"warnings":{}},
            "revision":2,
            "request_id":"req-period",
            "created_at":"2026-09-20T00:00:00Z",
            "updated_at":"2026-09-20T00:00:00Z",
            "created_by":"actor",
            "updated_by":"actor"
          },
          "revision":2,
          "sourceFingerprint":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "payrollInput":{
            "contractVersion":1,
            "period":{"from":"2026-09-01","to":"2026-09-20","branchId":null},
            "issueSummary":{"blockers":{},"warnings":{}},
            "status":"CLOSED",
            "revision":2,
            "employees":[{
              "employeeId":"00000000-0000-4000-8000-000000000002",
              "employeeCode":"NV001",
              "employeeName":"Nguyễn Văn An",
              "branchId":null,
              "branchCode":null,
              "branchName":null,
              "workDays":15,
              "completedDays":14,
              "countedMinutes":6720,
              "leaveCreditedMinutes":480,
              "approvedLeaveDays":1,
              "paidLeaveDays":1,
              "unpaidLeaveDays":0,
              "unexcusedAbsenceDays":0,
              "incompleteDays":0,
              "configurationIssueDays":0,
              "violationDays":0,
              "pendingLeaveDays":0,
              "pendingAdjustmentDays":0,
              "confirmedOvertimeMinutes":90
            }]
          }
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var overtime = JsonSerializer.Deserialize<OvertimeRequestData>(overtimeJson, options)
            ?? throw new InvalidOperationException("Không đọc được hồ sơ tăng ca.");
        var payroll = JsonSerializer.Deserialize<AttendancePayrollInputData>(payrollJson, options)
            ?? throw new InvalidOperationException("Không đọc được đầu vào tính lương.");

        Assert.AreEqual("CONFIRMED", overtime.Status);
        Assert.AreEqual(90, overtime.ConfirmedMinutes);
        Assert.AreEqual("Đã xác nhận giờ tính", OvertimeCloseoutPresentation.OvertimeStatusLabel(overtime.Status));
        Assert.AreEqual("CLOSED", payroll.PayrollInput.Status);
        Assert.AreEqual(2, payroll.Revision);
        Assert.HasCount(1, payroll.PayrollInput.Employees);
        Assert.AreEqual(90, payroll.PayrollInput.Employees[0].ConfirmedOvertimeMinutes);
    }

    [TestMethod]
    public void Lot5_ApiClient_UsesExactCurrentWebRoutesAndCanonicalIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "OvertimeCloseoutService.cs");

        foreach (var route in new[]
        {
            "/api/workforce/overtime",
            "/api/workforce/overtime/review",
            "/api/workforce/overtime/actual",
            "/api/workforce/overtime/confirm",
            "/api/workforce/attendance/periods",
            "/api/workforce/attendance/payroll-input"
        })
            StringAssert.Contains(service, route);

        foreach (var mutation in new[]
        {
            "SubmitOvertimeRequest",
            "ReviewOvertimeRequest",
            "RecordOvertimeActualRequest",
            "ConfirmOvertimeRequest",
            "AttendancePeriodMutationRequest"
        })
            StringAssert.Contains(service, $"PostIdempotentDataAsync<{mutation}");

        StringAssert.Contains(service, "periodId=");
        StringAssert.Contains(service, "branchId");
        StringAssert.Contains(service, "status");
    }

    [TestMethod]
    public void Lot5_Desktop_ActivatesCompactOvertimeCloseoutWorkspaceWithCanonicalPermissions()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.OvertimeCloseout.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var wire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OvertimeCloseout.cs");
        var hostWire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");

        foreach (var permission in new[]
        {
            "core.overtime.self-request",
            "core.overtime.read",
            "core.overtime.approve",
            "core.overtime.confirm",
            "core.attendance.self.read",
            "core.attendance.read",
            "core.attendance.reconcile",
            "core.attendance.lock"
        })
            StringAssert.Contains(nav, permission);

        StringAssert.Contains(shell, "SetSelectedNavigation(\"workforce.overtime\")");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex == WorkspaceSlots.OvertimeCloseout");
        StringAssert.Contains(xaml, "Click=\"OvertimeCloseout_OnClick\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsOvertimeCloseoutSelected}\"");
        Assert.IsFalse(xaml.Contains("IsEnabled=\"False\" ToolTip=\"Sẽ được triển khai ở Lô 6\"><TextBlock Text=\"Tăng ca và chốt công\"", StringComparison.Ordinal));
        StringAssert.Contains(wire, "new OvertimeCloseoutService(");
        StringAssert.Contains(wire, "new OvertimeCloseoutView(new OvertimeCloseoutViewModel(");
        StringAssert.Contains(hostWire, "WireOvertimeCloseoutWorkspace();");
    }

    [TestMethod]
    public void Lot5_Lifecycle_AndPeriodCloseoutRemainServerAuthoritative()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutViewModel.cs");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutViewModel.Actions.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutPresentation.cs");

        foreach (var status in new[]
        {
            "SUBMITTED", "APPROVED", "REJECTED", "ACTUAL_RECORDED", "CONFIRMED",
            "AGGREGATING", "NEEDS_ACTION", "RECONCILED", "CLOSED"
        })
            StringAssert.Contains(presentation, status);

        StringAssert.Contains(actions, "ExpectedVersion = selected.Source.Version");
        StringAssert.Contains(actions, "confirmedMinutes > selected.Source.ActualMinutes.Value");
        StringAssert.Contains(actions, "AcknowledgeWarnings");
        StringAssert.Contains(actions, "\"REFRESH\"");
        StringAssert.Contains(actions, "\"RECONCILE\"");
        StringAssert.Contains(actions, "\"CLOSE\"");
        StringAssert.Contains(vm, "ApplyPayroll");
        StringAssert.Contains(vm, "HasPayrollInputReadAccess");
        StringAssert.Contains(vm, "_access.HasPermission(AttendanceReconcilePermission)");
        StringAssert.Contains(vm, "_access.HasPermission(AttendanceLockPermission)");
        StringAssert.Contains(actions, "!HasPayrollInputReadAccess");
        StringAssert.Contains(vm, "item.Status == \"CLOSED\" && HasPayrollInputReadAccess");
        StringAssert.Contains(actions, "result.Period.Status == \"CLOSED\" && HasPayrollInputReadAccess");
        Assert.IsFalse(vm.Contains("SourceFingerprint =", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains("AttendanceEvent", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains("countedMinutes =", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot5_Mutations_ReuseCanonicalKeyUntilLogicalOperationSucceeds()
    {
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutViewModel.cs");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutViewModel.Actions.cs");

        StringAssert.Contains(vm, "if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;");
        StringAssert.Contains(vm, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(vm, "JsonSerializer.Serialize(payload)");

        foreach (var scope in new[]
        {
            "overtime-submit",
            "overtime-review",
            "overtime-actual",
            "overtime-confirm",
            "attendance-period-action"
        })
            StringAssert.Contains(actions, $"\"{scope}\"");

        StringAssert.Contains(actions, "_mutationKeys.Remove(slot);");
    }

    [TestMethod]
    public void Lot5_Ui_ContainsBothBusinessTabsBlockersWarningsAndReadonlyPayrollSnapshot()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutView.xaml");

        foreach (var label in new[]
        {
            "Tăng ca",
            "Chốt công",
            "Đăng ký → duyệt → thực tế → xác nhận giờ tính",
            "Đang tổng hợp → Cần xử lý → Đã đối soát → Đã chốt",
            "Việc cần xử lý trước đối soát",
            "Cảnh báo cần kiểm tra",
            "Tăng ca chưa xác nhận",
            "XÁC NHẬN ĐÃ ĐỐI SOÁT",
            "CHỐT KỲ CÔNG",
            "ĐẦU VÀO TÍNH LƯƠNG",
            "Chỉ đọc",
            "Giờ tăng ca đã xác nhận"
        })
            StringAssert.Contains(view, label);
    }

    [TestMethod]
    public void Lot5_Audit_LocksCurrentWebMainIssueAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT5_OVERTIME_CLOSEOUT_AUDIT.md");

        StringAssert.Contains(audit, "9cd5ed9c52932d3b078647e8e754b927af0bd27c");
        StringAssert.Contains(audit, "b4b975783b4a28879c2509c5bd08850d92dafef1");
        StringAssert.Contains(audit, "Issue #1140");
        StringAssert.Contains(audit, "PR #1149");
        StringAssert.Contains(audit, "CONFIRMED");
        StringAssert.Contains(audit, "append-only");
        StringAssert.Contains(audit, "backend là authority");
        StringAssert.Contains(audit, "Không sửa Web/backend/DB/migration");
        StringAssert.Contains(audit, "Không deploy production");
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
