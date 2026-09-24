namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkspaceSlotCollisionTests
{
    [TestMethod]
    public void TailWorkspaces_DoNotReuseAccountingOrWorkforceSlots()
    {
        var slots = ReadRepoFile("src", "CongTy.Desktop", "Shell", "WorkspaceSlots.cs");
        StringAssert.Contains(slots, "UserDirectory = 57");
        StringAssert.Contains(slots, "WorkSchedule = 58");
        StringAssert.Contains(slots, "DesktopApp = 59");

        var sales = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.SalesSettlement.cs");
        var leave = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.Leave.cs");
        StringAssert.Contains(sales, "SelectedWorkspaceIndex = 53");
        StringAssert.Contains(leave, "SelectedWorkspaceIndex = 54");

        foreach (var file in new[]
        {
            "MainWindow.UserDirectory.cs",
            "ShellViewModel.UserDirectory.cs"
        })
            StringAssert.Contains(ReadRepoFile("src", "CongTy.Desktop", "Shell", file), "WorkspaceSlots.UserDirectory");

        foreach (var file in new[]
        {
            "MainWindow.WorkSchedule.cs",
            "ShellViewModel.WorkSchedule.cs"
        })
            StringAssert.Contains(ReadRepoFile("src", "CongTy.Desktop", "Shell", file), "WorkspaceSlots.WorkSchedule");

        foreach (var file in new[]
        {
            "MainWindow.DataBackup.cs",
            "ShellViewModel.DataBackup.cs"
        })
            StringAssert.Contains(ReadRepoFile("src", "CongTy.Desktop", "Shell", file), "WorkspaceSlots.DesktopApp");
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
