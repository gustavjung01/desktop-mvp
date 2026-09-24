using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceLeaveLot4Tests
{
    [TestMethod]
    public void Lot4_Contracts_DeserializeCanonicalLeaveBalanceAndCapabilities()
    {
        const string json = """
        {
          "selectedEmployee":{"id":"00000000-0000-4000-8000-000000000001","code":"NV001","name":"Nguyễn Văn An","branchId":null},
          "branches":[{"id":"00000000-0000-4000-8000-000000000010","code":"CN01","name":"Chi nhánh 01","is_active":true}],
          "balanceAsOfDate":"2026-09-22",
          "leaveBalances":[{
            "employee_id":"00000000-0000-4000-8000-000000000001",
            "employee_code":"NV001","employee_name":"Nguyễn Văn An","branch_name":"Chi nhánh 01",
            "leave_type_id":"00000000-0000-4000-8000-000000000020",
            "leave_type_code":"AL","leave_type_name":"Phép năm","balance_days":8.5,
            "allow_negative_balance":false,"last_activity_date":"2026-09-20"
          }],
          "balanceEntries":[{
            "id":"00000000-0000-4000-8000-000000000030",
            "employee_id":"00000000-0000-4000-8000-000000000001",
            "leave_type_id":"00000000-0000-4000-8000-000000000020",
            "leave_type_code_snapshot":"AL","leave_type_name_snapshot":"Phép năm",
            "entry_type":"REVERSAL","quantity_days":1,"effective_date":"2026-09-20",
            "reason":"Hoàn phép do hủy đơn"
          }],
          "pagination":{"limit":50,"offset":0,"total":1,"hasPrevious":false,"hasNext":false},
          "requests":[{
            "id":"00000000-0000-4000-8000-000000000040",
            "employee_id":"00000000-0000-4000-8000-000000000001",
            "leave_type_id":"00000000-0000-4000-8000-000000000020",
            "leave_type_code_snapshot":"AL","leave_type_name_snapshot":"Phép năm",
            "leave_is_paid_snapshot":true,"leave_counts_as_workday_snapshot":true,
            "leave_requires_approval_snapshot":true,"leave_tracks_balance_snapshot":true,
            "leave_allow_negative_balance_snapshot":false,
            "date_from":"2026-09-23","date_to":"2026-09-23","day_part":"FIRST_HALF",
            "reason":"Việc gia đình","attachment_reference":null,"status":"SUBMITTED",
            "requested_by_actor_id":"actor",
            "requested_by_employee_id":"00000000-0000-4000-8000-000000000001",
            "version":1,"request_id":"req","created_at":"2026-09-22T00:00:00Z",
            "updated_at":"2026-09-22T00:00:00Z","employee_code":"NV001","employee_name":"Nguyễn Văn An"
          }],
          "capabilities":{"selfOnly":true,"canSubmitOwn":true,"canApprove":false,"canManageTypes":false}
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var data = JsonSerializer.Deserialize<LeaveRequestListResponseData>(json, options)
            ?? throw new InvalidOperationException("Không đọc được dữ liệu nghỉ.");

        Assert.IsTrue(data.Capabilities.SelfOnly);
        Assert.IsTrue(data.Capabilities.CanSubmitOwn);
        Assert.HasCount(1, data.LeaveBalances);
        Assert.AreEqual(8.5m, data.LeaveBalances[0].BalanceDays);
        Assert.HasCount(1, data.BalanceEntries);
        Assert.AreEqual("REVERSAL", data.BalanceEntries[0].EntryType);
        Assert.HasCount(1, data.Requests);
        Assert.IsTrue(data.Requests[0].LeaveTracksBalanceSnapshot);
        Assert.AreEqual("Nửa ca đầu", LeavePresentation.DayPartLabel(data.Requests[0].DayPart));
        Assert.AreEqual("Hoàn phép", LeavePresentation.EntryTypeLabel(data.BalanceEntries[0].EntryType));
    }

    [TestMethod]
    public void Lot4_ApiClient_UsesExactCurrentWebRoutesAndCanonicalIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "LeaveService.cs");
        foreach (var route in new[]
        {
            "/api/workforce/leave-types",
            "/api/workforce/leave-types/update",
            "/api/workforce/leave/requests",
            "/api/workforce/leave/requests/review",
            "/api/workforce/leave/requests/cancel",
            "/api/workforce/leave/balances",
            "/api/workforce/leave/balances/entries"
        })
            StringAssert.Contains(service, route);

        foreach (var mutationType in new[]
        {
            "SubmitLeaveRequestRequest",
            "ReviewLeaveRequestRequest",
            "CancelLeaveRequestRequest",
            "CreateLeaveTypeRequest",
            "UpdateLeaveTypeRequest",
            "PostLeaveBalanceEntryRequest"
        })
            StringAssert.Contains(service, $"PostIdempotentDataAsync<{mutationType}");

        StringAssert.Contains(service, "employeeQuery");
        StringAssert.Contains(service, "branchId");
        StringAssert.Contains(service, "asOfDate");
    }

    [TestMethod]
    public void Lot4_Desktop_UsesCanonicalPermissionsAndActivatesLeaveWorkspace()
    {
        var workforce = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var leaveShell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Leave.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var wire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Leave.cs");
        var orderWire = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.OrderManagement.cs");

        foreach (var permission in new[]
        {
            "core.leave.self.read","core.leave.self.request","core.leave.read",
            "core.leave.approve","core.leave-type.manage"
        })
            StringAssert.Contains(workforce, permission);

        StringAssert.Contains(leaveShell, "SetSelectedNavigation(\"workforce.leave\")");
        StringAssert.Contains(leaveShell, "SelectedWorkspaceIndex == WorkspaceSlots.Leave");
        StringAssert.Contains(shell, "Click=\"Leave_OnClick\"");
        StringAssert.Contains(shell, "Tag=\"{Binding IsLeaveSelected}\"");
        Assert.IsFalse(shell.Contains("IsEnabled=\"False\" ToolTip=\"Sẽ được triển khai ở Lô 5\"><TextBlock Text=\"Nghỉ và đơn nghỉ\"", StringComparison.Ordinal));
        StringAssert.Contains(wire, "new LeaveService(");
        StringAssert.Contains(wire, "new LeaveView(new LeaveViewModel(");
        StringAssert.Contains(orderWire, "WireLeaveWorkspace();");
    }

    [TestMethod]
    public void Lot4_Mutations_ReuseAttemptKeyForSameLogicalPayload()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.cs");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.Actions.cs");

        StringAssert.Contains(viewModel, "if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(viewModel, "$\"{kind}|{JsonSerializer.Serialize(payload)}\"");
        foreach (var scope in new[]
        {
            "leave-request-submit","leave-request-review","leave-request-cancel",
            "leave-balance-entry","leave-type-create","leave-type-update"
        })
            StringAssert.Contains(actions, $"\"{scope}\"");

        StringAssert.Contains(actions, "_mutationKeys.Remove(slot);");
        Assert.IsFalse(actions.Contains("_mutationKeys.Clear();", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot4_Ui_CoversRequestsBalanceLedgerAndLeaveTypeParity()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveView.xaml");
        var actions = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.Actions.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "LeavePresentation.cs");

        foreach (var label in new[]
        {
            "Từ ngày","Đến ngày","Trạng thái","Nhân sự","Chi nhánh","TẠO ĐƠN NGHỈ",
            "DUYỆT / TỪ CHỐI","HỦY ĐƠN NGHỈ","SỐ DƯ / SỔ PHÉP",
            "LỊCH SỬ BIẾN ĐỘNG SỐ DƯ","GHI SỔ PHÉP","CHẾ ĐỘ NGHỈ",
            "Theo dõi số dư phép","Cho phép số dư âm"
        })
            StringAssert.Contains(view, label);

        foreach (var entryType in new[]
        {
            "OPENING_GRANT","ACCRUAL","ADJUSTMENT","CARRY_OVER",
            "EXPIRY","COMPENSATORY","USAGE","REVERSAL"
        })
            StringAssert.Contains(presentation, entryType);

        StringAssert.Contains(actions, "ExpectedVersion = target.Source.Version");
        StringAssert.Contains(actions, "days < 0");
        StringAssert.Contains(actions, "\"ADJUSTMENT\"");
        Assert.IsFalse(actions.Contains("BalanceDays -=", StringComparison.Ordinal));
        Assert.IsFalse(actions.Contains("BalanceDays +=", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot4_Audit_LocksWebMainLifecycleAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT4_LEAVE_AUDIT.md");
        StringAssert.Contains(audit, "9cd5ed9c52932d3b078647e8e754b927af0bd27c");
        StringAssert.Contains(audit, "87d2062778ab81b15e71f46bd034041656a47c26");
        StringAssert.Contains(audit, "Issue #1140");
        StringAssert.Contains(audit, "PR #1148");
        StringAssert.Contains(audit, "append-only");
        StringAssert.Contains(audit, "USAGE");
        StringAssert.Contains(audit, "REVERSAL");
        StringAssert.Contains(audit, "backend là authority");
        StringAssert.Contains(audit, "không sửa Web/backend/DB/migration");
        StringAssert.Contains(audit, "không deploy production");
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
