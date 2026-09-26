namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot7FinalHardeningTests
{
    [TestMethod]
    public void Lot7_QuickAttendanceKeyIncludesWorkDateAndRetryStillUsesCanonicalProvider()
    {
        var source = Read("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.QuickActions.cs");

        StringAssert.Contains(source, "new QuickAttendanceOperation(SelectedDay.WorkDate, payload)");
        StringAssert.Contains(source, "private sealed record QuickAttendanceOperation(");
        StringAssert.Contains(source, "string WorkDate");
        StringAssert.Contains(source, "_dayMutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(source, "_idempotencyKeys.Create(operation)");
        StringAssert.Contains(source, "_dayMutationKeys.Remove(slot)");
    }

    [TestMethod]
    public void Lot7_ReloadAfterMutationRestoresTheSameEmployeeAndWorkDate()
    {
        var source = Read("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.QuickActions.cs");

        StringAssert.Contains(source, "var employeeId = SelectedDay?.Employee.Id;");
        StringAssert.Contains(source, "var workDate = SelectedDay?.WorkDate;");
        StringAssert.Contains(source, "await LoadAsync(offset)");
        StringAssert.Contains(source, "row.Source.Employee.Id");
        StringAssert.Contains(source, "employee.Source.Days.FirstOrDefault");
        StringAssert.Contains(source, "OpenEmployee(employee);");
        StringAssert.Contains(source, "OpenDay(day);");
    }

    [TestMethod]
    public void Lot7_OfficeExportsDoNotFallbackToRawInternalEnums()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeDataExportFile.cs");

        StringAssert.Contains(source, "private static string UnknownEnumLabel");
        StringAssert.Contains(source, "? "—" : "Cần kiểm tra"");
        StringAssert.Contains(source, ""OFF" => "Ngày nghỉ"");
        StringAssert.Contains(source, "_ => "Ngày khác"");
        Assert.IsFalse(source.Contains("_ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim()", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("_ => string.IsNullOrWhiteSpace(value) ? "Chưa giải trình" : value.Trim()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot7_AllIssue85RegressionGatesRemainPresent()
    {
        foreach (var path in new[]
        {
            new[] { "tests", "CongTy.UnitTests", "WorkforceTimesheetQuickActionsIssue85Lot1Tests.cs" },
            new[] { "tests", "CongTy.UnitTests", "Issue85Lot2ExportTemplateParityTests.cs" },
            new[] { "tests", "CongTy.UnitTests", "Issue85Lot3ActualDocumentPrintParityTests.cs" },
            new[] { "tests", "CongTy.UnitTests", "Issue85Lot4OperationalExportParityTests.cs" },
            new[] { "tests", "CongTy.UnitTests", "Issue85Lot5ReportExportParityTests.cs" },
            new[] { "tests", "CongTy.UnitTests", "Issue85Lot6OfficeFormsParityTests.cs" }
        })
            Assert.IsTrue(File.Exists(Path.Combine(new[] { RepoRoot() }.Concat(path).ToArray())));
    }

    [TestMethod]
    public void Lot7_FinalAuditLocksNoBackendDatabaseOrProductionDeployment()
    {
        var audit = Read("docs", "parity", "ISSUE_85_LOT7_FINAL_HARDENING_AUDIT.md");

        StringAssert.Contains(audit, "Không cần thay đổi backend, DB hay migration");
        StringAssert.Contains(audit, "không deploy production");
        StringAssert.Contains(audit, "35 form / 5 nhóm / 13 XLSX / 29 PDF");
        StringAssert.Contains(audit, "83 screens / 333 Web routes / 93 API source files / 441 endpoint candidates / 233 permissions / 327 mutation candidates");
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CongTy.Desktop.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        Assert.Fail("Không tìm thấy root Desktop.");
        return string.Empty;
    }
}
