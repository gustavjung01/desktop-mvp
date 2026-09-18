using CongTy.ApiClient;

namespace CongTy.UnitTests;

[TestClass]
public sealed class EmployeeDirectoryMutationParityTests
{
    [TestMethod]
    public void Lot2_MutationService_UsesCanonicalGeneratorCompatibleIdempotentCalls()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "EmployeeDirectoryMutationService.cs");

        StringAssert.Contains(service, "PostIdempotentDataAsync<EmployeeDirectoryCreateRequest, EmployeeDirectoryData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<EmployeeDirectoryUpdateRequest, EmployeeDirectoryData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<EmployeeDirectoryToggleRequest, EmployeeDirectoryData>");
        StringAssert.Contains(service, "\"/api/employees\"");
        StringAssert.Contains(service, "\"/api/employees/{Uri.EscapeDataString(employeeId.Trim())}\"");
    }

    [TestMethod]
    public void Lot2_Contracts_MatchCreateUpdateAndStatusWireShape()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "EmployeeDirectoryContracts.cs");

        foreach (var field in new[]
        {
            "\"code\"",
            "\"fullName\"",
            "\"jobTitle\"",
            "\"phone\"",
            "\"email\"",
            "\"branchId\"",
            "\"expectedUpdatedAt\"",
            "\"isActive\""
        })
            StringAssert.Contains(contracts, field);
    }

    [TestMethod]
    public void Lot2_ViewModel_ReusesCanonicalKeyForSameMutationSlot()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(slot, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.IsValid(created)");
        StringAssert.Contains(viewModel, "\"employee-create\"");
        StringAssert.Contains(viewModel, "\"employee-update\"");
        StringAssert.Contains(viewModel, "\"employee-status\"");
        StringAssert.Contains(viewModel, "ExpectedUpdatedAt");
    }

    [TestMethod]
    public void Lot2_View_WiresSaveToggleConfirmAndConflictReload()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryView.xaml.cs");

        foreach (var handler in new[]
        {
            "Save_OnClick",
            "Toggle_OnClick",
            "ConfirmToggle_OnClick",
            "CancelToggle_OnClick",
            "ReloadAfterConflict_OnClick"
        })
        {
            StringAssert.Contains(view, handler);
            StringAssert.Contains(codeBehind, handler);
        }

        StringAssert.Contains(view, "XÁC NHẬN TRẠNG THÁI");
        StringAssert.Contains(view, "TẢI LẠI DỮ LIỆU");
    }

    [TestMethod]
    public void Lot2_ViewModel_HandlesConflictAndProtectedOwnerInOfficeLanguage()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "CONFLICT");
        StringAssert.Contains(viewModel, "SECURITY_OWNER_PROTECTED");
        StringAssert.Contains(viewModel, "Hồ sơ nhân sự vừa có thay đổi.");
        StringAssert.Contains(viewModel, "Chủ sở hữu hệ thống");
        Assert.IsFalse(viewModel.Contains("Security Owner employee", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanonicalProvider_ProducesOnlyContractSafeCharacters()
    {
        var provider = new CanonicalIdempotencyKeyProvider();
        var key = provider.Create("employee-create");

        Assert.IsTrue(provider.IsValid(key));
        Assert.IsTrue(key.All(character =>
            char.IsLetterOrDigit(character) || character is '.' or '_' or '-'));
        Assert.IsLessThanOrEqualTo(key.Length, 128);
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
