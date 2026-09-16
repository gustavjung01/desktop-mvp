using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryCostingParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLanguageAndWebNumberRules()
    {
        Assert.AreEqual("1.23", InventoryCostingPresentation.DecimalText("1.230000"));
        Assert.AreEqual("1.235", InventoryCostingPresentation.DecimalText("1.235000"));
        Assert.AreEqual("1.235 ₫", InventoryCostingPresentation.MoneyVnd("1234.6"));
        Assert.AreEqual("-1.235 ₫", InventoryCostingPresentation.MoneyVnd("-1234.6"));
        Assert.AreEqual("Chưa xác định", InventoryCostingPresentation.MoneyVnd(null));
        Assert.AreEqual("Đã tính giá", InventoryCostingPresentation.Status("COSTED"));
        Assert.AreEqual("Thiếu nguồn giá", InventoryCostingPresentation.Status("ANOMALY"));
        Assert.AreEqual("Lệch số lượng", InventoryCostingPresentation.Status("QUANTITY_MISMATCH"));
        Assert.AreEqual("Chi phí mua hàng bổ sung", InventoryCostingPresentation.AdjustmentType("LANDED_COST"));
        Assert.AreEqual("Giá mua sau chiết khấu", InventoryCostingPresentation.CostSource("PURCHASE_ORDER_NET"));
    }

    [TestMethod]
    public void Source_UsesCanonicalBackendPermissionsAndIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryCostingService.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryCostingViewModel.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/costing/balances",
            "/api/inventory/costing/facts?limit=100",
            "/api/inventory/costing/anomalies?limit=100",
            "/api/inventory/costing/reconciliation?limit=500",
            "/api/inventory/costing/periods",
            "/api/inventory/costing/adjustments",
            "/api/inventory/costing/discrepancies",
            "/api/inventory/costing/run",
            "/api/inventory/costing/rebuild",
            "/api/inventory/costing/periods/{action}"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        foreach (var permission in new[]
        {
            "core.inventory-cost.read",
            "core.inventory-cost.reconcile",
            "core.inventory-cost.rebuild"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        StringAssert.Contains(service, "idempotencyKeys.IsValid(value)");
        StringAssert.Contains(viewModel, "_pendingRebuildKey ??= _idempotencyKeys.Create(\"inventory-costing-rebuild\")");
        StringAssert.Contains(viewModel, "_pendingPeriodFingerprint");
        StringAssert.Contains(viewModel, "_pendingPeriodKey");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create($\"inventory-costing-period-{action}\")");

        Assert.IsFalse(
            service.Contains("POST /api/inventory/costing/adjustments", StringComparison.Ordinal),
            "Desktop không được tự thêm form mutation Điều chỉnh giá khi Web hiện hành chưa có.");
    }

    [TestMethod]
    public void Ui_PreservesWebSectionAndTabOrder()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryCostingView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryCostingView.xaml.cs");

        var labels = new[]
        {
            "Content=\"Giá trị tồn\"",
            "Content=\"Kỳ giá vốn\"",
            "Content=\"Đối soát\"",
            "Content=\"Chờ xử lý\"",
            "Content=\"Điều chỉnh giá\"",
            "Content=\"Bất thường\"",
            "Content=\"Dữ liệu giá vốn\""
        };

        var previous = -1;
        foreach (var label in labels)
        {
            var index = view.IndexOf(label, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, index, $"Sai thứ tự tab: {label}");
            previous = index;
        }

        foreach (var header in new[]
        {
            "Header=\"Kho\"",
            "Header=\"SKU\"",
            "Header=\"Số lượng\"",
            "Header=\"Giá trị tồn\"",
            "Header=\"Giá bình quân\"",
            "Header=\"Trạng thái\"",
            "Header=\"Sổ kho\"",
            "Header=\"Chênh lệch\"",
            "Header=\"Kết quả\"",
            "Header=\"Ngày ghi nhận\"",
            "Header=\"Đơn giá\"",
            "Header=\"Nguồn\""
        })
        {
            StringAssert.Contains(view, header);
        }

        StringAssert.Contains(view, "Text=\"NHÓM GIÁ VỐN\"");
        StringAssert.Contains(view, "Text=\"THIẾU NGUỒN GIÁ\"");
        StringAssert.Contains(view, "Text=\"BÌNH QUÂN GIA QUYỀN DI ĐỘNG\"");
        Assert.IsFalse(view.Contains("Cost pool", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("MWA_V1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Text=\"Anomaly\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Content=\"Anomaly\"", StringComparison.OrdinalIgnoreCase));

        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.D1");
        StringAssert.Contains(code, "Key.D7");
    }

    [TestMethod]
    public void Shell_EnablesRealCostingWorkspaceAndTopbarAction()
    {
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        StringAssert.Contains(shell, "\"inventory.costing\" => \"Giá vốn tồn kho\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 12");
        StringAssert.Contains(shell, "CanRunCostingRebuild");

        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryCostingSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryCosting_OnClick\"");
        StringAssert.Contains(main, "Content=\"{Binding CostingRebuildActionText}\"");
        StringAssert.Contains(shell, "\"Dựng lại giá vốn\"");
        StringAssert.Contains(main, "x:Name=\"InventoryCostingHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Giá vốn tồn kho\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "InventoryCostingHost.Content = inventoryCostingView");
        StringAssert.Contains(mainCode, "await _inventoryCostingView.RebuildAsync()");
        StringAssert.Contains(app, "AddSingleton<IInventoryCostingService, InventoryCostingService>()");
        StringAssert.Contains(app, "AddSingleton<InventoryCostingViewModel>()");
        StringAssert.Contains(app, "AddSingleton<InventoryCostingView>()");
    }

    [TestMethod]
    public void Statuses_AreTextOnlyAndNoTechnicalIdsLeakToUi()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryCostingView.xaml");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryCostingViewModel.cs");

        Assert.IsFalse(view.Contains("Badge", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("movementId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("rebuildRunId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(viewModel.Contains(".Id[..", StringComparison.Ordinal));
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
