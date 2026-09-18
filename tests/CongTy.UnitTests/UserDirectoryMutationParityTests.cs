using CongTy.ApiClient;

namespace CongTy.UnitTests;

[TestClass]
public sealed class UserDirectoryMutationParityTests
{
    [TestMethod]
    public void Lot2_MutationService_UsesCanonicalBackendRoutes()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "UserDirectoryMutationService.cs");

        StringAssert.Contains(service, "PostIdempotentDataAsync<AccessUserCreateRequest, AccessUserData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<AccessUserRolesRequest, AccessUserData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<AccessUserStatusRequest, AccessUserData>");
        StringAssert.Contains(service, "\"/api/access/users\"");
        StringAssert.Contains(service, "$\"/api/access/users/{EscapedUserId(userId)}/roles\"");
        StringAssert.Contains(service, "$\"/api/internal-auth/users/{EscapedUserId(userId)}/credential\"");
        StringAssert.Contains(service, "PutDataAsync<AccessUserCredentialRequest, AccessUserCredentialResult>");
        Assert.IsFalse(service.Contains("PutIdempotentDataAsync<AccessUserCredentialRequest", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("/api/access/users/{EscapedUserId(userId)}/credential", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("/scopes", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot2_Contracts_MatchCreateRolesStatusAndCredentialWireShape()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "UserDirectoryContracts.cs");

        foreach (var field in new[]
        {
            "\"loginName\"",
            "\"employeeId\"",
            "\"isActive\"",
            "\"roleIds\"",
            "\"expectedUpdatedAt\"",
            "\"password\"",
            "\"credentialUpdated\"",
            "\"revokedSessionCount\""
        })
            StringAssert.Contains(contracts, field);
    }

    [TestMethod]
    public void Lot2_CreateFlow_StagesDisabledAccountRolesCredentialThenActivation()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");

        var create = viewModel.IndexOf("_mutationService.CreateAsync", StringComparison.Ordinal);
        var roles = viewModel.IndexOf("_mutationService.ReplaceRolesAsync", create, StringComparison.Ordinal);
        var credential = viewModel.IndexOf("_mutationService.SetCredentialAsync", roles, StringComparison.Ordinal);
        var status = viewModel.IndexOf("_mutationService.UpdateStatusAsync", credential, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, create);
        Assert.IsGreaterThan(create, roles);
        Assert.IsGreaterThan(roles, credential);
        Assert.IsGreaterThan(credential, status);
        StringAssert.Contains(viewModel, "DraftEmployeeId.Trim(),\n                false");
        StringAssert.Contains(viewModel, "Tài khoản đã được giữ an toàn ở trạng thái hiện tại");
        StringAssert.Contains(viewModel, "IsCreateMode = false");
    }

    [TestMethod]
    public void Lot2_Idempotency_UsesSharedProviderAndReusesSameIntentKey()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(scope)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.IsValid(created)");
        StringAssert.Contains(viewModel, "\"access-user-create\"");
        StringAssert.Contains(viewModel, "\"access-user-roles\"");
        StringAssert.Contains(viewModel, "\"access-user-status\"");
    }

    [TestMethod]
    public void Lot2_ViewModel_EnforcesPasswordAndOptimisticConcurrency()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryView.xaml");

        StringAssert.Contains(viewModel, "value.Length is >= 10 and <= 256");
        StringAssert.Contains(viewModel, "ExpectedUpdatedAt");
        StringAssert.Contains(viewModel, "CONFLICT");
        StringAssert.Contains(viewModel, "ShowReloadAfterConflict");
        StringAssert.Contains(view, "TẢI LẠI DỮ LIỆU");
    }

    [TestMethod]
    public void Lot2_View_WiresCreateEditPasswordRolesStatusAndConfirmation()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");
        var presentation = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryPresentation.cs");

        foreach (var handler in new[]
        {
            "Create_OnClick",
            "Edit_OnClick",
            "Password_OnChanged",
            "Save_OnClick",
            "ReloadAfterConflict_OnClick",
            "Toggle_OnClick",
            "CancelToggle_OnClick",
            "ConfirmToggle_OnClick"
        })
        {
            StringAssert.Contains(view, handler);
            StringAssert.Contains(codeBehind, handler);
        }

        StringAssert.Contains(view, "Text=\"{Binding PasswordLabel}\"");
        StringAssert.Contains(view, "Text=\"Vai trò\"");
        StringAssert.Contains(view, "Text=\"{Binding ToggleTitle}\"");
        StringAssert.Contains(viewModel, "Mật khẩu đăng nhập");
        StringAssert.Contains(viewModel, "Xác nhận thay đổi trạng thái");
        StringAssert.Contains(presentation, "Vai trò đã ngừng sử dụng — bỏ chọn để thu hồi");
    }

    [TestMethod]
    public void Lot2_MapsProtectedOwnerAndDuplicateErrorsToOfficeLanguage()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryViewModel.cs");

        StringAssert.Contains(viewModel, "SECURITY_OWNER_PROTECTED");
        StringAssert.Contains(viewModel, "Tài khoản Chủ sở hữu hệ thống đang được bảo vệ");
        StringAssert.Contains(viewModel, "DUPLICATE_LOGIN");
        StringAssert.Contains(viewModel, "DUPLICATE_EMPLOYEE");
        Assert.IsFalse(viewModel.Contains("installation hiện tại", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CanonicalProvider_ProducesContractSafeUserMutationKeys()
    {
        var provider = new CanonicalIdempotencyKeyProvider();
        foreach (var scope in new[] { "access-user-create", "access-user-roles", "access-user-status" })
        {
            var key = provider.Create(scope);
            Assert.IsTrue(provider.IsValid(key));
            Assert.IsTrue(key.All(character =>
                char.IsLetterOrDigit(character) || character is '.' or '_' or '-'));
            Assert.IsLessThanOrEqualTo(128, key.Length);
        }
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
