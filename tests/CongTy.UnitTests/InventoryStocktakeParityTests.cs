using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryStocktakeParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeStocktakeLabelsAndExactQuantityDisplay()
    {
        Assert.AreEqual("Đang đếm", StocktakePresentation.Status("draft"));
        Assert.AreEqual("Chờ duyệt", StocktakePresentation.Status("submitted"));
        Assert.AreEqual("Chờ cập nhật tồn", StocktakePresentation.Status("approved"));
        Assert.AreEqual("Hoàn tất", StocktakePresentation.Status("posted"));
        Assert.AreEqual("1", StocktakePresentation.Quantity("1.000000000000"));
        Assert.AreEqual("1,5", StocktakePresentation.Quantity("1.500000000000"));
        Assert.AreEqual("+2,5", StocktakePresentation.SignedDifference("12.5", "10"));
        Assert.AreEqual("-2,5", StocktakePresentation.SignedDifference("10", "12.5"));

        var balance = new InventoryBalanceData
        {
            LocationId = "location-1",
            BaseVariantId = "variant-1",
            LotId = "lot-1"
        };
        Assert.AreEqual("location-1:variant-1:lot-1", StocktakePresentation.ScopeKey(balance));
    }

    [TestMethod]
    public void Source_PreservesWebStocktakeFlowPermissionsBlindCountAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryStocktakeService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryStocktakeContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/stocktakes?limit=500&offset=0",
            "/api/inventory/stocktakes/{Uri.EscapeDataString",
            "ActionPath(stocktakeId, \"count\")",
            "ActionPath(stocktakeId, \"submit\")",
            "ActionPath(stocktakeId, \"recount\")",
            "ActionPath(stocktakeId, \"approve\")",
            "ActionPath(stocktakeId, \"post\")",
            "ActionPath(stocktakeId, \"cancel\")",
            "ActionPath(stocktakeId, \"reverse\")"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create($\"stocktake-{prefix}\")");
        StringAssert.Contains(viewModel, "_intentKeys.Clear()");
        StringAssert.Contains(viewModel, "KeyFor(\"create\"");
        StringAssert.Contains(viewModel, "KeyFor(\"count\"");
        StringAssert.Contains(viewModel, "KeyFor(action");

        foreach (var permission in new[]
        {
            "core.stocktake.read",
            "core.stocktake.create",
            "core.stocktake.count",
            "core.stocktake.submit",
            "core.stocktake.approve",
            "core.stocktake.post",
            "core.stocktake.cancel",
            "core.stocktake.reverse"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        StringAssert.Contains(contracts, "InventoryStocktakeRoundData");
        StringAssert.Contains(contracts, "InventoryStocktakeLineData");
        StringAssert.Contains(contracts, "InventoryStocktakeCountRequest");
        StringAssert.Contains(viewModel, "Số hệ thống được ẩn trong lúc đếm.");
        StringAssert.Contains(viewModel, "IsBlindCount => SelectedStocktake?.Status is \"draft\" or \"recount_required\"");

        foreach (var expected in new[]
        {
            "Text=\"TẠO ĐỢT KIỂM KÊ\"",
            "Text=\"KHO KIỂM KÊ\"",
            "Text=\"CÁCH CHỌN PHẠM VI\"",
            "Text=\"DANH SÁCH KIỂM KÊ\"",
            "Content=\"Hoàn tất đếm thực tế\"",
            "Content=\"Gửi duyệt\"",
            "Content=\"Yêu cầu đếm lại\"",
            "Content=\"Duyệt kết quả\"",
            "Content=\"Cập nhật tồn kho\"",
            "Content=\"Hủy kiểm kê\"",
            "Content=\"Hoàn tác cập nhật tồn\"",
            "Text=\"LỊCH SỬ CÁC LẦN ĐẾM\"",
            "Header=\"Tồn hệ thống\"",
            "Header=\"Thực đếm\"",
            "Header=\"Chênh lệch\""
        })
        {
            StringAssert.Contains(xaml, expected);
        }

        StringAssert.Contains(code, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
        StringAssert.Contains(code, "\"STOCKTAKE\"");
        StringAssert.Contains(code, "StocktakePrintPreview.Print(stocktake, _viewModel.Lines.ToArray(), template)");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.Escape");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "StocktakeSearchBox.Focus()");

        StringAssert.Contains(shell, "\"inventory.stocktake\" => \"Kiểm kê kho\"");
        StringAssert.Contains(shell, "_stocktake.CanRead");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 8");
        StringAssert.Contains(shell, "await _stocktake.EnsureLoadedAsync().ConfigureAwait(true)");
        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryStocktakeSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryStocktake_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"StocktakeHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Kiểm kê kho\"", StringComparison.Ordinal),
            "Kiểm kê kho phải là navigation thật, không còn placeholder bị vô hiệu hóa.");

        StringAssert.Contains(mainCode, "StocktakeHost.Content = stocktakeView");
        StringAssert.Contains(mainCode, "InventoryStocktakeRefresh_OnClick");
        StringAssert.Contains(mainCode, "InventoryStocktakeCreate_OnClick");

        StringAssert.Contains(app, "AddSingleton<IInventoryStocktakeService, InventoryStocktakeService>()");
        StringAssert.Contains(app, "AddSingleton<StocktakeViewModel>()");
        StringAssert.Contains(app, "AddSingleton<StocktakeView>()");
    }

    [TestMethod]
    public void BlindCount_TableDoesNotExposeSystemQuantityOrDifference()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");
        var blindStart = xaml.IndexOf("Visibility=\"{Binding IsBlindCount", StringComparison.Ordinal);
        var reviewStart = xaml.IndexOf("Visibility=\"{Binding IsReviewTable", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, blindStart);
        Assert.IsGreaterThan(blindStart, reviewStart);

        var blind = xaml[blindStart..reviewStart];
        Assert.IsFalse(blind.Contains("Tồn hệ thống", StringComparison.Ordinal));
        Assert.IsFalse(blind.Contains("Chênh lệch", StringComparison.Ordinal));
        StringAssert.Contains(blind, "Header=\"Thực đếm\"");
    }

    [TestMethod]
    public void StocktakeStatus_IsTextOnly_NotDecorativePill()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");
        var marker = "Text=\"{Binding DetailStatus}\"";
        var statusIndex = xaml.IndexOf(marker, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, statusIndex);
        var textStart = xaml.LastIndexOf("<TextBlock", statusIndex, StringComparison.Ordinal);
        var textEnd = xaml.IndexOf("/>", statusIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, textStart);
        Assert.IsGreaterThan(textStart, textEnd);

        var statusElement = xaml[textStart..(textEnd + 2)];
        Assert.IsFalse(statusElement.Contains("<Border", StringComparison.Ordinal));
        StringAssert.Contains(statusElement, "Foreground=");
        StringAssert.Contains(statusElement, "VerticalAlignment=\"Center\"");
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
