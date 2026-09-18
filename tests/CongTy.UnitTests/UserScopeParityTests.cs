using CongTy.Contracts;
using CongTy.Desktop.Access;

namespace CongTy.UnitTests;

[TestClass]
public sealed class UserScopeParityTests
{
    [TestMethod]
    public void Lot3_Service_UsesCanonicalBackendRoutesAndSharedIdempotencyHeader()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "UserScopeService.cs");

        StringAssert.Contains(service, "\"/api/branches?limit=1000&offset=0\"");
        StringAssert.Contains(service, "\"/api/warehouses?limit=1000&offset=0\"");
        StringAssert.Contains(service, "$\"/api/internal-auth/users/{EscapedUserId(userId)}/scopes\"");
        StringAssert.Contains(service, "PutIdempotentDataAsync<UserScopeReplaceRequest, UserScopeReplaceResult>");
        Assert.IsFalse(service.Contains("/api/access/users/{EscapedUserId(userId)}/scopes", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot3_Contracts_MatchScopeWireShape()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "UserScopeContracts.cs");

        foreach (var value in new[]
        {
            "\"branch_id\"",
            "\"branchIds\"",
            "\"warehouseIds\"",
            "\"territoryIds\"",
            "\"scopes\"",
            "\"userId\""
        })
            StringAssert.Contains(contracts, value);
    }

    [TestMethod]
    public void ScopeRules_SelectingWarehouseAlsoSelectsItsBranch()
    {
        var warehouse = new UserScopeWarehouseData
        {
            Id = "warehouse-a",
            BranchId = "branch-a"
        };

        var state = UserScopeSelectionRules.SetWarehouse(
            warehouse,
            true,
            [],
            []);

        Assert.IsTrue(state.BranchIds.Contains("branch-a", StringComparer.Ordinal));
        Assert.IsTrue(state.WarehouseIds.Contains("warehouse-a", StringComparer.Ordinal));
    }

    [TestMethod]
    public void ScopeRules_DeselectingBranchRemovesWarehousesInThatBranchOnly()
    {
        var warehouses = new[]
        {
            new UserScopeWarehouseData { Id = "warehouse-a1", BranchId = "branch-a" },
            new UserScopeWarehouseData { Id = "warehouse-a2", BranchId = "branch-a" },
            new UserScopeWarehouseData { Id = "warehouse-b1", BranchId = "branch-b" }
        };

        var state = UserScopeSelectionRules.SetBranch(
            "branch-a",
            false,
            ["branch-a", "branch-b"],
            ["warehouse-a1", "warehouse-a2", "warehouse-b1"],
            warehouses);

        Assert.IsFalse(state.BranchIds.Contains("branch-a", StringComparer.Ordinal));
        Assert.IsTrue(state.BranchIds.Contains("branch-b", StringComparer.Ordinal));
        Assert.IsFalse(state.WarehouseIds.Contains("warehouse-a1", StringComparer.Ordinal));
        Assert.IsFalse(state.WarehouseIds.Contains("warehouse-a2", StringComparer.Ordinal));
        Assert.IsTrue(state.WarehouseIds.Contains("warehouse-b1", StringComparer.Ordinal));
    }

    [TestMethod]
    public void ScopeRules_DeselectingWarehouseKeepsItsBranch()
    {
        var warehouse = new UserScopeWarehouseData
        {
            Id = "warehouse-a",
            BranchId = "branch-a"
        };

        var state = UserScopeSelectionRules.SetWarehouse(
            warehouse,
            false,
            ["branch-a"],
            ["warehouse-a"]);

        Assert.IsTrue(state.BranchIds.Contains("branch-a", StringComparer.Ordinal));
        Assert.IsFalse(state.WarehouseIds.Contains("warehouse-a", StringComparer.Ordinal));
    }

    [TestMethod]
    public void Lot3_View_PreservesWebScopeSurface()
    {
        var directory = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserDirectoryView.xaml");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserScopeView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserScopeViewModel.cs");

        StringAssert.Contains(directory, "Header=\"Phạm vi chi nhánh &amp; kho\"");
        StringAssert.Contains(directory, "x:Name=\"UserScopeHost\"");
        Assert.IsFalse(directory.Contains("Phần phạm vi sẽ được bật ở Lô 3", StringComparison.Ordinal));

        foreach (var text in new[]
        {
            "Tìm tài khoản",
            "Tài khoản quản trị — toàn Công Ty",
            "Chi nhánh",
            "Kho hàng",
            "kho hiệu lực"
        })
            StringAssert.Contains(view, text);

        StringAssert.Contains(viewModel, "Lưu phạm vi");
    }

    [TestMethod]
    public void Lot3_ViewModel_UsesRequiredPermissionsAndOwnerProtection()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserScopeViewModel.cs");

        StringAssert.Contains(viewModel, "core.user.read");
        StringAssert.Contains(viewModel, "core.user-role.write");
        StringAssert.Contains(viewModel, "core.branch.read");
        StringAssert.Contains(viewModel, "core.warehouse.read");
        StringAssert.Contains(viewModel, "OwnerFullScope");
        StringAssert.Contains(viewModel, "!OwnerFullScope");
        StringAssert.Contains(viewModel, "SECURITY_OWNER_PROTECTED");
        StringAssert.Contains(viewModel, "Toàn Công Ty");
    }

    [TestMethod]
    public void Lot3_Idempotency_ReusesKeyForSameScopeIntent()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserScopeViewModel.cs");

        StringAssert.Contains(viewModel, "_mutationKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(\"access-user-scopes\")");
        StringAssert.Contains(viewModel, "_idempotencyKeys.IsValid(created)");
        StringAssert.Contains(viewModel, "ForgetMutationKey(intent)");
    }

    [TestMethod]
    public void Lot3_SendsZeroScopeAndNoTerritory()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Access", "UserScopeViewModel.cs");

        StringAssert.Contains(viewModel, "new UserScopeSet(branchIds, warehouseIds, [])");
        StringAssert.Contains(viewModel, "Đã lưu phạm vi trống.");
        Assert.IsFalse(viewModel.Contains("expectedUpdatedAt", StringComparison.OrdinalIgnoreCase));
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
