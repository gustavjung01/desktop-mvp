using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceAttendanceViolationLot13Tests
{
    [TestMethod]
    public void Lot13_Contracts_DeserializeCanonicalViolationWorkflow()
    {
        const string json = """
        {
          "period":{"from":"2026-09-01","to":"2026-09-24","timezone":"Asia/Ho_Chi_Minh"},
          "scope":{"companyScope":false,"selfOnly":true,"branches":[]},
          "pagination":{"limit":100,"offset":0,"total":1,"hasPrevious":false,"hasNext":false},
          "entries":[{
            "employee":{"id":"10000000-0000-4000-8000-000000000001","code":"NV001","name":"Nguyễn An","branchId":null,"branchCode":null,"branchName":null},
            "workDate":"2026-09-18",
            "violation":{"kind":"LATE","label":"Đi trễ","detail":"Đi trễ 18 phút","minutes":18,"dayFraction":null},
            "evaluationState":"HAS_VIOLATIONS",
            "case":{
              "id":"20000000-0000-4000-8000-000000000001",
              "installation_id":"30000000-0000-4000-8000-000000000001",
              "employee_id":"10000000-0000-4000-8000-000000000001",
              "work_date":"2026-09-18",
              "violation_kind":"LATE",
              "violation_label_snapshot":"Đi trễ",
              "violation_detail_snapshot":"Đi trễ 18 phút",
              "violation_minutes_snapshot":18,
              "violation_day_fraction_snapshot":null,
              "policy_id_snapshot":null,
              "policy_version_snapshot":2,
              "status":"EXPLANATION_SUBMITTED",
              "explanation":"Kẹt xe do sự cố",
              "explained_by_actor_id":"40000000-0000-4000-8000-000000000001",
              "explained_at":"2026-09-18T12:00:00.000Z",
              "reviewed_by_actor_id":null,
              "review_note":null,
              "reviewed_at":null,
              "outcome":null,
              "version":1,
              "request_id":"req-1",
              "created_at":"2026-09-18T12:00:00.000Z",
              "updated_at":"2026-09-18T12:00:00.000Z"
            }
          }],
          "capabilities":{"selfOnly":true,"canExplain":true,"canReview":false}
        }
        """;

        var data = JsonSerializer.Deserialize<AttendanceViolationHandlingResponseData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được hồ sơ vi phạm.");

        Assert.IsTrue(data.Scope.SelfOnly);
        Assert.HasCount(1, data.Entries);
        Assert.AreEqual("LATE", data.Entries[0].Violation?.Kind);
        Assert.AreEqual("EXPLANATION_SUBMITTED", data.Entries[0].Case?.Status);
        Assert.AreEqual(1, data.Entries[0].Case?.Version);
        Assert.IsTrue(data.Capabilities.CanExplain);
    }

    [TestMethod]
    public void Lot13_ApiClient_UsesCanonicalViolationRoutesAndTwoIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceViolationService.cs");

        foreach (var route in new[]
                 {
                     "/api/workforce/attendance/violations?",
                     "/api/workforce/attendance/violations/explain",
                     "/api/workforce/attendance/violations/review",
                 })
            StringAssert.Contains(service, route);

        Assert.AreEqual(
            2,
            CountOccurrences(service, "PostIdempotentDataAsync<"),
            "Explain và review phải dùng canonical Idempotency-Key.");
        StringAssert.Contains(service, "limit={Math.Clamp(limit, 1, 100)}");
    }

    [TestMethod]
    public void Lot13_ViewModel_ImplementsCanonicalStateMachineAndStableRetryKeys()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViolationViewModel.cs");

        foreach (var value in new[]
                 {
                     "EXPLANATION_SUBMITTED",
                     "UNDER_REVIEW",
                     "START_REVIEW",
                     "CONCLUDE",
                     "EXCUSED",
                     "CONFIRMED",
                 })
            StringAssert.Contains(viewModel, value);

        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(viewModel, "JsonSerializer.Serialize(payload)");
        StringAssert.Contains(viewModel, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(viewModel, "desktop-attendance-violation-explain");
        StringAssert.Contains(viewModel, "desktop-attendance-violation-review");
        StringAssert.Contains(viewModel, "Source.Case.Version");
        StringAssert.Contains(viewModel, "(end - start).TotalDays + 1 > 93");
        StringAssert.Contains(viewModel, "Từ khóa nhân sự tối đa 80 ký tự");
        StringAssert.Contains(viewModel, "2.000 ký tự");
    }

    [TestMethod]
    public void Lot13_View_UsesOfficeLanguageAndKeepsViolationSeparateFromPay()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViolationView.xaml");

        foreach (var label in new[]
                 {
                     "VI PHẠM ĐANG GHI NHẬN",
                     "CHỜ XEM XÉT",
                     "ĐANG XEM XÉT",
                     "ĐÃ KẾT LUẬN",
                     "Vi phạm và giải trình",
                     "GIẢI TRÌNH",
                     "KẾT LUẬN HỒ SƠ",
                     "Xác nhận vi phạm",
                     "không sửa sự kiện chấm công",
                     "không tự điều chỉnh thu nhập",
                 })
            StringAssert.Contains(view, label);

        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "AttendanceViolationViewModel.cs");
        StringAssert.Contains(viewModel, "Chấp nhận giải trình");
        StringAssert.Contains(view, "ItemsSource=\"{Binding OutcomeOptions}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding Rows}\"");
        StringAssert.Contains(view, "Click=\"StartReview_OnClick\"");
        StringAssert.Contains(view, "Click=\"SubmitConclusion_OnClick\"");
        Assert.IsFalse(view.Contains("phạt tiền", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("trừ lương", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot13_Shell_FixesReadPermissionParityAndEnablesWorkspace()
    {
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.AttendanceViolation.cs");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.AttendanceViolation.cs");

        var block = SliceBetween(nav, "public bool CanViewWorkforceViolations", "public bool CanViewWorkforceAdjustments");
        foreach (var permission in new[]
                 {
                     "core.attendance.self.read",
                     "core.attendance.read",
                     "core.attendance-violation.self-explain",
                     "core.attendance-violation.resolve",
                 })
            StringAssert.Contains(block, permission);

        StringAssert.Contains(xaml, "Click=\"AttendanceViolation_OnClick\"");
        StringAssert.Contains(xaml, "Tag=\"{Binding IsAttendanceViolationSelected}\"");
        Assert.IsFalse(xaml.Contains("ToolTip=\"Sẽ được triển khai ở Lô 4\"><TextBlock Text=\"Xử lý vi phạm công\"", StringComparison.Ordinal));
        StringAssert.Contains(shell, "workforce.violations");
        StringAssert.Contains(host, "WorkspaceSlots.AttendanceViolation");
    }

    [TestMethod]
    public void Lot13_Timesheet_LinksCurrentViolationsToHandlingWorkspace()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Timesheet.cs");

        StringAssert.Contains(viewModel, "SelectedDay?.ViolationEvaluation.Items.Length > 0");
        StringAssert.Contains(view, "MỞ XỬ LÝ VI PHẠM");
        StringAssert.Contains(view, "OpenViolationHandling_OnClick");
        StringAssert.Contains(codeBehind, "ViolationRequested");
        StringAssert.Contains(shell, "NavigateAttendanceViolationAsync()");
    }

    [TestMethod]
    public void Lot13_Audit_LocksTruthBoundaryPermissionsAndNoPayrollMutation()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT13_ATTENDANCE_VIOLATION_AUDIT.md");

        StringAssert.Contains(audit, "e354ceda7cd23d8f4a8d8b4f61b2d5d9b83adf89");
        StringAssert.Contains(audit, "af4acc24bc411d3206d07256f6119472068c01c3");
        StringAssert.Contains(audit, "core.attendance.self.read");
        StringAssert.Contains(audit, "core.attendance.read");
        StringAssert.Contains(audit, "core.attendance-violation.self-explain");
        StringAssert.Contains(audit, "core.attendance-violation.resolve");
        StringAssert.Contains(audit, "VIOLATION_CHANGED");
        StringAssert.Contains(audit, "Retry cùng payload reuse key cũ");
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
