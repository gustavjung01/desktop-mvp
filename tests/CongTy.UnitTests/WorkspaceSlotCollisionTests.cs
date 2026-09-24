using System.Text.RegularExpressions;

namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkspaceSlotCollisionTests
{
    [TestMethod]
    public void DynamicTailWorkspaceSlots_AreUniqueAndUsedByBothNavigationAndHost()
    {
        var slots = ReadRepoFile("src", "CongTy.Desktop", "Shell", "WorkspaceSlots.cs");
        var expected = new Dictionary<string, int>
        {
            ["SalesSettlement"] = 53,
            ["Leave"] = 54,
            ["OvertimeCloseout"] = 55,
            ["PayrollFoundation"] = 56,
            ["UserDirectory"] = 57,
            ["WorkSchedule"] = 58,
            ["DesktopApp"] = 59,
            ["WorkPolicy"] = 60,
            ["Attendance"] = 61,
            ["Timesheet"] = 62,
            ["AttendanceAdjustment"] = 63,
            ["AttendanceViolation"] = 64,
        };

        var parsed = Regex.Matches(slots, @"public const int (\w+) = (\d+);")
            .ToDictionary(
                match => match.Groups[1].Value,
                match => int.Parse(match.Groups[2].Value));

        foreach (var item in expected)
        {
            Assert.IsTrue(parsed.TryGetValue(item.Key, out var value), $"Thiếu workspace slot {item.Key}.");
            Assert.AreEqual(item.Value, value, $"Sai workspace slot {item.Key}.");
        }

        Assert.AreEqual(
            parsed.Count,
            parsed.Values.Distinct().Count(),
            "Có workspace dùng trùng SelectedWorkspaceIndex.");

        AssertWorkspacePair("SalesSettlement", "MainWindow.SalesSettlement.cs", "ShellViewModel.SalesSettlement.cs");
        AssertWorkspacePair("Leave", "MainWindow.Leave.cs", "ShellViewModel.Leave.cs");
        AssertWorkspacePair("OvertimeCloseout", "MainWindow.OvertimeCloseout.cs", "ShellViewModel.OvertimeCloseout.cs");
        AssertWorkspacePair("PayrollFoundation", "MainWindow.PayrollFoundation.cs", "ShellViewModel.PayrollFoundation.cs");
        AssertWorkspacePair("UserDirectory", "MainWindow.UserDirectory.cs", "ShellViewModel.UserDirectory.cs");
        AssertWorkspacePair("WorkSchedule", "MainWindow.WorkSchedule.cs", "ShellViewModel.WorkSchedule.cs");
        AssertWorkspacePair("WorkPolicy", "MainWindow.WorkPolicy.cs", "ShellViewModel.WorkPolicy.cs");
        AssertWorkspacePair("Attendance", "MainWindow.Attendance.cs", "ShellViewModel.Attendance.cs");
        AssertWorkspacePair("Timesheet", "MainWindow.Timesheet.cs", "ShellViewModel.Timesheet.cs");
        AssertWorkspacePair("AttendanceAdjustment", "MainWindow.AttendanceAdjustment.cs", "ShellViewModel.AttendanceAdjustment.cs");
        AssertWorkspacePair("AttendanceViolation", "MainWindow.AttendanceViolation.cs", "ShellViewModel.AttendanceViolation.cs");

        var settingsHost = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.DataBackup.cs");
        var settingsShell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.DataBackup.cs");
        StringAssert.Contains(settingsHost, "workspaceTabs.Items[WorkspaceSlots.DesktopApp]");
        StringAssert.Contains(settingsShell, "SelectedWorkspaceIndex == WorkspaceSlots.DesktopApp");
        StringAssert.Contains(settingsShell, "WorkspaceSlots.DesktopApp,");
    }

    [TestMethod]
    public void DynamicTailWorkspaces_DoNotHardcode53To64OutsideWorkspaceSlots()
    {
        var shellDirectory = FindRepoDirectory("src", "CongTy.Desktop", "Shell");
        var hardcodedHost = new Regex(@"workspaceTabs\.Items\[((?:5[3-9])|6[0-4])\]");
        var hardcodedSelection = new Regex(@"SelectedWorkspaceIndex\s*(?:==|=)\s*((?:5[3-9])|6[0-4])");

        foreach (var path in Directory.GetFiles(shellDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(path).Equals("WorkspaceSlots.cs", StringComparison.Ordinal))
                continue;

            var source = File.ReadAllText(path);
            Assert.IsFalse(
                hardcodedHost.IsMatch(source),
                $"Không được hard-code dynamic workspace slot trong {Path.GetFileName(path)}.");
            Assert.IsFalse(
                hardcodedSelection.IsMatch(source),
                $"Không được hard-code SelectedWorkspaceIndex 53-64 trong {Path.GetFileName(path)}.");
        }
    }

    private static void AssertWorkspacePair(string slot, string hostFile, string shellFile)
    {
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", hostFile);
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", shellFile);
        StringAssert.Contains(host, $"workspaceTabs.Items[WorkspaceSlots.{slot}]");
        StringAssert.Contains(shell, $"WorkspaceSlots.{slot}");
    }

    private static string FindRepoDirectory(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (Directory.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        Assert.Fail($"Không tìm thấy thư mục trong repo: {string.Join("/", parts)}");
        return string.Empty;
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
