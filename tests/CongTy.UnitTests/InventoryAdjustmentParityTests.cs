using System.Text;
using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryAdjustmentParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndExactQuantityFormatting()
    {
        Assert.AreEqual("Điều chỉnh thủ công", InventoryAdjustmentPresentation.Kind("MANUAL_ADJUSTMENT"));
        Assert.AreEqual("Chuyển cách ly", InventoryAdjustmentPresentation.Kind("QUARANTINE_TRANSFER"));
        Assert.AreEqual("Chuyển hư hỏng", InventoryAdjustmentPresentation.Kind("DAMAGED_TRANSFER"));
        Assert.AreEqual("Tiêu hủy", InventoryAdjustmentPresentation.Kind("SCRAP"));
        Assert.AreEqual("Lập phiếu", InventoryAdjustmentPresentation.Status("DRAFT"));
        Assert.AreEqual("Chờ duyệt", InventoryAdjustmentPresentation.Status("SUBMITTED"));
        Assert.AreEqual("Chờ cập nhật tồn", InventoryAdjustmentPresentation.Status("APPROVED"));
        Assert.AreEqual("Hoàn tất", InventoryAdjustmentPresentation.Status("POSTED"));
        Assert.AreEqual("+2,5", InventoryAdjustmentPresentation.SignedQuantity("2.500000000000"));
        Assert.AreEqual("-2,5", InventoryAdjustmentPresentation.SignedQuantity("-2.500000000000"));
    }

    [TestMethod]
    public void BulkFile_ParsesWebHeadersAndOptionalScopeColumns()
    {
        IReadOnlyList<IReadOnlyList<string>> sheet =
        [
            new[] { "SKU", "Tồn thực tế", "Vị trí", "Lô" },
            new[] { "SKU-01", "12.5", "a-01", "lot-01" },
            new[] { "SKU-02", "0", "", "" }
        ];

        var rows = InventoryAdjustmentBulkFile.ParseSheet(sheet);

        Assert.HasCount(2, rows);
        Assert.AreEqual(2, rows[0].LineNumber);
        Assert.AreEqual("SKU-01", rows[0].Sku);
        Assert.AreEqual("12.5", rows[0].ActualQuantity);
        Assert.AreEqual("A-01", rows[0].LocationCode);
        Assert.AreEqual("LOT-01", rows[0].LotCode);
        Assert.AreEqual("SKU,Tồn thực tế", InventoryAdjustmentBulkFile.TemplateCsv.TrimStart('\uFEFF').Trim());
        StringAssert.Contains(ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentBulkFile.cs"), "public const int MaxRows = 200;");
    }

    [TestMethod]
    public void Source_KeepsFourWebTabsLifecyclePermissionsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryAdjustmentService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "InventoryAdjustmentContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/adjustments?{string.Join",
            "/api/inventory/adjustments/reasons",
            "/api/inventory/adjustments/{Uri.EscapeDataString",
            "/api/inventory/adjustments",
            "ActionPath(adjustmentId, \"submit\")",
            "ActionPath(adjustmentId, \"approve\")",
            "ActionPath(adjustmentId, \"post\")",
            "ActionPath(adjustmentId, \"cancel\")",
            "ActionPath(adjustmentId, \"reverse\")",
            "/api/inventory/adjustments/bulk-preview",
            "/api/inventory/adjustments/bulk-confirm"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        StringAssert.Contains(service, "ListForExportAsync");
        StringAssert.Contains(service, "PageSize = 500");
        StringAssert.Contains(service, "MaxExportRows = 2000");
        StringAssert.Contains(service, "MaxExportScanRows = 5000");
        StringAssert.Contains(service, "offset += batch.Count");
        StringAssert.Contains(service, "Có hơn 2.000 phiếu phù hợp");
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(idempotencyKey)");
        StringAssert.Contains(viewModel, "ICanonicalIdempotencyKeyProvider");
        StringAssert.Contains(viewModel, "_intentKeys.TryGetValue(intent, out var existing)");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create($\"inventory-adjustment-{prefix}\")");
        StringAssert.Contains(viewModel, "_intentKeys.Clear()");

        foreach (var permission in new[]
        {
            "core.inventory-adjustment.read",
            "core.inventory-adjustment.create",
            "core.inventory-adjustment.submit",
            "core.inventory-adjustment.approve",
            "core.inventory-adjustment.post",
            "core.inventory-adjustment.cancel",
            "core.inventory-adjustment.reverse"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        var tabDocuments = view.IndexOf("Content=\"Phiếu điều chỉnh\"", StringComparison.Ordinal);
        var tabManual = view.IndexOf("Content=\"Điều chỉnh thủ công\"", StringComparison.Ordinal);
        var tabBulk = view.IndexOf("Content=\"Điều chỉnh hàng loạt\"", StringComparison.Ordinal);
        var tabExport = view.IndexOf("Content=\"Xuất dữ liệu\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, tabDocuments);
        Assert.IsGreaterThan(tabDocuments, tabManual);
        Assert.IsGreaterThan(tabManual, tabBulk);
        Assert.IsGreaterThan(tabBulk, tabExport);

        foreach (var action in new[]
        {
            "Content=\"Gửi duyệt\"",
            "Content=\"Duyệt\"",
            "Content=\"Cập nhật tồn kho\"",
            "Content=\"Hủy\"",
            "Content=\"Hoàn tác phiếu\"",
            "Content=\"Lập phiếu\""
        })
        {
            StringAssert.Contains(view, action);
        }

        foreach (var bulkStep in new[]
        {
            "Text=\"Tải tệp mẫu\"",
            "Text=\"Chọn tệp đã điền\"",
            "Text=\"Kiểm tra dữ liệu\"",
            "Text=\"Lập phiếu\"",
            "Header=\"Tồn hệ thống\"",
            "Header=\"Tồn thực tế\"",
            "Header=\"Chênh lệch\"",
            "Header=\"Kết quả\""
        })
        {
            StringAssert.Contains(view, bulkStep);
        }

        foreach (var exportText in new[]
        {
            "Phiếu điều chỉnh tồn",
            "Dữ liệu được lấy từ server theo phạm vi kho được cấp",
            "Thông tin muốn xuất",
            "Chọn tất cả",
            "Bỏ chọn",
            "Cột mặc định",
            "ExportStatusFilter",
            "ExportKindFilter",
            "ExportFormatOptions",
            "ExportColumns",
            "ExportData_OnClick"
        })
        {
            StringAssert.Contains(view, exportText);
        }

        StringAssert.Contains(viewModel, "ListForExportAsync");
        StringAssert.Contains(viewModel, "InventoryAdjustmentExportFile.Create");
        StringAssert.Contains(viewModel, "IsExportTab");
        StringAssert.Contains(viewModel, "CanExportData");
        StringAssert.Contains(viewModel, "Vui lòng chọn ít nhất một cột để xuất.");

        StringAssert.Contains(code, "Filter = \"Excel hoặc CSV (*.xlsx;*.csv)");
        StringAssert.Contains(code, "Lưu dữ liệu Điều chỉnh tồn");
        StringAssert.Contains(code, "File.WriteAllBytesAsync");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.D1");
        StringAssert.Contains(code, "Key.D2");
        StringAssert.Contains(code, "Key.D3");
        StringAssert.Contains(code, "Key.D4");

        StringAssert.Contains(shell, "\"inventory.adjustments\" => \"Điều chỉnh tồn\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 9");
        StringAssert.Contains(shell, "_adjustment.SetTab(\"documents\")");
        StringAssert.Contains(main, "Tag=\"{Binding IsInventoryAdjustmentSelected}\"");
        StringAssert.Contains(main, "Click=\"InventoryAdjustment_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"AdjustmentHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Điều chỉnh và xử lý tồn\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "AdjustmentHost.Content = adjustmentView");
        StringAssert.Contains(mainCode, "InventoryAdjustmentRefresh_OnClick");
        StringAssert.Contains(app, "AddSingleton<IInventoryAdjustmentService, InventoryAdjustmentService>()");
        StringAssert.Contains(app, "AddSingleton<InventoryAdjustmentViewModel>()");
        StringAssert.Contains(app, "AddSingleton<InventoryAdjustmentView>()");

        StringAssert.Contains(contracts, "BulkInventoryAdjustmentPreviewData");
        StringAssert.Contains(contracts, "BulkInventoryAdjustmentConfirmRequest");
    }

    [TestMethod]
    public void Export_UsesWebColumnsDefaultsCsvGuardAndXlsx()
    {
        var definitions = InventoryAdjustmentExportFile.Columns;
        Assert.HasCount(15, definitions);
        CollectionAssert.AreEqual(
            new[]
            {
                "adjustmentNumber", "createdAt", "warehouseCode", "warehouseName", "documentKind",
                "adjustmentDirection", "status", "reasonLabel", "reasonNote", "lineCount",
                "submittedAt", "approvedAt", "postedAt", "cancelledAt", "reversedAt"
            },
            definitions.Select(item => item.Key).ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                "adjustmentNumber", "createdAt", "warehouseCode", "warehouseName", "documentKind",
                "adjustmentDirection", "status", "reasonLabel", "lineCount", "postedAt"
            },
            definitions.Where(item => item.DefaultSelected).Select(item => item.Key).ToArray());

        var document = new InventoryAdjustmentData
        {
            AdjustmentNumber = "=ADJ-001",
            WarehouseCode = "K01",
            WarehouseName = "Kho 1",
            DocumentKind = "MANUAL_ADJUSTMENT",
            AdjustmentDirection = "IN",
            Status = "POSTED",
            ReasonCode = "COUNT",
            ReasonLabel = "Kiểm đếm",
            ReasonNote = "Điều chỉnh sau kiểm đếm",
            LineCount = 2,
            CreatedAt = "2026-09-18T18:30:00.000Z",
            PostedAt = "2026-09-18T19:00:00.000Z"
        };
        var columns = definitions.Where(item => item.DefaultSelected).Select(item => item.Key).ToArray();
        var generatedAt = new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero);

        var csv = InventoryAdjustmentExportFile.Create([document], columns, "csv", generatedAt);
        Assert.AreEqual("Dieu-chinh-ton-2026-09-19.csv", csv.FileName);
        Assert.AreEqual("text/csv; charset=utf-8", csv.ContentType);
        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, csv.Content.Take(3).ToArray());
        var csvText = Encoding.UTF8.GetString(csv.Content);
        StringAssert.Contains(csvText, "\"Số phiếu\"");
        StringAssert.Contains(csvText, "\"'=ADJ-001\"");
        StringAssert.Contains(csvText, "\"19/09/2026 01:30\"");
        StringAssert.Contains(csvText, "\"Tăng tồn\"");
        StringAssert.Contains(csvText, "\"Hoàn tất\"");

        var xlsx = InventoryAdjustmentExportFile.Create([document], columns, "xlsx", generatedAt);
        Assert.AreEqual("Dieu-chinh-ton-2026-09-19.xlsx", xlsx.FileName);
        Assert.AreEqual(InventoryAdjustmentExportFile.XlsxContentType, xlsx.ContentType);
        Assert.IsGreaterThan(100, xlsx.Content.Length);
        Assert.AreEqual((byte)'P', xlsx.Content[0]);
        Assert.AreEqual((byte)'K', xlsx.Content[1]);
    }

    [TestMethod]
    public void Ui_UsesOfficeLanguageAndTextOnlyStatuses()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentView.xaml");

        Assert.IsFalse(view.Contains(">Ghi sổ<", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Bút toán kho", StringComparison.Ordinal));
        Assert.IsFalse(view.Contains("Phiên bản", StringComparison.Ordinal));

        var marker = "Text=\"{Binding DetailStatus}\"";
        var statusIndex = view.IndexOf(marker, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, statusIndex);
        var textStart = view.LastIndexOf("<TextBlock", statusIndex, StringComparison.Ordinal);
        var textEnd = view.IndexOf("/>", statusIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, textStart);
        Assert.IsGreaterThan(textStart, textEnd);

        var element = view[textStart..(textEnd + 2)];
        Assert.IsFalse(element.Contains("<Border", StringComparison.Ordinal));
        StringAssert.Contains(element, "Foreground=");
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
