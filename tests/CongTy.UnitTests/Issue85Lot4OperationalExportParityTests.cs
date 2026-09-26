namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot4OperationalExportParityTests
{
    [TestMethod]
    public void Lot4_WiresAllElevenOperationalExports()
    {
        var exports = Read("src", "CongTy.Desktop", "Operations", "OperationalExports.cs");
        foreach (var fileName in new[]
        {
            "don-ban-hang-theo-bo-loc.xlsx",
            "phieu-nhan-hang-theo-bo-loc.xlsx",
            "tra-hang-nha-cung-cap-theo-bo-loc.xlsx",
            "hang-khach-tra.xlsx",
            "ket-qua-giao-",
            "lich-su-thu-tien-khach-hang.xlsx",
            "lich-su-thanh-toan-nha-cung-cap.xlsx",
            "giam-cong-no-va-hoan-tien-khach.xlsx",
            "cong-no-phai-thu.xlsx",
            "cong-no-phai-tra.xlsx",
            "gia-mua-nha-cung-cap-theo-bo-loc.xlsx"
        })
        {
            StringAssert.Contains(exports, fileName);
        }

        var views = new[]
        {
            Read("src", "CongTy.Desktop", "Sales", "OrderManagementView.xaml"),
            Read("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml"),
            Read("src", "CongTy.Desktop", "Purchasing", "SupplierReturnView.xaml"),
            Read("src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml"),
            Read("src", "CongTy.Desktop", "Logistics", "DeliveryAttemptView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "CustomerPaymentView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "SupplierPaymentView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "CustomerReturnCreditView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "ReceivablesView.xaml"),
            Read("src", "CongTy.Desktop", "Accounting", "PayablesView.xaml"),
            Read("src", "CongTy.Desktop", "Purchasing", "PurchasePriceView.xaml")
        };
        foreach (var view in views)
        {
            StringAssert.Contains(view, "ExportXlsx_OnClick");
            StringAssert.Contains(view, "Xuất Excel");
        }
    }

    [TestMethod]
    public void Lot4_FilteredExportsDoNotUseVisiblePageOnly()
    {
        var exports = Read("src", "CongTy.Desktop", "Operations", "OperationalExports.cs");
        StringAssert.Contains(exports, "var rows = _filtered.Select(order =>");
        StringAssert.Contains(exports, "Rows.Select(row => row.Data)");
        StringAssert.Contains(exports, "SelectedTripRow");
        StringAssert.Contains(exports, "Attempts.Select(row => row.Data)");

        foreach (var view in new[]
        {
            Read("src", "CongTy.Desktop", "Sales", "OrderManagementView.xaml"),
            Read("src", "CongTy.Desktop", "Purchasing", "GoodsReceiptView.xaml"),
            Read("src", "CongTy.Desktop", "Purchasing", "SupplierReturnView.xaml"),
            Read("src", "CongTy.Desktop", "Logistics", "CustomerReturnView.xaml")
        })
        {
            StringAssert.Contains(view, "ExportCsv_OnClick");
            StringAssert.Contains(view, "Xuất CSV");
        }
    }

    [TestMethod]
    public void Lot4_FinancialExportsKeepCanonicalWorkbookSheetsAndOfficeWording()
    {
        var exports = Read("src", "CongTy.Desktop", "Operations", "OperationalExports.cs");
        foreach (var sheet in new[]
        {
            "Giảm công nợ",
            "Hoàn tiền",
            "Số dư phải thu",
            "Chứng từ phải thu",
            "Số dư phải trả",
            "Chứng từ phải trả"
        })
        {
            StringAssert.Contains(exports, sheet);
        }

        StringAssert.Contains(exports, "Đã ghi vào đơn");
        StringAssert.Contains(exports, "Tiền chưa gắn với đơn");
        StringAssert.Contains(exports, "CustomerPaymentPresentation.PaymentMethod");
        StringAssert.Contains(exports, "SupplierPaymentPresentation.PaymentMethod");
        StringAssert.Contains(exports, "ReceivablesPresentation.CollectionPolicy");
        StringAssert.Contains(exports, "OperationalExportBuild.DeliveryReason");
    }

    [TestMethod]
    public void Lot4_CsvReusesSpreadsheetInjectionGuard()
    {
        var helper = Read("src", "CongTy.Desktop", "Operations", "OfficeDataExportFile.cs");
        StringAssert.Contains(helper, "public static ApiDownloadFile Csv(");
        StringAssert.Contains(helper, "CsvCell(");
        StringAssert.Contains(helper, "GuardSpreadsheetText");
        StringAssert.Contains(helper, "StartsWith('=')");
        StringAssert.Contains(helper, "StartsWith('+')");
        StringAssert.Contains(helper, "StartsWith('-')");
        StringAssert.Contains(helper, "StartsWith('@')");
    }

    [TestMethod]
    public void Lot4_DoesNotIntroduceBackendOrIdempotencyWork()
    {
        var exports = Read("src", "CongTy.Desktop", "Operations", "OperationalExports.cs");
        Assert.IsFalse(exports.Contains("PostAsync", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(exports.Contains("PatchAsync", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(exports.Contains("Idempotency", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(exports.Contains("/api/", StringComparison.OrdinalIgnoreCase));
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
