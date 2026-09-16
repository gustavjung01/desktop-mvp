using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryTransferParityTests
{
    [TestMethod]
    public void Presentation_PreservesTransferOfficeLabelsAndInventoryScope()
    {
        var balance = new InventoryBalanceData
        {
            WarehouseId = "warehouse-1",
            LocationId = "location-1",
            BaseVariantId = "variant-1",
            LotId = "lot-1",
            BaseSku = "SP-001",
            LocationCode = "A-01",
            LotCode = "LO-01",
            AvailableQuantity = "12.5"
        };

        Assert.AreEqual("warehouse-1|location-1|variant-1|lot-1", TransferPresentation.BalanceKey(balance));
        StringAssert.Contains(TransferPresentation.BalanceLabel(balance), "SP-001 · A-01 · LO-01 · khả dụng 12,5");
        Assert.AreEqual("Nháp", TransferPresentation.Status("draft", false));
        Assert.AreEqual("Đang đi đường", TransferPresentation.Status("dispatched", false));
        Assert.AreEqual("Đã xử lý nhận", TransferPresentation.Status("dispatched", true));
        Assert.IsTrue(TransferPresentation.IsPositive("0.5"));
        Assert.IsFalse(TransferPresentation.IsPositive("0"));
        Assert.IsTrue(TransferPresentation.IsGreaterThan("12.5", "12"));
    }

    [TestMethod]
    public void Source_PreservesTransferWebFlowPermissionsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryTransferService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");
        var access = ReadRepoFile("src", "CongTy.ApiClient", "AccessStateService.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/transfers?limit=500",
            "/api/inventory/transfers/in-transit?limit=1000",
            "/api/inventory/transfers/{Uri.EscapeDataString",
            "/receipts",
            "/approve",
            "/dispatch",
            "/cancel",
            "/approve-damage",
            "/reverse",
            "/close-short"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create($\"transfer-{prefix}\")");
        StringAssert.Contains(viewModel, "_intentKeys.Clear()");
        StringAssert.Contains(viewModel, "KeyFor(\"create\"");
        StringAssert.Contains(viewModel, "KeyFor(\"approve\"");
        StringAssert.Contains(viewModel, "KeyFor(\"dispatch\"");
        StringAssert.Contains(viewModel, "KeyFor(\"cancel\"");
        StringAssert.Contains(viewModel, "KeyFor(\"receive\"");
        StringAssert.Contains(viewModel, "KeyFor(\"damage-approve\"");
        StringAssert.Contains(viewModel, "KeyFor(\"receipt-reverse\"");
        StringAssert.Contains(viewModel, "KeyFor(\"close-short\"");

        foreach (var permission in new[]
        {
            "core.inventory-transfer.read",
            "core.inventory-transfer.create",
            "core.inventory-transfer.approve",
            "core.inventory-transfer.dispatch",
            "core.inventory-transfer.cancel",
            "core.inventory-transfer.receive",
            "core.inventory-transfer.damage-approve",
            "core.inventory-transfer.resolve",
            "core.inventory-transfer.reverse"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        var sections = new[]
        {
            "Phiếu nháp",
            "Chờ xuất kho",
            "Đã xuất chuyển",
            "Dòng đang đi đường",
            "Phiếu chuyển kho",
            "Hàng đang đi đường",
            "Tạo phiếu chuyển kho",
            "Hàng chuyển",
            "Nhận hàng tại kho đích",
            "LẦN NHẬN MỚI",
            "Lịch sử nhận và xử lý",
            "Đóng phần thiếu"
        };
        var cursor = -1;
        foreach (var section in sections)
        {
            var next = xaml.IndexOf(section, cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự khối: {section}");
            cursor = next;
        }

        StringAssert.Contains(xaml, "ToolTip=\"Số phiếu, kho, SKU hoặc lô\"");
        StringAssert.Contains(xaml, "Text=\"TRẠNG THÁI CHỨNG TỪ\"");
        StringAssert.Contains(xaml, "Content=\"Đặt lại\"");
        StringAssert.Contains(xaml, "Text=\"KHO XUẤT\"");
        StringAssert.Contains(xaml, "Text=\"KHO NHẬN\"");
        StringAssert.Contains(xaml, "Content=\"Thêm dòng\"");
        StringAssert.Contains(xaml, "Content=\"{Binding CreateButtonText}\"");
        StringAssert.Contains(xaml, "Content=\"{Binding ApproveButtonText}\"");
        StringAssert.Contains(xaml, "Content=\"{Binding DispatchButtonText}\"");
        StringAssert.Contains(xaml, "Content=\"Lập lần nhận\"");
        StringAssert.Contains(xaml, "Content=\"{Binding ReceiveButtonText}\"");
        StringAssert.Contains(xaml, "Content=\"Duyệt hư hỏng\"");
        StringAssert.Contains(xaml, "Content=\"Đảo lần nhận\"");
        StringAssert.Contains(xaml, "Content=\"{Binding CloseShortButtonText}\"");
        StringAssert.Contains(xaml, "Content=\"In phiếu\"");
        StringAssert.Contains(xaml, "Hàng thừa không tự cộng tồn.");
        StringAssert.Contains(xaml, "Phiếu xuất gốc vẫn được giữ nguyên.");

        StringAssert.Contains(code, "new PrintDialog()");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.Escape");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "TransferSearchBox.Focus()");
        Assert.IsFalse(code.Contains("private bool _loaded;", StringComparison.Ordinal));

        StringAssert.Contains(viewModel, "var transfersTask = _service.ListTransfersAsync()");
        StringAssert.Contains(viewModel, "var inTransitTask = _service.ListInTransitAsync()");
        StringAssert.Contains(viewModel, "var balancesTask = _inventoryService.ListBalancesAsync()");
        StringAssert.Contains(viewModel, "var locationsTask = _organizationService.ListLocationsAsync()");
        StringAssert.Contains(viewModel, "Một phần dữ liệu chuyển kho chưa tải được.");
        StringAssert.Contains(viewModel, "if (generation != _accessGeneration || !CanRead) return false;");
        StringAssert.Contains(viewModel, "_loaded = await LoadAsync(SelectedTransfer?.Id, preserveMessage: false)");
        StringAssert.Contains(viewModel, "CreateButtonText => _busyAction == \"create\"");
        StringAssert.Contains(viewModel, "CanCloseShortSelected => CanResolve && HasRemainingTransfer && IsNotBusy");

        StringAssert.Contains(shell, "\"inventory.transfer\" => \"Chuyển kho\"");
        StringAssert.Contains(shell, "Navigate(\"inventory-transfer\", 7)");
        StringAssert.Contains(shell, "await _transfer.EnsureLoadedAsync().ConfigureAwait(true)");
        StringAssert.Contains(shell, "CanCreateTransferTopbar");
        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryTransferSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryTransfer_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"TransferHost\"");
        Assert.IsFalse(main.Contains(
            "IsEnabled=\"False\"><TextBlock Text=\"Chuyển kho\"",
            StringComparison.Ordinal));
        StringAssert.Contains(mainCode, "TransferHost.Content = transferView");
        StringAssert.Contains(mainCode, "InventoryTransferRefresh_OnClick");
        StringAssert.Contains(mainCode, "InventoryTransferCreate_OnClick");

        StringAssert.Contains(access, "\"inventory-transfer\" => HasPermission(\"core.inventory-transfer.read\")");
        StringAssert.Contains(app, "AddSingleton<IInventoryTransferService, InventoryTransferService>()");
        StringAssert.Contains(app, "AddSingleton<TransferViewModel>()");
        StringAssert.Contains(app, "AddSingleton<TransferView>()");
    }

    [TestMethod]
    public void Source_DoesNotExposeDeveloperOnlyTransferLanguage()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "TransferView.xaml");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");

        StringAssert.Contains(xaml, "Text=\"Ghi sổ kho nguồn\"");
        Assert.IsFalse(xaml.Contains("Movement nguồn", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(xaml.Contains("inventoryMovementId", StringComparison.Ordinal));
        Assert.IsFalse(shell.Contains("Core ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(shell.Contains("NPP ", StringComparison.OrdinalIgnoreCase));
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
