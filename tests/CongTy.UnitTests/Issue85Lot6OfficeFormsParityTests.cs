using System.Text.RegularExpressions;

namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot6OfficeFormsParityTests
{
    [TestMethod]
    public void Lot6_CatalogContainsExactlyThirtyFiveLockedFormsAndFormats()
    {
        var source = Read("src", "CongTy.Desktop", "Operations", "OfficeFormsCatalog.cs");

        Assert.HasCount(35, Regex.Matches(source, @"new OfficeFormDefinition\("));
        Assert.HasCount(13, Regex.Matches(source, @"\bXlsx:\s"));
        Assert.HasCount(29, Regex.Matches(source, @"\bPdf:\s"));

        foreach (var name in new[]
        {
            "Mẫu đơn bán hàng / phiếu đặt hàng khách hàng",
            "Mẫu báo giá",
            "Biên bản đối chiếu công nợ khách hàng",
            "Mẫu khai báo thông tin khách hàng",
            "Mẫu đơn mua hàng",
            "Mẫu phiếu nhận hàng Nhà cung cấp",
            "Mẫu phiếu trả hàng Nhà cung cấp",
            "Mẫu phiếu chi / thanh toán Nhà cung cấp",
            "Biên bản đối chiếu công nợ Nhà cung cấp",
            "Mẫu khai báo thông tin Nhà cung cấp",
            "Mẫu phiếu nhập kho",
            "Mẫu phiếu chuyển kho",
            "Mẫu phiếu kiểm kê",
            "Mẫu phiếu điều chỉnh tồn",
            "Mẫu phiếu soạn hàng / cấp hàng",
            "Mẫu phiếu chuyến giao hàng",
            "Biên bản bàn giao hàng cho tài xế",
            "Biên bản bàn giao / thu tiền COD",
            "Biên bản đối soát chuyến",
            "Biên bản đối soát COD",
            "Đơn xin nghỉ phép",
            "Phiếu đăng ký tăng ca",
            "Phiếu điều chỉnh chấm công",
            "Bảng chấm công tháng trống",
            "Lịch làm việc / phân ca tháng trống",
            "Bản giải trình vi phạm chấm công",
            "Biên bản xử lý vi phạm chấm công",
            "Bảng kê khoản lương bổ sung / khấu trừ"
        })
            StringAssert.Contains(source, name);

        foreach (var group in new[]
        {
            "Bán hàng & khách hàng",
            "Mua hàng & Nhà cung cấp",
            "Kho",
            "Giao vận & COD",
            "Nhân sự"
        })
            StringAssert.Contains(source, group);

        Assert.IsFalse(source.Contains("Phiếu lương trống", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("CSV", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Lot6_DesktopTabSupportsSearchGroupFilterBlankXlsxAndPrintPdf()
    {
        var view = Read("src", "CongTy.Desktop", "Operations", "DataExchangeView.xaml");
        var library = Read("src", "CongTy.Desktop", "Operations", "OfficeFormsLibraryView.xaml");
        var code = Read("src", "CongTy.Desktop", "Operations", "OfficeFormsLibraryView.xaml.cs");
        var print = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");

        StringAssert.Contains(view, "Header=\"Biểu mẫu văn phòng\"");
        StringAssert.Contains(view, "<operations:OfficeFormsLibraryView");
        StringAssert.Contains(library, "Tìm biểu mẫu");
        StringAssert.Contains(library, "Nhóm nghiệp vụ");
        StringAssert.Contains(library, "Xuất dữ liệu:");
        StringAssert.Contains(library, "File mẫu nhập liệu:");
        StringAssert.Contains(library, "Biểu mẫu văn phòng:");
        StringAssert.Contains(library, "Tải Excel");
        StringAssert.Contains(library, "Xem / In PDF");

        StringAssert.Contains(code, "DataExchangeFileHelper.Write");
        StringAssert.Contains(code, "\"xlsx\"");
        StringAssert.Contains(print, "DocumentPrintTemplateRuntime.CreateDocument");
        StringAssert.Contains(print, "AddOfficeHeader");
        StringAssert.Contains(print, "AddOfficeSignatures");
        StringAssert.Contains(print, "In / lưu PDF");
    }

    [TestMethod]
    public void Lot6_OfficeFormsDoNotFetchProductionDataOrMutateAndDirectOpenDoesNotLoadReferences()
    {
        var catalog = Read("src", "CongTy.Desktop", "Operations", "OfficeFormsCatalog.cs");
        var library = Read("src", "CongTy.Desktop", "Operations", "OfficeFormsLibraryView.xaml.cs");
        var print = Read("src", "CongTy.Desktop", "Operations", "OfficeFormPrintPreview.cs");
        var vm = Read("src", "CongTy.Desktop", "Operations", "DataExchangeViewModel.cs");

        var combined = catalog + library + print;
        foreach (var token in new[] { "/api/", "PostAsync", "PatchAsync", "Idempotency", "CustomerId", "SupplierId", "EmployeeId" })
            Assert.IsFalse(combined.Contains(token, StringComparison.OrdinalIgnoreCase), token);

        StringAssert.Contains(vm, "Math.Clamp(value, 0, 5)");
        StringAssert.Contains(vm, "public bool IsOfficeFormsTab => SelectedTabIndex == 5;");
        StringAssert.Contains(vm, "if (_loaded || IsBusy || !CanOpen || IsOfficeFormsTab) return;");
        StringAssert.Contains(vm, "if (IsBusy || !CanOpen || IsOfficeFormsTab) return;");
        StringAssert.Contains(vm, "public bool CanRefresh => CanOpen && !IsOfficeFormsTab && !IsBusy;");
    }

    [TestMethod]
    public void Lot6_DoesNotRegressLot3PrintAndLot2ToLot5Exports()
    {
        var lot3 = Read("tests", "CongTy.UnitTests", "Issue85Lot3ActualDocumentPrintParityTests.cs");
        var lot4 = Read("tests", "CongTy.UnitTests", "Issue85Lot4OperationalExportParityTests.cs");
        var lot5 = Read("tests", "CongTy.UnitTests", "Issue85Lot5ReportExportParityTests.cs");
        var lot2 = Read("tests", "CongTy.UnitTests", "Issue85Lot2ExportTemplateParityTests.cs");

        StringAssert.Contains(lot3, "Lot3_WiresSevenActualDocumentsToCanonicalTemplateRuntime");
        StringAssert.Contains(lot4, "Issue85Lot4OperationalExportParityTests");
        StringAssert.Contains(lot5, "Lot5_WiresExactlyFiveReportWorkbooks");
        StringAssert.Contains(lot2, "Issue85Lot2ExportTemplateParityTests");
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
