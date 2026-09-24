using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceAttendanceAdjustmentLot12Tests
{
    [TestMethod]
    public void Lot12_Contracts_DeserializeCanonicalAdjustmentAndLockReadModels()
    {
        const string json = """
        {
          "period":{"from":"2026-09-01","to":"2026-09-24"},
          "selectedEmployee":{"id":"10000000-0000-4000-8000-000000000001","code":"NV001","name":"Nguyễn An","branchId":null},
          "branches":[],
          "pagination":{"limit":50,"offset":0,"total":1,"hasPrevious":false,"hasNext":false},
          "requests":[{
            "id":"20000000-0000-4000-8000-000000000001",
            "installation_id":"30000000-0000-4000-8000-000000000001",
            "employee_id":"10000000-0000-4000-8000-000000000001",
            "work_date":"2026-09-18",
            "requested_check_in_at":"2026-09-18T01:00:00.000Z",
            "requested_check_out_at":"2026-09-18T10:00:00.000Z",
            "reason":"Quên chấm công",
            "request_source":"SELF_REQUEST",
            "status":"SUBMITTED",
            "requested_by_actor_id":"40000000-0000-4000-8000-000000000001",
            "requested_by_employee_id":"10000000-0000-4000-8000-000000000001",
            "reviewed_by_actor_id":null,
            "review_reason":null,
            "reviewed_at":null,
            "version":1,
            "request_id":"req-1",
            "created_at":"2026-09-18T12:00:00.000Z",
            "updated_at":"2026-09-18T12:00:00.000Z",
            "employee_code":"NV001",
            "employee_name":"Nguyễn An",
            "employee_branch_id":null,
            "branch_code":null,
            "branch_name":null
          }],
          "capabilities":{"selfOnly":true,"canSubmitOwn":true,"canManage":false,"canLock":false}
        }
        """;

        var data = JsonSerializer.Deserialize<AttendanceAdjustmentListResponseData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được Điều chỉnh công.");

        Assert.IsTrue(data.Capabilities.SelfOnly);
        Assert.HasCount(1, data.Requests);
        Assert.AreEqual("SUBMITTED", data.Requests[0].Status);
        Assert.AreEqual("SELF_REQUEST", data.Requests[0].RequestSource);
        Assert.AreEqual(1, data.Requests[0].Version);
    }

    [TestMethod]
    public void Lot12_ApiClient_UsesCanonicalBackendRoutesAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceAdjustmentService.cs");

        foreach (var route in new[]
                 {
                     "/api/workforce/attendance/adjustments?",
                     "/api/workforce/attendance/adjustments",
                     "/api/workforce/attendance/adjustments/review",
                     "/api/workforce/attendance/adjustments/direct",
                     "/api/workforce/attendance/period-locks",
                 })
            StringAssert.Contains(service, route);

        Assert.AreEqual(
            4,
            CountOccurrences(service, "PostIdempotentDataAsync<"),
            "Bốn mutation của Lô 12 phải dùng canonical Idempotency-Key.");
    }

    [TestMethod]
    public void Lot12_ViewModel_ReusesIdempotencyKeyForSameLogicalPayload()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentViewModel.cs");

        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(viewModel, "JsonSerializer.Serialize(payload)");
        StringAssert.Contains(viewModel, "_mutationKeys.Remove(slot)");
        foreach (var scope in new[]
                 {
                     "desktop-attendance-adjustment-submit",
                     "desktop-attendance-adjustment-review",
                     "desktop-attendance-adjustment-direct",
                     "desktop-attendance-period-lock",
                 })
            StringAssert.Contains(viewModel, scope);

        StringAssert.Contains(viewModel, "_prefillEmployeeId");
        StringAssert.Contains(viewModel, "(end - start).TotalDays + 1 > 93");
        StringAssert.Contains(viewModel, "NextDay");
        StringAssert.Contains(viewModel, "Giờ ra không được sớm hơn giờ vào");
        StringAssert.Contains(viewModel, "Từ khóa nhân sự tối đa 80 ký tự");
    }

    [TestMethod]
    public void Lot12_View_CoversSelfReviewDirectLockAndOfficeLayout()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentView.xaml");

        foreach (var label in new[]
                 {
                     "YÊU CẦU TRONG KỲ",
                     "CHỜ DUYỆT",
                     "KỲ CÔNG ĐÃ KHÓA",
                     "LỊCH SỬ XỬ LÝ",
                     "GỬI YÊU CẦU CỦA TÔI",
                     "ĐIỀU CHỈNH TRỰC TIẾP",
                     "KHÓA KỲ CÔNG",
                     "XỬ LÝ YÊU CẦU",
                     "DUYỆT",
                     "TỪ CHỐI",
                     "Ngày kế tiếp",
                     "Lý do xác nhận",
                 })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "Grid.Row=\"4\"");
        StringAssert.Contains(view, "VerticalScrollBarVisibility=\"Auto\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding Rows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding LockRows}\"");
    }

    [TestMethod]
    public void Lot12_Timesheet_DeepLinksWithLockedPeriodCapabilityRules()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Timesheet.cs");

        StringAssert.Contains(viewModel, "_data.Capabilities.CanManage");
        StringAssert.Contains(viewModel, "SelectedDay.PeriodLock is null || _data.Capabilities.CanLock");
        StringAssert.Contains(viewModel, "_data.Capabilities.CanSubmitOwn");
        StringAssert.Contains(viewModel, "AdjustmentTargetEmployeeId");
        StringAssert.Contains(view, "OpenAdjustment_OnClick");
        StringAssert.Contains(codeBehind, "TimesheetAdjustmentRequestedEventArgs");
        StringAssert.Contains(shell, "NavigateAttendanceAdjustmentAsync(e.EmployeeId, e.WorkDate)");
    }

    [TestMethod]
    public void Lot12_Shell_UsesCanonicalReadPermissionsAndEnablesNavigation()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.AttendanceAdjustment.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.AttendanceAdjustment.cs");

        var block = SliceBetween(nav, "public bool CanViewWorkforceAdjustments", "public bool CanViewWorkforceSchedules");
        StringAssert.Contains(block, "core.attendance.self-adjust-request");
        StringAssert.Contains(block, "core.attendance.adjust");
        Assert.IsFalse(block.Contains("core.attendance.lock", StringComparison.Ordinal));

        StringAssert.Contains(xaml, "Click=\"AttendanceAdjustment_OnClick\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsAttendanceAdjustmentSelected}\"");
        StringAssert.Contains(shell, "workforce.adjustments");
        StringAssert.Contains(host, "WorkspaceSlots.AttendanceAdjustment");
    }

    [TestMethod]
    public void Lot12_Audit_LocksWebBaselineAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT12_ATTENDANCE_ADJUSTMENT_AUDIT.md");

        StringAssert.Contains(audit, "6bde0ff0f4c64923d9ce1f77305a8afc622927d2");
        StringAssert.Contains(audit, "af4acc24bc411d3206d07256f6119472068c01c3");
        StringAssert.Contains(audit, "core.attendance.self-adjust-request");
        StringAssert.Contains(audit, "core.attendance.adjust");
        StringAssert.Contains(audit, "core.attendance.lock");
        StringAssert.Contains(audit, "retry cùng payload reuse key cũ");
        StringAssert.Contains(audit, "không sửa Web/backend");
        StringAssert.Contains(audit, "không sửa DB/migration");
        StringAssert.Contains(audit, "không deploy production");
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }
        return count;
    }

    private static string SliceBetween(string value, string startToken, string endToken)
    {
        var start = value.IndexOf(startToken, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"Không tìm thấy {startToken}");
        var end = value.IndexOf(endToken, start, StringComparison.Ordinal);
        Assert.IsGreaterThan(start, end, $"Không tìm thấy {endToken}");
        return value[start..end];
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
