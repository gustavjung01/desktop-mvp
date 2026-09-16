using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class ManualInboundParityTests
{
    [TestMethod]
    public void Presentation_UsesOfficeLabelsAndExactFormatting()
    {
        Assert.AreEqual("Nhập hàng thủ công", ManualInboundPresentation.InboundType("MANUAL_RECEIPT"));
        Assert.AreEqual("Khách trả ngoài chứng từ", ManualInboundPresentation.InboundType("OFF_DOCUMENT_CUSTOMER_RETURN"));
        Assert.AreEqual("Hàng thu hồi", ManualInboundPresentation.InboundType("RECOVERY"));
        Assert.AreEqual("Đã nhập", ManualInboundPresentation.HistoryStatus("POSTED"));
        Assert.AreEqual("Đã đảo", ManualInboundPresentation.HistoryStatus("REVERSED"));
        Assert.AreEqual("1,5", ManualInboundPresentation.Quantity("1.500000"));
    }

    [TestMethod]
    public void FileParser_UsesWebHeadersAndFiveHundredRowGate()
    {
        IReadOnlyList<IReadOnlyList<string>> sheet =
        [
            new[] { "SKU", "Số lượng", "Giá vốn", "Vị trí", "Mã lô", "Ngày sản xuất", "Hạn sử dụng", "Mã lô nhà cung cấp" },
            new[] { "SP-01", "2.5", "12000", "a-01", "lo-01", "2026-09-01", "2027-09-01", "NCC-01" }
        ];

        var rows = ManualInboundFile.ParseSheet(sheet);

        Assert.HasCount(1, rows);
        Assert.AreEqual("SP-01", rows[0].Sku);
        Assert.AreEqual("2.5", rows[0].SourceQuantity);
        Assert.AreEqual("12000", rows[0].UnitCost);
        Assert.AreEqual("A-01", rows[0].LocationCode);
        Assert.AreEqual("LO-01", rows[0].LotCode);
        Assert.AreEqual("SKU,Số lượng,Giá vốn", ManualInboundFile.TemplateCsv.TrimStart('﻿').Trim());
        StringAssert.Contains(
            ReadRepoFile("src", "CongTy.Desktop", "Inventory", "ManualInboundFile.cs"),
            "public const int MaxRows = 500;");
    }

    [TestMethod]
    public void Source_PreservesWebFlowPermissionsAndCanonicalIdempotency()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "ManualInboundService.cs");
        var contracts = ReadRepoFile("src", "CongTy.Contracts", "ManualInboundContracts.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "ManualInboundViewModel.cs");
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "ManualInboundView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "ManualInboundView.xaml.cs");
        var shell = ReadRepoFile("src", "CongTy.Desktop", "Shell", "ShellViewModel.cs");
        var main = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml");
        var mainCode = ReadRepoFile("src", "CongTy.Desktop", "Shell", "MainWindow.xaml.cs");
        var app = ReadRepoFile("src", "CongTy.Desktop", "App.xaml.cs");

        foreach (var endpoint in new[]
        {
            "/api/inventory/manual-inbounds/operator/warehouses",
            "/api/inventory/manual-inbounds/operator/locations?warehouseId=",
            "/api/inventory/manual-inbounds/operator/suppliers",
            "/api/inventory/manual-inbounds/operator/products?warehouseId=",
            "/api/inventory/manual-inbounds/operator/preview",
            "/api/inventory/manual-inbounds/operator/confirm",
            "/api/inventory/manual-inbounds/operator/history?",
            "/api/inventory/manual-inbounds/operator/history-detail?documentId=",
            "/api/inventory/manual-inbounds/{Uri.EscapeDataString"
        })
        {
            StringAssert.Contains(service, endpoint);
        }

        Assert.IsFalse(service.Contains("/operator/reverse", StringComparison.Ordinal));
        StringAssert.Contains(service, "PostIdempotentDataAsync");
        StringAssert.Contains(service, "idempotencyKeys.IsValid(value)");

        foreach (var permission in new[]
        {
            "core.inventory-manual-inbound.read",
            "core.inventory-manual-inbound.prepare",
            "core.inventory-manual-inbound.post",
            "core.inventory-manual-inbound.reverse"
        })
        {
            StringAssert.Contains(viewModel, permission);
        }

        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(\"manual-inbound-confirm\")");
        StringAssert.Contains(viewModel, "_idempotencyKeys.Create(\"manual-inbound-reverse\")");
        StringAssert.Contains(viewModel, "_pendingConfirmFingerprint");
        StringAssert.Contains(viewModel, "_pendingReverseFingerprint");

        var direct = view.IndexOf("Content=\"Nhập trực tiếp\"", StringComparison.Ordinal);
        var file = view.IndexOf("Content=\"Nhập từ file\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, direct);
        Assert.IsGreaterThan(direct, file);

        foreach (var text in new[]
        {
            "Text=\"THÔNG TIN CHỨNG TỪ\"",
            "Text=\"KHO NHẬP *\"",
            "Text=\"NHÀ CUNG CẤP\"",
            "Text=\"LOẠI NHẬP *\"",
            "Text=\"NGÀY CHỨNG TỪ *\"",
            "Text=\"HÀNG NHẬP\"",
            "Content=\"Kiểm tra dữ liệu\"",
            "Text=\"KẾT QUẢ KIỂM TRA\"",
            "Content=\"XÁC NHẬN NHẬP\"",
            "Text=\"LỊCH SỬ NHẬP KHO THỦ CÔNG\"",
            "Content=\"Xem biến động\"",
            "Content=\"Đảo chứng từ\"",
            "Text=\"BIẾN ĐỘNG TỒN THEO CHỨNG TỪ\""
        })
        {
            StringAssert.Contains(view, text);
        }

        foreach (var column in new[]
        {
            "Header=\"Tồn hiện tại\"",
            "Header=\"Tồn sau nhập\"",
            "Header=\"Vị trí\"",
            "Header=\"Lô\"",
            "Header=\"HSD\"",
            "Header=\"Giá vốn\"",
            "Header=\"Trạng thái\""
        })
        {
            StringAssert.Contains(view, column);
        }

        StringAssert.Contains(code, "Task.Delay(120");
        StringAssert.Contains(code, "Key.Down");
        StringAssert.Contains(code, "Key.Enter");
        StringAssert.Contains(view, "MinHeight=\"280\"");
        StringAssert.Contains(view, "MaxHeight=\"360\"");
        StringAssert.Contains(view, "PreviewMouseLeftButtonUp=\"ProductResultsList_OnPreviewMouseLeftButtonUp\"");
        StringAssert.Contains(code, "ItemsControl.ContainerFromElement(ProductResultsList");
        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "Filter = \"Excel hoặc CSV (*.xlsx;*.csv)");

        StringAssert.Contains(shell, "\"inventory.manual-inbound\" => \"Nhập kho thủ công\"");
        StringAssert.Contains(shell, "SelectedWorkspaceIndex = 10");
        StringAssert.Contains(main, "Tag=\"{Binding IsManualInboundSelected}\"");
        StringAssert.Contains(main, "Click=\"ManualInbound_OnClick\"");
        StringAssert.Contains(main, "x:Name=\"ManualInboundHost\"");
        Assert.IsFalse(
            main.Contains("IsEnabled=\"False\"><TextBlock Text=\"Nhập kho thủ công\"", StringComparison.Ordinal));

        StringAssert.Contains(mainCode, "ManualInboundHost.Content = manualInboundView");
        StringAssert.Contains(app, "AddSingleton<IManualInboundService, ManualInboundService>()");
        StringAssert.Contains(app, "AddSingleton<ManualInboundViewModel>()");
        StringAssert.Contains(app, "AddSingleton<ManualInboundView>()");

        StringAssert.Contains(contracts, "ManualInboundPreviewData");
        StringAssert.Contains(contracts, "ManualInboundHistoryMovementData");
    }

    [TestMethod]
    public void Ui_UsesOfficeLanguageAndTextOnlyStatuses()
    {
        var view = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "ManualInboundView.xaml");

        Assert.IsFalse(view.Contains("movementId", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("scopeVersion", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(view.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));

        var marker = "Text=\"{Binding Status}\"";
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
