namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot5ReportExportParityTests
{
    [TestMethod]
    public void Lot5_WiresExactlyFiveReportWorkbooks()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var fileName in new[]
        {
            "bao-cao-mua-hang.xlsx",
            "bao-cao-tuoi-no.xlsx",
            "bao-cao-cod.xlsx",
            "bao-cao-giao-van.xlsx",
            "bao-cao-nhan-vien-mcp.xlsx"
        })
        {
            StringAssert.Contains(source, fileName);
        }

        foreach (var view in new[]
        {
            Read("src", "CongTy.Desktop", "Purchasing", "PurchasingReportingView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "AgingReportingView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "CodAccountingView.xaml"),
            Read("src", "CongTy.Desktop", "Logistics", "LogisticsReportingView.xaml"),
            Read("src", "CongTy.Desktop", "Settings", "EmployeeMcpReportingView.xaml")
        })
        {
            StringAssert.Contains(view, "ExportXlsx_OnClick");
            StringAssert.Contains(view, "Xuất Excel");
            Assert.IsFalse(view.Contains("Xuất CSV", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void Lot5_PurchasingExportMatchesWebWorkbookShape()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var sheet in new[] { "Tổng hợp", "Giá trị theo tiền tệ", "Trạng thái", "Xu hướng theo ngày", "Nhà cung cấp", "Sản phẩm" })
            StringAssert.Contains(source, $"new OfficeExportSheet(\"{sheet}\"");

        StringAssert.Contains(source, "ReportExportLabels.PurchaseDimension");
        StringAssert.Contains(source, "ReportExportLabels.PurchaseStatus");
    }

    [TestMethod]
    public void Lot5_AgingContractIncludesDocumentRowsAndWorkbookKeepsSixSheets()
    {
        var contracts = Read("src", "CongTy.Contracts", "AgingReportingContracts.cs");
        StringAssert.Contains(contracts, "public sealed record AgingDocumentData");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"documents\")] public AgingDocumentData[] Documents");

        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var sheet in new[]
        {
            "Phải thu tổng hợp",
            "Phải thu khách hàng",
            "Phải thu chứng từ",
            "Phải trả tổng hợp",
            "Phải trả Nhà cung cấp",
            "Phải trả chứng từ"
        })
            StringAssert.Contains(source, $"new OfficeExportSheet(\"{sheet}\"");

        Assert.IsFalse(source.Contains(".ReceivableDocumentId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains(".PayableDocumentId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains(".SourceDocumentId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot5_CodExportUsesReportScopeAndOfficeLabels()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var sheet in new[]
        {
            "Thông tin báo cáo",
            "Tiền tài xế",
            "Thu trong kỳ",
            "Bàn giao",
            "Kế toán tiếp nhận",
            "Hẹn thu quá hạn",
            "Cần kiểm tra"
        })
            StringAssert.Contains(source, $"new OfficeExportSheet(\"{sheet}\"");

        StringAssert.Contains(source, "ReportExportLabels.CodWarehouse(report.Filters.WarehouseId, report.Warehouses)");
        StringAssert.Contains(source, "ReportExportLabels.CodMethod");
        StringAssert.Contains(source, "ReportExportLabels.CodStatus");
        Assert.IsFalse(source.Contains("row.CollectionId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("row.HandoverId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot5_LogisticsReadsAttemptsAndDoesNotLeakTechnicalIds()
    {
        var contracts = Read("src", "CongTy.Contracts", "LogisticsReportingContracts.cs");
        StringAssert.Contains(contracts, "public sealed record LogisticsReportingAttemptData");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"attempts\")] public LogisticsReportingAttemptData[] Attempts");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"routeCode\")]");
        StringAssert.Contains(contracts, "[JsonPropertyName(\"routeName\")]");

        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var sheet in new[] { "Tổng hợp", "Tài xế", "Phương tiện", "Kết quả lần giao", "Chuyến giao", "Cần kiểm tra" })
            StringAssert.Contains(source, $"new OfficeExportSheet(\"{sheet}\"");

        StringAssert.Contains(source, "report.Attempts.Select");
        StringAssert.Contains(source, "ReportExportLabels.DeliveryResult");
        StringAssert.Contains(source, "ReportExportLabels.DeliveryReason");
        StringAssert.Contains(source, "ReportExportLabels.LogisticsStatus");
        Assert.IsFalse(source.Contains("row.AttemptId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("row.TripId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("row.DeliveryOrderId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot5_EmployeeMcpKeepsFiveOfficeSheets()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        foreach (var sheet in new[] { "Tổng hợp", "Nhân viên", "Tuyến", "Phiên đi thị trường", "Cần đối soát" })
            StringAssert.Contains(source, $"new OfficeExportSheet(\"{sheet}\"");

        StringAssert.Contains(source, "ReportExportLabels.Actor");
        StringAssert.Contains(source, "ReportExportLabels.EmployeeSessionStatus");
        StringAssert.Contains(source, "ReportExportLabels.EmployeeException");
        Assert.IsFalse(source.Contains("row.SessionId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("row.EmployeeId", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("row.RouteId", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lot5_ReusesDesktopSpreadsheetFoundationWithoutNewApiOrMutation()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "ReportExports.cs");
        StringAssert.Contains(source, "OfficeDataExportFile.Xlsx");
        Assert.IsFalse(source.Contains("/api/", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("PostAsync", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(source.Contains("PatchAsync", StringComparison.OrdinalIgnoreCase));

        foreach (var token in new[]
        {
            "if (!CanRead)",
            "if (!CanReadReport)"
        })
            StringAssert.Contains(source, token);
    }

    [TestMethod]
    public void Lot5_DoesNotRegressExistingSalesInventoryAndGrossMarginExports()
    {
        var sales = Read("src", "CongTy.Desktop", "Sales", "SalesReportingView.xaml");
        var inventory = Read("src", "CongTy.Desktop", "Inventory", "InventoryView.xaml");
        var grossMargin = Read("src", "CongTy.Desktop", "Sales", "GrossMarginReportingView.xaml");

        foreach (var source in new[] { sales, inventory, grossMargin })
        {
            StringAssert.Contains(source, "Excel (.xlsx)");
            StringAssert.Contains(source, "CSV (.csv)");
        }
    }

    private static string Read(params string[] parts)
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
