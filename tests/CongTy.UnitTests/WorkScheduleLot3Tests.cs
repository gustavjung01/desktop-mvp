using System.Text.Json;
using CongTy.Contracts;
using CongTy.Desktop.Workforce;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkScheduleLot3Tests
{
    [TestMethod]
    public void Lot3_Contracts_DeserializeSchedulePlanningCatalog()
    {
        const string json = """
        {
          "shiftTemplates":[{
            "id":"10000000-0000-4000-8000-000000000001",
            "installation_id":"demo",
            "code":"CA_SANG",
            "name":"Ca sáng",
            "start_time":"08:00:00",
            "end_time":"17:00:00",
            "break_minutes":60,
            "is_active":true,
            "created_at":"2026-09-01T00:00:00.000Z",
            "updated_at":"2026-09-01T00:00:00.000Z"
          }],
          "weekTemplates":[{
            "id":"10000000-0000-4000-8000-000000000002",
            "installation_id":"demo",
            "code":"VAN_PHONG",
            "name":"Lịch văn phòng",
            "is_active":true,
            "created_at":"2026-09-01T00:00:00.000Z",
            "updated_at":"2026-09-01T00:00:00.000Z",
            "days":[
              {"id":"1","weekday":1,"schedule_kind":"WORK","shift_template_id":"10000000-0000-4000-8000-000000000001","shift_code":"CA_SANG","shift_name":"Ca sáng","shift_start_time":"08:00:00","shift_end_time":"17:00:00","shift_break_minutes":60},
              {"id":"2","weekday":2,"schedule_kind":"OFF","shift_template_id":null},
              {"id":"3","weekday":3,"schedule_kind":"OFF","shift_template_id":null},
              {"id":"4","weekday":4,"schedule_kind":"OFF","shift_template_id":null},
              {"id":"5","weekday":5,"schedule_kind":"OFF","shift_template_id":null},
              {"id":"6","weekday":6,"schedule_kind":"OFF","shift_template_id":null},
              {"id":"7","weekday":0,"schedule_kind":"OFF","shift_template_id":null}
            ]
          }],
          "calendarDays":[{
            "id":"10000000-0000-4000-8000-000000000003",
            "installation_id":"demo",
            "calendar_date":"2026-10-01",
            "calendar_kind":"PUBLIC_HOLIDAY",
            "name":"Ngày nghỉ kiểm thử",
            "is_active":true,
            "created_at":"2026-09-01T00:00:00.000Z",
            "updated_at":"2026-09-01T00:00:00.000Z"
          }]
        }
        """;

        var catalog = JsonSerializer.Deserialize<SchedulePlanningCatalogData>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Không đọc được catalog lập lịch.");

        Assert.HasCount(1, catalog.ShiftTemplates);
        Assert.HasCount(1, catalog.WeekTemplates);
        Assert.HasCount(1, catalog.CalendarDays);
        Assert.HasCount(7, catalog.WeekTemplates[0].Days);
        Assert.AreEqual("CA_SANG", catalog.ShiftTemplates[0].Code);
        Assert.AreEqual("PUBLIC_HOLIDAY", catalog.CalendarDays[0].CalendarKind);
    }

    [TestMethod]
    public void Lot3_ApiClient_UsesCanonicalRoutesAndIdempotentMutations()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "WorkScheduleService.cs");

        StringAssert.Contains(service, "\"/api/workforce/schedules?from=");
        StringAssert.Contains(service, "\"/api/workforce/schedule-planning\"");
        StringAssert.Contains(service, "\"/api/workforce/policies\"");
        StringAssert.Contains(service, "PostIdempotentDataAsync<WorkScheduleMutationRequest, WorkScheduleData>");
        StringAssert.Contains(service, "PostIdempotentDataAsync<TRequest, TResponse>");
        StringAssert.Contains(service, "idempotencyKey");
    }

    [TestMethod]
    public void Lot3_Mutations_ReuseKeyForSameLogicalPayloadUntilSuccess()
    {
        var core = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkScheduleViewModel.cs");
        var planning = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkScheduleViewModel.Planning.cs");

        StringAssert.Contains(core, "JsonSerializer.Serialize(payload)");
        StringAssert.Contains(core, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(core, "_mutationKeys[slot] = key");
        StringAssert.Contains(core, "_mutationKeys.Remove(slot)");
        StringAssert.Contains(core, "work-schedule-save");

        foreach (var scope in new[]
        {
            "work-shift-template-save",
            "work-week-template-save",
            "work-company-calendar-save",
            "work-week-template-apply",
            "work-schedule-copy"
        })
            StringAssert.Contains(planning, scope);
    }

    [TestMethod]
    public void Lot3_View_ImplementsWebPlanningSurfaceInsideScheduleWorkspace()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkScheduleView.xaml");

        foreach (var label in new[]
        {
            "Ca và ngày nghỉ",
            "Ca mẫu",
            "Lịch tuần",
            "Ngày lễ &amp; ngày nghỉ",
            "Xếp hàng loạt",
            "XẾP TỪ LỊCH TUẦN",
            "SAO CHÉP LỊCH",
            "Lý do xếp / điều chỉnh lịch",
            "Lịch đã điều chỉnh riêng theo người/ngày sẽ được giữ nguyên",
            "Ngày nghỉ Công Ty được ưu tiên"
        })
            StringAssert.Contains(view, label);

        StringAssert.Contains(view, "ItemsSource=\"{Binding VisibleSchedules}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding WeekDayRows}\"");
        StringAssert.Contains(view, "ItemsSource=\"{Binding BulkEmployees}\"");
    }

    [TestMethod]
    public void Lot3_Desktop_EnforcesFutureAndRangeGuardsWithoutReplacingBackendAuthority()
    {
        var core = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkScheduleViewModel.cs");
        var planning = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkScheduleViewModel.Planning.cs");

        StringAssert.Contains(core, "DraftWorkDate.Value.Date <= WorkSchedulePresentation.BusinessToday()");
        StringAssert.Contains(core, "(to - from).TotalDays > 93");
        StringAssert.Contains(core, "DraftOverrideReason.Trim().Length > 512");
        StringAssert.Contains(planning, "selected is < 1 or > 500");
        StringAssert.Contains(planning, "TotalDays > 92");
        StringAssert.Contains(planning, "TotalDays > 30");
        StringAssert.Contains(planning, "result.SkippedOverrides");
    }

    [TestMethod]
    public void Lot3_Shell_ActivatesOnlyScheduleNavigationWithCanonicalPermission()
    {
        var menu = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkSchedule.cs");
        var nav = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");

        StringAssert.Contains(menu, "Click=\"WorkSchedules_OnClick\"");
        StringAssert.Contains(menu, "Tag=\"{Binding IsWorkScheduleSelected}\"");
        StringAssert.Contains(shell, "SetSelectedNavigation(\"workforce.schedules\")");
        StringAssert.Contains(shell, "core.work-schedule.read");
        StringAssert.Contains(nav, "core.work-schedule.manage");

        var workforce = SliceTemplate(menu, "WorkforceMenuTemplate");
        StringAssert.Contains(workforce, "ToolTip=\"Sẽ được triển khai ở Lô 3\"><TextBlock Text=\"Chấm công\"");
        Assert.IsFalse(workforce.Contains("ToolTip=\"Sẽ được triển khai ở Lô 2\"><TextBlock Text=\"Ca / lịch làm việc\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot3_Audit_LocksCanonicalBusinessRulesAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT3_SCHEDULE_PLANNING_AUDIT.md");

        StringAssert.Contains(audit, "3bf4bac7071f2a4f1be3e296050b300cb2b4a288");
        StringAssert.Contains(audit, "core.work-schedule.read");
        StringAssert.Contains(audit, "core.work-schedule.manage");
        StringAssert.Contains(audit, "1 đến 500 nhân sự");
        StringAssert.Contains(audit, "93 ngày");
        StringAssert.Contains(audit, "31 ngày");
        StringAssert.Contains(audit, "giữ nguyên ngoại lệ cá nhân");
        StringAssert.Contains(audit, "không sửa Web/backend/DB/migration");
    }

    private static string SliceTemplate(string xaml, string key)
    {
        var startToken = $"<DataTemplate x:Key=\"{key}\">";
        var start = xaml.IndexOf(startToken, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"Không tìm thấy template {key}");
        var end = xaml.IndexOf("</DataTemplate>", start, StringComparison.Ordinal);
        Assert.IsGreaterThan(start, end, $"Template {key} không đóng đúng");
        return xaml[start..(end + "</DataTemplate>".Length)];
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
