namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceTimesheetQuickActionsIssue85Lot1Tests
{
    [TestMethod]
    public void Lot1_ApiClient_UsesCanonicalManagedManualAndDirectAdjustmentRoutes()
    {
        var attendance = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceService.cs");
        var adjustment = ReadRepoFile("src", "CongTy.ApiClient", "AttendanceAdjustmentService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "AttendanceContracts.cs");

        StringAssert.Contains(contracts, "ManagedManualAttendanceRequest");
        StringAssert.Contains(contracts, "JsonPropertyName(\"employeeId\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"action\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"exitReason\")");
        StringAssert.Contains(contracts, "JsonPropertyName(\"note\")");

        StringAssert.Contains(attendance, "RecordManagedManualAsync");
        StringAssert.Contains(attendance, "/api/workforce/attendance/manual");
        StringAssert.Contains(attendance, "PostIdempotentDataAsync<ManagedManualAttendanceRequest, AttendanceRecordResultData>");
        StringAssert.Contains(adjustment, "/api/workforce/attendance/adjustments/direct");
    }

    [TestMethod]
    public void Lot1_ViewModel_CoversWebQuickActionsAndReusesStableRetryKeys()
    {
        var main = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.cs");
        var quick = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.QuickActions.cs");

        StringAssert.Contains(main, "PrepareDayActions(day)");
        StringAssert.Contains(main, "ResetDayActions()");
        foreach (var action in new[] { "CHECK_IN", "CHECK_OUT", "TEMP_EXIT", "RETURN", "END_EXTERNAL_WORK" })
            StringAssert.Contains(quick, action);
        foreach (var reason in new[] { "WORK_BUSINESS", "PERSONAL", "BREAK", "OTHER" })
            StringAssert.Contains(quick, reason);

        StringAssert.Contains(quick, "timesheet-attendance-quick-action");
        StringAssert.Contains(quick, "_attendanceService.RecordManagedManualAsync(payload, key)");
        StringAssert.Contains(quick, "_adjustmentService.DirectAsync(request!, key)");
        StringAssert.Contains(quick, "timesheet-attendance-direct-adjustment");
        StringAssert.Contains(quick, "_dayMutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(quick, "_dayMutationKeys.Remove(slot)");
        StringAssert.Contains(quick, "_idempotencyKeys.Create(operation)");
        StringAssert.Contains(quick, "SelectedDay.PeriodLock is null || data.Capabilities.CanLock");
        StringAssert.Contains(quick, "selectedDate.Date == WorkSchedulePresentation.BusinessToday()");
        Assert.IsFalse(quick.Contains("DateTime.Now", StringComparison.Ordinal));
        Assert.IsFalse(quick.Contains("DateTimeOffset.Now", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot1_Popup_ProvidesQuickAttendanceExitReasonsAndInlineCorrection()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "TimesheetView.xaml.cs");

        foreach (var label in new[]
                 {
                     "THAO TÁC NHANH",
                     "CHẤM VÀO",
                     "RA NGOÀI",
                     "QUAY LẠI",
                     "KẾT THÚC LÀM VIỆC",
                     "KẾT THÚC CÔNG VIỆC BÊN NGOÀI",
                     "Mục đích ra ngoài",
                     "GHI NHẬN RA NGOÀI",
                     "SỬA GIỜ VÀO / RA",
                     "Lý do",
                 })
            StringAssert.Contains(view, label);

        StringAssert.Contains(quick, "\"LƯU ĐIỀU CHỈNH\"");
        StringAssert.Contains(view, "MaxLength=\"1024\"");
        StringAssert.Contains(view, "MaxLength=\"1000\"");
        StringAssert.Contains(codeBehind, "SubmitQuickAttendanceAsync(\"CHECK_IN\")");
        StringAssert.Contains(codeBehind, "SubmitQuickAttendanceAsync(\"CHECK_OUT\")");
        StringAssert.Contains(codeBehind, "SubmitQuickAttendanceAsync(\"RETURN\")");
        StringAssert.Contains(codeBehind, "SubmitQuickAttendanceAsync(\"END_EXTERNAL_WORK\")");
        StringAssert.Contains(codeBehind, "SubmitQuickExitAsync()");
        StringAssert.Contains(codeBehind, "SaveDayAdjustmentAsync()");
    }

    [TestMethod]
    public void Lot1_Shell_WiresExistingCoreClientsWithoutBackendChanges()
    {
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.Timesheet.cs");

        StringAssert.Contains(host, "new AttendanceService(apiClient, session)");
        StringAssert.Contains(host, "new AttendanceAdjustmentService(apiClient, session)");
        StringAssert.Contains(host, "ResolveRequired<ICanonicalIdempotencyKeyProvider>()");
        Assert.IsFalse(host.Contains("HttpClient(", StringComparison.Ordinal));
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
