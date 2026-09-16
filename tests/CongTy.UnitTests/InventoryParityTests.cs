using CongTy.Contracts;
using CongTy.Desktop.Inventory;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InventoryParityTests
{
    [TestMethod]
    public void Presentation_FormatsCanonicalInventoryForOfficeUse()
    {
        var balance = new InventoryBalanceData
        {
            BaseSku = "SP-001",
            ProductName = "Nước mẫu",
            BaseUnitName = "Chai",
            OnHandQuantity = "139.500000000000",
            PackageUnitName = "Thùng",
            PackageConversionToBase = "24"
        };

        Assert.AreEqual("139,5 Chai", InventoryPresentation.QuantityWithUnit(balance.OnHandQuantity, balance));
        Assert.AreEqual("5 Thùng + 19,5 Chai", InventoryPresentation.PackageBreakdown(balance));
        Assert.AreEqual("5 Thùng + 19,5 Chai", InventoryPresentation.PackageBreakdown("139.5", balance));
        StringAssert.Contains(InventoryPresentation.ProductSkuWithUnit(balance, "SP-001"), "ĐVT: Chai · 1 Thùng = 24 Chai");
        Assert.AreEqual("15/09/2026 (12 ngày)", InventoryPresentation.DateWithAge("2026-09-15", "12"));
        Assert.AreEqual("Giao thủ công", InventoryPresentation.HoldFlow("DELIVERY", "MANUAL"));
        Assert.AreEqual("Khách nhận tại kho", InventoryPresentation.HoldFlow("PICKUP", null));
        Assert.AreEqual("Xuất kho giao khách", InventoryPresentation.Movement("SALES_DELIVERY_ISSUE"));
        Assert.AreEqual("Chuyển kho đi", InventoryPresentation.ReportMovement("INVENTORY_TRANSFER_OUT"));
        Assert.AreEqual("Soạn hàng", InventoryPresentation.ReportMovement("FULFILLMENT_PICK"));
        Assert.AreEqual("Tồn đầu kỳ", InventoryPresentation.ReportMovement("OPENING_BALANCE"));
        Assert.AreEqual("Đã tính giá vốn", InventoryPresentation.CostingStatus("COSTED"));
    }

    [TestMethod]
    public void Source_PreservesInventoryReportingWebParityAndKeyboard()
    {
        var service = ReadRepoFile("src", "CongTy.ApiClient", "InventoryService.cs");
        var xaml = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml");
        var code = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml.cs");
        var viewModel = ReadRepoFile("src", "CongTy.Desktop", "Inventory", "InventoryViewModel.cs");

        StringAssert.Contains(service, "/api/inventory/balances/history");
        StringAssert.Contains(service, "scope=warehouse");
        StringAssert.Contains(service, "/api/inventory/lots");
        StringAssert.Contains(service, "/api/reporting/inventory");
        StringAssert.Contains(service, "/api/inventory/holds");

        var expected = new[] { "Tổng quan", "Tồn hiện tại", "Luân chuyển", "Chậm luân chuyển", "Lô &amp; hạn dùng", "Cần kiểm tra" };
        var cursor = -1;
        foreach (var label in expected)
        {
            var next = xaml.IndexOf($"Header=\"{label}\"", cursor + 1, StringComparison.Ordinal);
            Assert.IsGreaterThan(cursor, next, $"Thiếu hoặc sai thứ tự tab: {label}");
            cursor = next;
        }

        StringAssert.Contains(xaml, "x:Name=\"ReportWarehouseBox\"");
        StringAssert.Contains(xaml, "DisplayMemberPath=\"Label\"");
        StringAssert.Contains(xaml, "SelectedValuePath=\"Days\"");
        StringAssert.Contains(xaml, "IsDefault=\"True\"");
        StringAssert.Contains(xaml, "Content=\"Xuất file\"");

        StringAssert.Contains(xaml, "Đếm mã hàng có số tồn thực tế lớn hơn 0.");
        StringAssert.Contains(xaml, "Theo kho và mã hàng sau khi gộp vị trí, lô.");
        StringAssert.Contains(xaml, "Số vị thế đang có hàng được giữ cho đơn.");
        StringAssert.Contains(xaml, "Chỉ tính các lô còn số lượng thực tế.");
        StringAssert.Contains(xaml, "Chỉ cộng các vị thế đã tính được giá vốn.");
        StringAssert.Contains(xaml, "Số vị thế có chênh lệch số lượng hoặc chưa tính đủ giá vốn.");

        StringAssert.Contains(xaml, "Text=\"Tổng quan theo kho\"");
        StringAssert.Contains(xaml, "Text=\"Tồn khả dụng &amp; giá trị hiện tại\"");
        StringAssert.Contains(xaml, "Text=\"Nhập – xuất – tồn theo kỳ\"");
        StringAssert.Contains(xaml, "Text=\"Hàng chậm luân chuyển\"");
        StringAssert.Contains(xaml, "Text=\"Lô, tuổi hàng &amp; hạn dùng\"");
        StringAssert.Contains(xaml, "Text=\"Cần kiểm tra số lượng &amp; giá vốn\"");

        StringAssert.Contains(xaml, "Header=\"Quy đổi\" Binding=\"{Binding PackageBreakdown}\"");
        StringAssert.Contains(xaml, "ToolTip=\"Xem các đơn đang giữ hàng\"");
        StringAssert.Contains(xaml, "Text=\"Đơn đang giữ hàng\"");
        StringAssert.Contains(xaml, "ItemsSource=\"{Binding HoldOrders}\"");
        StringAssert.Contains(xaml, "Không có vị thế tồn hiện tại.");
        StringAssert.Contains(xaml, "Không có phát sinh trong kỳ và không có số dư cuối kỳ.");
        StringAssert.Contains(xaml, "Không có mã hàng chậm luân chuyển theo ngưỡng đã chọn.");
        StringAssert.Contains(xaml, "Không có lô còn hàng trong phạm vi.");
        StringAssert.Contains(xaml, "Không có chênh lệch giá vốn hiện tại.");
        StringAssert.Contains(xaml, "Nguyên tắc số liệu:");

        StringAssert.Contains(viewModel, "\"Quy đổi\", \"Đã giữ\"");
        StringAssert.Contains(viewModel, "InventoryPresentation.DateWithAge");
        StringAssert.Contains(viewModel, "InventoryPresentation.PackageBreakdown(x.OnHandQuantity, metadata)");
        StringAssert.Contains(viewModel, "GetHoldBreakdownAsync(row.WarehouseId, row.VariantId)");
        StringAssert.Contains(viewModel, "Đang tải báo cáo tồn kho");

        StringAssert.Contains(code, "Key.F5");
        StringAssert.Contains(code, "Key.Enter");
        StringAssert.Contains(code, "Key.Escape");
        StringAssert.Contains(code, "ModifierKeys.Control && e.Key == Key.F");
        StringAssert.Contains(code, "ReportWarehouseBox.IsDropDownOpen = true");
        StringAssert.Contains(code, "OpenReportHoldsAsync(position)");
        StringAssert.Contains(code, "CloseReportHolds()");
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
