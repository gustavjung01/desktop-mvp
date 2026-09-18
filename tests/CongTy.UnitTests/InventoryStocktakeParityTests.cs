using System.Text.Json;
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
        Assert.AreEqual("Khớp", StocktakePresentation.CountStatus("matched"));
        Assert.AreEqual("Lệch", StocktakePresentation.CountStatus("mismatch"));
        Assert.AreEqual("1", StocktakePresentation.Quantity("1.000000000000"));
        Assert.AreEqual("1,5", StocktakePresentation.Quantity("1.500000000000"));
        Assert.AreEqual("+2,5", StocktakePresentation.SignedDifference("12.5", "10"));
        Assert.AreEqual("-2,5", StocktakePresentation.SignedDifference("10", "12.5"));
    }

    [TestMethod]
    public void CreateContract_UsesCanonicalIntentScopeModes_NotLegacyExactScopes()
    {
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryStocktakeContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");

        Assert.IsFalse(contracts.Contains("InventoryStocktakeScopeRequest", StringComparison.Ordinal));
        StringAssert.Contains(contracts, "[property: JsonPropertyName(\"scopeMode\")] string ScopeMode");
        StringAssert.Contains(contracts, "[property: JsonPropertyName(\"lotSelections\")]");
        StringAssert.Contains(contracts, "[property: JsonPropertyName(\"locationIds\")]");

        Assert.IsFalse(viewModel.Contains("scopes.Count > 500", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.Contains("Phạm vi này có hơn 500", StringComparison.Ordinal));
        StringAssert.Contains(viewModel, "ScopePickerResultLimit = 60");
        StringAssert.Contains(viewModel, "ScopeMode == \"lot\"");
        StringAssert.Contains(viewModel, "ScopeMode == \"location\"");
        StringAssert.Contains(viewModel, "new InventoryStocktakeLotSelectionRequest");
        StringAssert.Contains(viewModel, "locationIds");

        var allJson = JsonSerializer.Serialize(new InventoryStocktakeCreateRequest(
            "00000000-0000-0000-0000-000000000001",
            null,
            "all"));
        StringAssert.Contains(allJson, "\"scopeMode\":\"all\"");

        var lotJson = JsonSerializer.Serialize(new InventoryStocktakeCreateRequest(
            "00000000-0000-0000-0000-000000000001",
            null,
            "lot",
            [new InventoryStocktakeLotSelectionRequest(
                "00000000-0000-0000-0000-000000000002",
                "00000000-0000-0000-0000-000000000003")]));
        StringAssert.Contains(lotJson, "\"lotSelections\"");
        StringAssert.Contains(lotJson, "\"baseVariantId\"");

        var locationJson = JsonSerializer.Serialize(new InventoryStocktakeCreateRequest(
            "00000000-0000-0000-0000-000000000001",
            null,
            "location",
            null,
            ["00000000-0000-0000-0000-000000000004"]));
        StringAssert.Contains(locationJson, "\"locationIds\"");
    }

    [TestMethod]
    public void Source_PreservesPermissionsMutationsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryStocktakeService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryStocktakeContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/stocktakes?limit=500&offset=0",
            "/api/inventory/stocktakes/{Uri.EscapeDataString",
            "ActionPath(stocktakeId, \"count\")",
            "ActionPath(stocktakeId, \"annotate\")",
            "ActionPath(stocktakeId, \"copy\")",
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
        StringAssert.Contains(viewModel, "KeyFor(\"create\"");
        StringAssert.Contains(viewModel, "KeyFor(\"count\"");
        StringAssert.Contains(viewModel, "KeyFor(\"annotate\"");
        StringAssert.Contains(viewModel, "KeyFor(\"copy\"");
        StringAssert.Contains(contracts, "[property: JsonPropertyName(\"expectedRevision\")]");

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

        foreach (var contract in new[]
        {
            "InventoryStocktakeRoundData",
            "InventoryStocktakeLineData",
            "[JsonPropertyName(\"countStatus\")]",
            "[JsonPropertyName(\"reason\")]",
            "[JsonPropertyName(\"note\")]",
            "[JsonPropertyName(\"currentCountedAt\")]",
            "[JsonPropertyName(\"currentCountedBy\")]",
            "InventoryStocktakeAnnotateRequest",
            "InventoryStocktakeCopyRequest"
        })
        {
            StringAssert.Contains(contracts, contract);
        }

        StringAssert.Contains(viewModel, "line.Reason.Trim().Length > 500");
        StringAssert.Contains(viewModel, "line.Note.Trim().Length > 2000");
        StringAssert.Contains(viewModel, "SelectedStocktake?.Status is \"submitted\" or \"approved\"");
        StringAssert.Contains(viewModel, "WAREHOUSE_LOCATION_MODE_REQUIRED");
        StringAssert.Contains(viewModel, "Desktop không tự chọn thay");
    }

    [TestMethod]
    public void BlindCount_TableDoesNotExposeSystemQuantityDifferenceOrReviewStatus()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");
        var blindStart = xaml.IndexOf("Visibility=\"{Binding IsBlindCount", StringComparison.Ordinal);
        var reviewStart = xaml.IndexOf("Visibility=\"{Binding IsReviewTable", blindStart, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, blindStart);
        Assert.IsGreaterThan(blindStart, reviewStart);

        var blind = xaml[blindStart..reviewStart];
        Assert.IsFalse(blind.Contains("Tồn hệ thống", StringComparison.Ordinal));
        Assert.IsFalse(blind.Contains("Chênh lệch", StringComparison.Ordinal));
        Assert.IsFalse(blind.Contains("Header=\"Trạng thái\"", StringComparison.Ordinal));
        StringAssert.Contains(blind, "Header=\"Số thực đếm\"");
        StringAssert.Contains(blind, "Header=\"Lý do\"");
        StringAssert.Contains(blind, "Header=\"Ghi chú\"");
    }

    [TestMethod]
    public void Lines_HaveWebFiltersPagingAnnotationAndRevealRules()
    {
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");

        StringAssert.Contains(viewModel, "public const int LinePageSize = 100");
        StringAssert.Contains(viewModel, "AllLineFilterText");
        StringAssert.Contains(viewModel, "UncountedLineFilterText");
        StringAssert.Contains(viewModel, "MatchedLineFilterText");
        StringAssert.Contains(viewModel, "MismatchLineFilterText");
        StringAssert.Contains(viewModel, "CanUseRevealFilters => IsReviewTable");
        StringAssert.Contains(xaml, "Content=\"{Binding AllLineFilterText}\"");
        StringAssert.Contains(xaml, "Content=\"{Binding UncountedLineFilterText}\"");
        StringAssert.Contains(xaml, "IsEnabled=\"{Binding CanUseRevealFilters}\"");
        StringAssert.Contains(xaml, "Content=\"Trước\"");
        StringAssert.Contains(xaml, "Content=\"Sau\"");
        StringAssert.Contains(xaml, "Text=\"{Binding LinePageText}\"");
        StringAssert.Contains(xaml, "Content=\"Lưu Lý do &amp; Ghi chú\"");
        StringAssert.Contains(xaml, "MaxLength=\"500\"");
        StringAssert.Contains(xaml, "MaxLength=\"2000\"");
        StringAssert.Contains(xaml, "Header=\"Tồn hệ thống\"");
        StringAssert.Contains(xaml, "Header=\"Chênh lệch\"");
        StringAssert.Contains(xaml, "Header=\"Trạng thái\"");
    }

    [TestMethod]
    public void CreatePanel_UsesThreeOfficeScopeChoicesAndSearchablePicker()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");

        StringAssert.Contains(xaml, "Content=\"Toàn bộ sản phẩm trong kho\"");
        StringAssert.Contains(xaml, "Content=\"Theo lô\"");
        StringAssert.Contains(xaml, "Content=\"Theo vị trí\"");
        StringAssert.Contains(xaml, "Text=\"{Binding ScopeSearch, UpdateSourceTrigger=PropertyChanged}\"");
        StringAssert.Contains(xaml, "Content=\"Chọn tất cả kết quả\"");
        StringAssert.Contains(xaml, "Content=\"Bỏ chọn kết quả\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding SelectedScopeGroups}\"");
        StringAssert.Contains(xaml, "Visibility=\"{Binding ShowScopePicker");
        Assert.IsFalse(xaml.Contains("ItemsSource=\"{Binding ScopeModeOptions}\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FileFlow_UsesCountTemplateWithoutSystemQuantityAndReviewExportsWithCsvGuard()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "Phiếu kiểm kê", "SKU", "Tên sản phẩm", "ĐVT", "Mã lô", "Mã vị trí",
                "Số đếm thực tế", "Lý do", "Ghi chú"
            },
            StocktakeFileCodec.CountHeaders);

        CollectionAssert.AreEqual(
            new[]
            {
                "SKU", "Tên sản phẩm", "ĐVT", "Mã lô", "Mã vị trí",
                "Tồn hệ thống", "Thực đếm", "Chênh lệch", "Lý do", "Ghi chú"
            },
            StocktakeFileCodec.ResultHeaders);

        Assert.AreEqual("\"'=SUM(A1)\"", StocktakeFileCodec.CsvCell("=SUM(A1)"));
        Assert.AreEqual("\"'+1\"", StocktakeFileCodec.CsvCell("+1"));
        Assert.AreEqual("\"'@cmd\"", StocktakeFileCodec.CsvCell("@cmd"));
        Assert.AreEqual("\"'-cmd\"", StocktakeFileCodec.CsvCell("-cmd"));
        Assert.AreEqual("\"-12.5\"", StocktakeFileCodec.CsvCell("-12.5"));

        var codec = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeFileCodec.cs");
        var exportStart = codec.IndexOf("public static void ExportCountXlsx", StringComparison.Ordinal);
        var importStart = codec.IndexOf("public static int ImportCountFile", exportStart, StringComparison.Ordinal);
        var countExport = codec[exportStart..importStart];

        Assert.IsFalse(countExport.Contains("ExpectedBaseQuantity", StringComparison.Ordinal));
        Assert.IsFalse(countExport.Contains("FinalDelta", StringComparison.Ordinal));
        StringAssert.Contains(codec, "file thuộc phiếu");
        StringAssert.Contains(codec, "có nhiều lô trong phiếu");
        StringAssert.Contains(codec, "có nhiều vị trí trong phiếu");
        StringAssert.Contains(codec, "bị trùng phạm vi trong file");
        StringAssert.Contains(codec, "if (reason.Length > 500)");
        StringAssert.Contains(codec, "if (note.Length > 2000)");
        StringAssert.Contains(codec, "Chỉ hỗ trợ file .xlsx hoặc .csv.");
    }

    [TestMethod]
    public void ToolbarListNavigationAndPrint_AreWired()
    {
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "StocktakeViewModel.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        foreach (var action in new[]
        {
            "Content=\"Xuất file phiếu\"",
            "Content=\"Nhập file\"",
            "Content=\"Sao chép phiếu\"",
            "Content=\"Kết quả Excel\"",
            "Content=\"Kết quả CSV\"",
            "Content=\"In\""
        })
        {
            StringAssert.Contains(xaml, action);
        }

        StringAssert.Contains(xaml, "Visibility=\"{Binding CanImportCountFile");
        StringAssert.Contains(xaml, "Visibility=\"{Binding CanCopySelected");
        StringAssert.Contains(xaml, "Visibility=\"{Binding CanExportResults");
        StringAssert.Contains(xaml, "Visibility=\"{Binding CanPrint");
        StringAssert.Contains(viewModel, "SelectedStocktake?.Status != \"draft\"");
        StringAssert.Contains(viewModel, "public bool CanCopySelected => HasDetail && CanCreate");
        StringAssert.Contains(viewModel, "public bool CanAnnotateSelected =>");
        StringAssert.Contains(viewModel, "CanCount && IsNotBusy");
        StringAssert.Contains(viewModel, "public string Counted =>");
        StringAssert.Contains(viewModel, "$\"Kiểm:");

        StringAssert.Contains(code, "OpenFileDialog");
        StringAssert.Contains(code, "*.xlsx;*.csv");
        StringAssert.Contains(code, "StocktakeFileCodec.ImportCountFile");
        StringAssert.Contains(code, "StocktakeFileCodec.ExportCountXlsx");
        StringAssert.Contains(code, "StocktakeFileCodec.ExportResultXlsx");
        StringAssert.Contains(code, "StocktakeFileCodec.ExportResultCsv");
        StringAssert.Contains(code, "DocumentPrintTemplateRuntime.LoadForPrintAsync");
        StringAssert.Contains(code, "\"STOCKTAKE\"");
        StringAssert.Contains(code, "StocktakePrintPreview.Print(stocktake, _viewModel.Lines.ToArray(), template)");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.Escape");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");

        StringAssert.Contains(shell, "\"inventory.stocktake\" => \"Kiểm kê kho\"");
        StringAssert.Contains(shell, "_stocktake.CanRead");
        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryStocktakeSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryStocktake_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"StocktakeHost\"");
        StringAssert.Contains(mainCode, "StocktakeHost.Content = stocktakeView");
        StringAssert.Contains(app, "AddSingleton<IInventoryStocktakeService, InventoryStocktakeService>()");
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
