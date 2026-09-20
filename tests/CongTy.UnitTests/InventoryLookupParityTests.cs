using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryLookupParityTests
{
    [TestMethod]
    public void Presentation_FiltersZeroRowsAndFormatsPackageBreakdown()
    {
        var row = new InventoryBalanceData
        {
            WarehouseId = "11111111-1111-4111-8111-111111111111",
            WarehouseCode = "K01",
            WarehouseName = "Kho chính",
            BaseVariantId = "22222222-2222-4222-8222-222222222222",
            BaseSku = "SKU-01",
            ProductName = "Sản phẩm 01",
            BaseUnitName = "chai",
            PackageUnitName = "thùng",
            PackageConversionToBase = "24",
            OnHandQuantity = "50",
            ReservedQuantity = "0",
            AvailableQuantity = "50"
        };

        Assert.IsTrue(InventoryLookupPresentation.HasDisplayableBalance(row));
        Assert.AreEqual("Quy cách: 1 thùng = 24 chai", InventoryLookupPresentation.PackageRule(row));
        Assert.AreEqual("2 thùng + 2 chai", InventoryLookupPresentation.PackageBreakdown("50", row));
        Assert.AreEqual("50 chai", InventoryLookupPresentation.QuantityWithUnit("50", row));

        var zero = row with { OnHandQuantity = "0", ReservedQuantity = "0" };
        Assert.IsFalse(InventoryLookupPresentation.HasDisplayableBalance(zero));

        var heldByBusiness = zero with { BusinessHeldQuantity = "2" };
        Assert.IsTrue(InventoryLookupPresentation.HasDisplayableBalance(heldByBusiness));

        var businessOverridesLegacyReserved = zero with { ReservedQuantity = "5", BusinessHeldQuantity = "0" };
        Assert.IsFalse(InventoryLookupPresentation.HasDisplayableBalance(businessOverridesLegacyReserved));
    }

    [TestMethod]
    public void Presentation_GroupsWarehouseSkuAndDoesNotSplitGroupAcrossPages()
    {
        var template = new InventoryBalanceData
        {
            WarehouseId = "11111111-1111-4111-8111-111111111111",
            WarehouseCode = "K01",
            WarehouseName = "Kho chính",
            BaseVariantId = "22222222-2222-4222-8222-222222222222",
            BaseSku = "SKU-01",
            ProductName = "Sản phẩm 01",
            OnHandQuantity = "1",
            ReservedQuantity = "1",
            AvailableQuantity = "0"
        };

        var firstGroup = Enumerable.Range(0, 60)
            .Select(index => template with { LocationId = $"A-{index:000}" })
            .ToArray();
        var secondGroup = Enumerable.Range(0, 60)
            .Select(index => template with
            {
                WarehouseId = "33333333-3333-4333-8333-333333333333",
                WarehouseCode = "K02",
                BaseVariantId = "44444444-4444-4444-8444-444444444444",
                BaseSku = "SKU-02",
                LocationId = $"B-{index:000}"
            })
            .ToArray();

        var pages = InventoryLookupPresentation.PaginateBalanceGroups(firstGroup.Concat(secondGroup), 100);
        Assert.HasCount(2, pages);
        Assert.HasCount(60, pages[0]);
        Assert.HasCount(60, pages[1]);

        var enriched = firstGroup
            .Select(row => row with { BusinessHeldQuantity = "7", BusinessAvailableQuantity = "53" })
            .ToArray();
        Assert.AreEqual("7", InventoryLookupPresentation.BusinessHeldQuantity(enriched));
        Assert.AreEqual("53", InventoryLookupPresentation.BusinessAvailableQuantity(enriched));
        Assert.AreEqual("60", InventoryLookupPresentation.BusinessHeldQuantity(firstGroup));
        Assert.AreEqual("0", InventoryLookupPresentation.BusinessAvailableQuantity(firstGroup));
    }

    [TestMethod]
    public void Source_PreservesWebTabsColumnsPagingAndDocumentDialog()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupView.xaml.cs");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryContracts.cs");

        var balances = view.IndexOf("Content=\"Tồn kho\"", StringComparison.Ordinal);
        var history = view.IndexOf("Content=\"Lịch sử kho\"", StringComparison.Ordinal);
        Assert.IsGreaterThan(balances, history);

        foreach (var header in new[]
        {
            "Header=\"Kho / vị trí\"",
            "Header=\"Sản phẩm / SKU\"",
            "Header=\"Lô\"",
            "Header=\"Hạn dùng\"",
            "Header=\"Tồn kho\"",
            "Header=\"Đã giữ cho đơn\"",
            "Header=\"Có thể xuất\"",
            "Header=\"Ngày ghi nhận\"",
            "Header=\"Nhân viên\"",
            "Header=\"Thao tác\"",
            "Header=\"Số lượng thay đổi\"",
            "Header=\"Mã chứng từ\""
        })
        {
            StringAssert.Contains(view, header);
        }

        StringAssert.Contains(view, "Content=\"Xem lịch sử\"");
        StringAssert.Contains(view, "Content=\"Làm mới dữ liệu\"");
        StringAssert.Contains(view, "Content=\"Làm mới lịch sử\"");
        StringAssert.Contains(view, "Text=\"TỒN HIỆN TẠI TẠI KHO\"");
        StringAssert.Contains(view, "Text=\"VỊ TRÍ\"");
        StringAssert.Contains(view, "Text=\"LÔ\"");

        StringAssert.Contains(vm, "private const int PageSize = 100;");
        StringAssert.Contains(vm, "private const int HistoryPageSize = 50;");
        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.read\")");
        StringAssert.Contains(vm, "InventoryLookupPresentation.SumWarehouseOnHand");
        StringAssert.Contains(vm, "InventoryLookupPresentation.PaginateBalanceGroups(filtered, PageSize)");
        StringAssert.Contains(vm, "InventoryLookupPresentation.GroupBalancesByWarehouseSku(pageRows)");
        StringAssert.Contains(vm, "InventoryLookupPresentation.BusinessHeldQuantity(group)");
        StringAssert.Contains(vm, "InventoryLookupPresentation.BusinessAvailableQuantity(group)");
        StringAssert.Contains(vm, "index == 0");
        StringAssert.Contains(vm, "rows.Take(HistoryPageSize)");

        StringAssert.Contains(view, "Text=\"{Binding Held}\"");
        StringAssert.Contains(view, "Text=\"Theo kho\"");
        StringAssert.Contains(view, "Visibility=\"{Binding IsBusinessSummaryRow");
        Assert.IsFalse(view.Contains("Text=\"{Binding Reserved}\"", StringComparison.Ordinal));

        StringAssert.Contains(contracts, "[JsonPropertyName(\"business_on_hand_quantity\")]");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"business_held_quantity\")]");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"business_available_quantity\")]");

        StringAssert.Contains(service, "/api/inventory/balances");
        StringAssert.Contains(service, "/api/inventory/balances/history?warehouseId=");
        StringAssert.Contains(service, "scope=warehouse");
        StringAssert.Contains(service, "HistoryPageSize + 1");

        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "Key.Escape");
    }

    [TestMethod]
    public void Shell_UsesDedicatedLookupWorkspaceNotLegacyCombinedRoute()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "public bool CanViewInventoryBalances");
        StringAssert.Contains(shell, "public async Task NavigateInventoryLookupAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 13");
        StringAssert.Contains(shell, "_inventoryLookup.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "\"inventory.balances\" => \"Tra cứu tồn kho\"");

        StringAssert.Contains(main, "Visibility=\"{Binding CanViewInventoryBalances");
        StringAssert.Contains(main, "x:Name=\"InventoryLookupHost\"");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateInventoryLookupAsync()");
        Assert.IsFalse(
            mainCode.Contains("NavigateInventoryAsync(0, \"inventory.balances\")", StringComparison.Ordinal));

        StringAssert.Contains(app, "AddSingleton<InventoryLookupViewModel>()");
        StringAssert.Contains(app, "AddSingleton<InventoryLookupView>()");
    }

    [TestMethod]
    public void Ui_DoesNotExposeTechnicalIdentifiers()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLookupView.xaml");
        Assert.IsFalse(view.Contains("movement_id", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("baseVariantId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("requestId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
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
