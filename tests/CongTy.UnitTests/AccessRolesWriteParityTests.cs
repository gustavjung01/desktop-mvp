using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AccessRolesWriteParityTests
{
    [TestMethod]
    public void MutationService_UsesOnlyCanonicalIdempotentPostAndPatch()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "AccessRoleMutationService.cs");

        StringAssert.Contains(service, "PostIdempotentDataAsync<AccessRoleCreateRequest, AccessRoleData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<AccessRoleUpdateRequest, AccessRoleData>");
        StringAssert.Contains(service, "PatchIdempotentDataAsync<AccessRoleToggleRequest, AccessRoleData>");
        StringAssert.Contains(service, "\"/api/access/roles\"");
        StringAssert.Contains(service, "\"/api/access/roles/{Uri.EscapeDataString(roleId.Trim())}\"");
        Assert.IsFalse(service.Contains("Guid.NewGuid", StringComparison.Ordinal));
        Assert.IsFalse(service.Contains("Idempotency-Key", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ViewModel_ReusesKeyForSameIntentAndClearsOnlyAfterSuccess()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesViewModel.cs");

        StringAssert.Contains(source, "private readonly Dictionary<string, string> _mutationKeys");
        StringAssert.Contains(source, "if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;");
        StringAssert.Contains(source, "var created = _idempotencyKeys.Create(scope);");
        StringAssert.Contains(source, "_mutationKeys[slot] = created;");
        StringAssert.Contains(source, "_mutationKeys.Remove(slot);");
        StringAssert.Contains(source, "_mutationKeys.Remove(updateSlot);");
        StringAssert.Contains(source, "request.ExpectedUpdatedAt");
        Assert.IsFalse(source.Contains("Guid.NewGuid", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UpdateAndToggle_AlwaysCarryExpectedUpdatedAtAndDoNotOverwriteConflict()
    {
        var source = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesViewModel.cs");

        StringAssert.Contains(source, "new AccessRoleUpdateRequest(");
        StringAssert.Contains(source, "_editingRole.UpdatedAt");
        StringAssert.Contains(source, "new AccessRoleToggleRequest(nextActive, role.UpdatedAt)");
        StringAssert.Contains(source, "exception.StatusCode == HttpStatusCode.Conflict");
        StringAssert.Contains(source, "string.Equals(exception.Code, \"CONFLICT\", StringComparison.Ordinal)");
        StringAssert.Contains(source, "HasConflict = true");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesView.xaml");
        StringAssert.Contains(view, "TẢI LẠI DỮ LIỆU");
    }

    [TestMethod]
    public void View_ProvidesSaveAndExplicitStatusConfirmation()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesView.xaml");
        var codeBehind = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesView.xaml.cs");

        StringAssert.Contains(view, "Click=\"Save_OnClick\"");
        StringAssert.Contains(view, "Click=\"Toggle_OnClick\"");
        StringAssert.Contains(view, "Click=\"ConfirmToggle_OnClick\"");
        StringAssert.Contains(view, "Click=\"CancelToggle_OnClick\"");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "AccessRolesViewModel.cs");
        StringAssert.Contains(viewModel, "Ngừng sử dụng vai trò");
        StringAssert.Contains(viewModel, "đối soát và lịch sử chứng từ");
        StringAssert.Contains(codeBehind, "await ViewModel.SaveAsync()");
        StringAssert.Contains(codeBehind, "await ViewModel.ConfirmToggleAsync()");
    }

    [TestMethod]
    public void MutationContracts_SerializeExactBackendFieldNames()
    {
        var create = new AccessRoleCreateRequest(
            "SALES",
            "Bán hàng",
            "Vai trò bán hàng",
            true,
            false,
            ["core.sales-order.read"]);
        var update = new AccessRoleUpdateRequest(
            "Bán hàng",
            "Vai trò bán hàng",
            true,
            true,
            ["core.sales-order.read"],
            "2026-09-18T00:00:00.000Z");
        var toggle = new AccessRoleToggleRequest(
            false,
            "2026-09-18T00:00:00.000Z");

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using var createJson = JsonDocument.Parse(JsonSerializer.Serialize(create, options));
        using var updateJson = JsonDocument.Parse(JsonSerializer.Serialize(update, options));
        using var toggleJson = JsonDocument.Parse(JsonSerializer.Serialize(toggle, options));

        Assert.AreEqual("SALES", createJson.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(createJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.IsFalse(createJson.RootElement.GetProperty("webLoginChallengeRequired").GetBoolean());
        Assert.HasCount(1, createJson.RootElement.GetProperty("permissionKeys").EnumerateArray().ToArray());

        Assert.AreEqual(
            "2026-09-18T00:00:00.000Z",
            updateJson.RootElement.GetProperty("expectedUpdatedAt").GetString());
        Assert.IsTrue(updateJson.RootElement.GetProperty("webLoginChallengeRequired").GetBoolean());

        Assert.IsFalse(toggleJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.AreEqual(
            "2026-09-18T00:00:00.000Z",
            toggleJson.RootElement.GetProperty("expectedUpdatedAt").GetString());
    }

    [TestMethod]
    public void MainWindow_WiresSharedIdempotencyProviderInsteadOfParallelGenerator()
    {
        var host = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.AccessRoles.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(host, "AccessRoleMutationService");
        StringAssert.Contains(host, "ResolveRequired<ICanonicalIdempotencyKeyProvider>()");
        StringAssert.Contains(app, "AddSingleton<ICanonicalIdempotencyKeyProvider, CanonicalIdempotencyKeyProvider>()");
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
