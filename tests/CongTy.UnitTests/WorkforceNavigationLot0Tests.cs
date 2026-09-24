namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceNavigationLot0Tests
{
    [TestMethod]
    public void Lot0_WorkforceMenu_MatchesCurrentWebOrder_AndKeepsFutureLotsNonInteractive()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var workforce = SliceTemplate(xaml, "WorkforceMenuTemplate");
        var access = SliceTemplate(xaml, "AccessMenuTemplate");
        var labels = new[] { "Chấm công", "Bảng công", "Tăng ca &amp; chốt công", "Tính lương", "Nghỉ và đơn nghỉ", "Xử lý vi phạm công", "Điều chỉnh công", "Danh mục nhân sự", "Ca / lịch làm việc", "Chính sách làm việc" };

        var previous = -1;
        foreach (var label in labels)
        {
            var current = workforce.IndexOf($"Text=\"{label}\"", StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, current, $"Sai thứ tự hoặc thiếu mục Workforce: {label}");
            previous = current;
        }

        StringAssert.Contains(workforce, "Click=\"EmployeeDirectory_OnClick\"");
        StringAssert.Contains(workforce, "Click=\"Attendance_OnClick\"");
        StringAssert.Contains(workforce, "Click=\"PayrollFoundation_OnClick\"");
        Assert.IsFalse(access.Contains("Danh mục nhân sự", StringComparison.Ordinal));
        StringAssert.Contains(access, "Vai trò và phân quyền");
        StringAssert.Contains(access, "Người dùng");
        StringAssert.Contains(xaml, "Text=\"Người dùng &amp; phân quyền\"");
    }

    [TestMethod]
    public void Lot0_WorkforceShell_UsesCanonicalPermissions_WithoutAddingBusinessApiOrMutation()
    {
        var workforce = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkforceNavigation.cs");
        var employee = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.EmployeeDirectory.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        foreach (var permission in new[] { "core.attendance.self.read", "core.attendance-point.manage", "core.attendance.read", "core.overtime.read", "core.payroll.read", "core.leave.read", "core.attendance-violation.resolve", "core.attendance.adjust", "core.work-schedule.read", "core.work-policy.read" })
            StringAssert.Contains(workforce, permission);

        Assert.IsFalse(workforce.Contains("/api/", StringComparison.Ordinal));
        Assert.IsFalse(workforce.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(employee, "SetSelectedNavigation(\"workforce.employees\")");
        StringAssert.Contains(employee, "IsWorkforceOpen = true");
        StringAssert.Contains(shell, "\"workforce\" => !IsWorkforceOpen");
        StringAssert.Contains(shell, "case \"workforce\": IsWorkforceOpen = true");
        StringAssert.Contains(shell, "case \"workforce\": IsWorkforceOpen = false");
    }

    [TestMethod]
    public void Lot0_Audit_LocksCurrentWebBaselineAndDesktopBoundary()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT0_AUDIT.md");
        StringAssert.Contains(audit, "4186ea9638470d2f89882f51de8fa0aa51347654");
        StringAssert.Contains(audit, "83 screens / 331 Web routes / 91 API source files");
        StringAssert.Contains(audit, "421 endpoint candidates / 233 permissions / 312 mutation candidates");
        StringAssert.Contains(audit, "không thêm API nghiệp vụ");
        StringAssert.Contains(audit, "không sửa backend/DB/migration");
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
