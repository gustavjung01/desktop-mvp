using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceTimesheetLot11Tests
{
    [TestMethod]
    public void Lot11_Contracts_DeserializeCanonicalEmployeeTimesheetReadModel()
    {
        const string json = """
        {
          "view":"employee",
          "period":{"from":"2026-09-01","to":"2026-09-30","timezone":"Asia/Ho_Chi_Minh"},
          "scope":{"companyScope":false,"selfOnly":true,"branches":[]},
          "pagination":{"limit":100,"offset":0,"total":1,"hasPrevious":false,"hasNext":false},
          "capabilities":{"canSubmitOwn":true,"canManage":false,"canLock":false},
          "rows":[{
            "employee":{"id":"10000000-0000-4000-8000-000000000001","code":"NV001","name":"Nguyễn Văn A","branchId":null,"branchCode":null,"branchName":null},
            "period":{"from":"2026-09-01","to":"2026-09-30","timezone":"Asia/Ho_Chi_Minh"},
            "workDays":22,
            "completedDays":20,
            "scheduledDaysOff":8,
            "approvedLeaveDays":1,
            "pendingLeaveDays":0,
            "unexcusedAbsenceDays":1,
            "incompleteDays":0,
            "configurationIssueDays":0,
            "violationDays":1,
            "lateViolationDays":1,
            "earlyLeaveViolationDays":0,
            "missingAttendanceViolationDays":0,
            "unexcusedAbsenceViolationDays":0,
            "missingDays":0,
            "actualMinutes":9600,
            "countedMinutes":9600,
            "leaveCreditedMinutes":480,
            "lateMinutes":10,
            "earlyLeaveMinutes":0,
            "adjustedDays":1,
            "pendingAdjustmentDays":0,
            "lockedDays":0,
            "attendanceSources":["QR"],
            "scheduleSources":["POLICY"],
            "days":[{
              "workDate":"2026-09-01",
              "employee":{"id":"10000000-0000-4000-8000-000000000001","code":"NV001","name":"Nguyễn Văn A","branchId":null,"branchCode":null,"branchName":null},
              "policy":{"id":"20000000-0000-4000-8000-000000000001","code":"HC","version":1,"name":"Hành chính","timeMode":"FIXED","attendanceBasis":"TIME","timezone":"Asia/Ho_Chi_Minh","breakMinutes":60,"lateGraceMinutes":5,"earlyLeaveGraceMinutes":5},
              "schedule":{"id":"30000000-0000-4000-8000-000000000001","kind":"WORK","source":"POLICY"},
              "expectedStartAt":"2026-09-01T01:00:00.000Z",
              "expectedEndAt":"2026-09-01T10:00:00.000Z",
              "requiredStartAt":"2026-09-01T01:00:00.000Z",
              "requiredEndAt":"2026-09-01T10:00:00.000Z",
              "checkInAt":"2026-09-01T01:10:00.000Z",
              "checkOutAt":"2026-09-01T10:00:00.000Z",
              "actualMinutes":470,
              "countedMinutes":470,
              "leaveCreditedMinutes":0,
              "lateMinutes":10,
              "earlyLeaveMinutes":0,
              "missingCheckIn":false,
              "missingCheckOut":false,
              "scheduledWorkDay":true,
              "validWork":true,
              "status":"LATE",
              "attendanceStatus":"LATE",
              "configurationIssue":null,
              "unexcusedAbsenceFraction":0,
              "violationEvaluation":{"state":"HAS_VIOLATIONS","explanation":"Có sai lệch","items":[{"kind":"LATE","label":"Đi trễ","detail":"Trễ 10 phút","minutes":10,"dayFraction":null}]},
              "leave":{"requests":[],"approvedFraction":0,"pendingFraction":0,"countedAsWorkdayFraction":0,"paidFraction":0,"approvedSegments":[],"pendingSegments":[],"countedSegments":[],"approvedLabels":[],"pendingLabels":[]},
              "attendanceSources":["QR"],
              "scheduleSource":"POLICY",
              "adjustment":null,
              "periodLock":null,
              "events":[]
            }]
          }]
        }
        """;

        var data = JsonSerializer.Deserialize<AttendanceTimesheetResponseData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được Bảng công.");

        Assert.AreEqual("employee", data.View);
        Assert.IsTrue(data.Scope.SelfOnly);
        Assert.HasCount(1, data.Rows);
        Assert.HasCount(1, data.Rows[0].Days);
        Assert.AreEqual("LATE", data.Rows[0].Days[0].Status);
        Assert.AreEqual(10, data.Rows[0].Days[0].LateMinutes);
        Assert.AreEqual("HAS_VIOLATIONS", data.Rows[0].Days[0].ViolationEvaluation.State);
    }

    [TestMethod]
    public void Lot11_ApiClient_UsesReadOnlyCanonicalTimesheetContract()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "TimesheetService.cs");

        StringAssert.Contains(service, "/api/workforce/attendance/timesheet?");
        StringAssert.Contains(service, "view={Uri.EscapeDataString(normalizedView)}");
        StringAssert.Contains(service, "employeeQuery");
        StringAssert.Contains(service, "branchId");
        StringAssert.Contains(service, "Math.Clamp(limit, 1, maxLimit)");
        Assert.IsFalse(service.Contains("PostDataAsync", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotent", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot11_ViewModel_EnforcesPeriodFilterScopeAndPaginationWithoutRecomputingTimesheet()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.cs");

        StringAssert.Contains(viewModel, "(end - start).TotalDays + 1 > 93");
        StringAssert.Contains(viewModel, "Từ khóa nhân sự tối đa 80 ký tự");
        StringAssert.Contains(viewModel, "var view = IsMonthlyView ? \"monthly\" : \"employee\"");
        StringAssert.Contains(viewModel, "data.Scope.SelfOnly");
        StringAssert.Contains(viewModel, "_data?.Pagination.HasPrevious");
        StringAssert.Contains(viewModel, "_data?.Pagination.HasNext");
        StringAssert.Contains(viewModel, "AttendanceTimesheetMonthData");
        Assert.IsFalse(viewModel.Contains("Idempotency-Key", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot11_View_ImplementsFixedOfficeSurfaceDailyMonthlyAndDayDetails()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml");

        foreach (var label in new[]
                 {
                     "KỲ ĐANG XEM",
                     "PHẠM VI",
                     "SỐ NHÂN SỰ",
                     "Ngày phải làm",
                     "Nghỉ và phép",
                     "Cần xử lý",
                     "Ngày 01–31",
                     "CÔNG THEO NGÀY",
                     "CHI TIẾT NGÀY CÔNG",
                     "LỊCH VÀ ĐƠN NGHỈ",
                     "KIỂM SOÁT · NGUỒN DỮ LIỆU",
                     "ĐÁNH GIÁ VI PHẠM",
                     "CHI TIẾT SỰ KIỆN",
                 })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "Content=\"{Binding DailyViewLabel}\"");
        StringAssert.Contains(view, "Content=\"{Binding MonthlyViewLabel}\"");
        StringAssert.Contains(view, "Grid.Row=\"4\"");
        StringAssert.Contains(view, "FrozenColumnCount=\"1\"");
        StringAssert.Contains(view, "ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding EmployeeRows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding DayEvents}\"");
    }

    [TestMethod]
    public void Lot11_Shell_UsesOnlyCanonicalTimesheetReadPermissions()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Timesheet.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Timesheet.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");

        var block = SliceBetween(nav, "public bool CanViewWorkforceTimesheet", "public bool CanViewWorkforceOvertime");
        StringAssert.Contains(block, "core.attendance.self.read");
        StringAssert.Contains(block, "core.attendance.read");
        Assert.IsFalse(block.Contains("core.attendance.reconcile", StringComparison.Ordinal));
        Assert.IsFalse(block.Contains("core.attendance.lock", StringComparison.Ordinal));

        StringAssert.Contains(shell, "workforce.timesheet");
        StringAssert.Contains(host, "WorkspaceSlots.Timesheet");
        StringAssert.Contains(xaml, "Click=\"Timesheet_OnClick\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsTimesheetSelected}\"");
        Assert.IsFalse(xaml.Contains("ToolTip=\"Sẽ được triển khai ở Lô 4\"><TextBlock Text=\"Bảng công\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot11_Audit_LocksWebBaselineAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT11_TIMESHEET_AUDIT.md");

        StringAssert.Contains(audit, "9170ecf99410537d407130d9ca1eaa2713adadbc");
        StringAssert.Contains(audit, "af4acc24bc411d3206d07256f6119472068c01c3");
        StringAssert.Contains(audit, "core.attendance.read");
        StringAssert.Contains(audit, "core.attendance.self.read");
        StringAssert.Contains(audit, "không có mutation");
        StringAssert.Contains(audit, "không sửa Web/backend");
        StringAssert.Contains(audit, "không sửa DB/migration");
        StringAssert.Contains(audit, "không deploy production");
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
