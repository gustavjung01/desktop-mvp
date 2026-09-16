using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryLotsParityTests
{
    [TestMethod]
    public void Presentation_PreservesWebLotSearchAndOfficeDisplay()
    {
        var row = new InventoryLotData
        {
            Id = "11111111-1111-4111-8111-111111111111",
            BaseVariantId = "22222222-2222-4222-8222-222222222222",
            BaseSku = "SKU-BASE",
            BaseVariantName = "Chai",
            ProductCode = "SP01",
            ProductName = "Nước uống",
            LotCode = "LOT-09",
            NormalizedLotCode = "LOT-09",
            ManufacturedDate = "2026-09-01",
            ExpiryDate = "2027-09-01",
            SupplierLotReference = "NCC-LOT-88",
            CreatedAt = "2026-09-16T01:02:03Z"
        };

        var search = InventoryLotsPresentation.SearchText(row);
        StringAssert.Contains(search, "sku-base");
        StringAssert.Contains(search, "lot-09");
        StringAssert.Contains(search, "ncc-lot-88");
        StringAssert.Contains(search, "nước uống");

        Assert.AreEqual("01/09/2027", InventoryLotsPresentation.Date(row.ExpiryDate));
        Assert.AreEqual("Không có", InventoryLotsPresentation.Date(null));

        var display = new InventoryLotsRow(1, row);
        Assert.AreEqual(string.Empty, display.NormalizedLotCode);
        Assert.AreEqual("SP01 · Nước uống", display.Product);
    }

    [TestMethod]
    public void Ui_PreservesWebSearchRefreshAndColumnOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLotsView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLotsView.xaml.cs");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLotsViewModel.cs");
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryService.cs");

        StringAssert.Contains(view, "ToolTip=\"Tìm theo SKU, mã lô hoặc hạn dùng\"");
        StringAssert.Contains(view, "Content=\"{Binding RefreshText}\"");
        StringAssert.Contains(view, "Text=\"Chưa có lô hàng nào.\"");

        var headers = new[]
        {
            "Header=\"STT\"",
            "Header=\"SKU\"",
            "Header=\"Lô\"",
            "Header=\"Hạn dùng\"",
            "Header=\"Ngày SX\"",
            "Header=\"Tham chiếu nhà cung cấp\"",
            "Header=\"Tạo lúc\""
        };
        var previous = -1;
        foreach (var header in headers)
        {
            var index = view.IndexOf(header, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index);
            previous = index;
        }

        StringAssert.Contains(vm, "_access.HasPermission(\"core.inventory.lot.read\")");
        StringAssert.Contains(vm, "_service.ListLotsAsync()");
        StringAssert.Contains(service, "ReadAllAsync<InventoryLotData>(\"/api/inventory/lots\"");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");

        Assert.IsFalse(view.Contains("UUID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("metadata", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Shell_UsesDedicatedLotsWorkspace()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "public bool CanViewInventoryLots");
        StringAssert.Contains(shell, "public async Task NavigateInventoryLotsAsync()");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 16");
        StringAssert.Contains(shell, "_inventoryLots.EnsureLoadedAsync()");
        StringAssert.Contains(shell, "\"inventory.lots\" => \"Lô hàng\"");
        StringAssert.Contains(shell, "\"inventory.lots\" => \"TỒN KHO, LÔ VÀ NHẬP ĐẦU KỲ\"");

        StringAssert.Contains(main, "Visibility=\"{Binding CanViewInventoryLots");
        StringAssert.Contains(main, "x:Name=\"InventoryLotsHost\"");
        StringAssert.Contains(mainCode, "await _viewModel.NavigateInventoryLotsAsync()");
        Assert.IsFalse(
            mainCode.Contains("NavigateInventoryAsync(2, \"inventory.lots\")", StringComparison.Ordinal));

        StringAssert.Contains(app, "AddSingleton<InventoryLotsViewModel>()");
        StringAssert.Contains(app, "AddSingleton<InventoryLotsView>()");
    }

    [TestMethod]
    public void Workspace_DoesNotInventLotMutation()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLotsView.xaml");
        var vm = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryLotsViewModel.cs");

        Assert.IsFalse(view.Contains("Tạo lô", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Sửa lô", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Xóa lô", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(vm.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(vm.Contains("SaveAsync", StringComparison.Ordinal));
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
