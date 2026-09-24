namespace CongTy.UnitTests;

[TestClass]
public sealed class WorkforceWorkPolicyLot9Tests
{
    [TestMethod]
    public void Lot9_UsesCanonicalWorkPolicyApiAndIdempotentMutation()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "WorkPolicyService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkPolicyViewModel.cs");

        StringAssert.Contains(service, ""/api/workforce/policies"");
        StringAssert.Contains(service, "PostIdempotentDataAsync<SaveWorkPolicyRequest, WorkPolicyDetailData>");
        StringAssert.Contains(viewModel, "desktop-work-policy-save");
        StringAssert.Contains(viewModel, "JsonSerializer.Serialize(request)");
        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(viewModel, "_mutationKeys.Remove(slot)");
    }

    [TestMethod]
    public void Lot9_CoversCurrentWebPolicyContract()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "WorkPolicyContracts.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkPolicyPresentation.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Workforce", "WorkPolicyView.xaml");

        foreach (var method in new[] { "QR_FACE", "BOTH", "FACE_MANUAL", "ALL", "NONE" })
            StringAssert.Contains(presentation, method);

        StringAssert.Contains(contracts, "attendance_basis");
        StringAssert.Contains(contracts, "minimum_full_day_minutes");
        StringAssert.Contains(contracts, "minimum_half_day_minutes");
        StringAssert.Contains(contracts, "supersedes_policy_id");
        StringAssert.Contains(view, "Áp dụng ngay");
        StringAssert.Contains(view, "Chọn ngày áp dụng");
        StringAssert.Contains(view, "Cập nhật trong cùng ngày vẫn tạo phiên bản chính sách mới");
        StringAssert.Contains(view, "Lịch sử thay đổi");
    }

    [TestMethod]
    public void Lot9_WiresRealWorkspaceAndRemovesPolicyPlaceholder()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.WorkPolicy.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.WorkPolicy.cs");

        StringAssert.Contains(xaml, "Click="WorkPolicies_OnClick"");
        Assert.IsFalse(xaml.Contains("ToolTip="Sẽ được triển khai ở Lô 2"><TextBlock Text="Chính sách làm việc"", StringComparison.Ordinal));
        StringAssert.Contains(host, "WorkspaceSlots.WorkPolicy");
        StringAssert.Contains(shell, "SetSelectedNavigation("workforce.policies")");
        StringAssert.Contains(shell, "core.work-policy.read");
    }

    [TestMethod]
    public void Lot9_DoesNotAddBackendOrDatabaseMutation()
    {
        var audit = ReadRepoFile("docs", "parity", "WORKFORCE_LOT9_WORK_POLICY_AUDIT.md");
        StringAssert.Contains(audit, "af4acc24bc411d3206d07256f6119472068c01c3");
        StringAssert.Contains(audit, "không sửa backend");
        StringAssert.Contains(audit, "không sửa DB");
        StringAssert.Contains(audit, "không migration");
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
