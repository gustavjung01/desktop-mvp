namespace CongTy.UnitTests;

[TestClass]
public sealed class Issue85Lot2ExportTemplateParityTests
{
    [TestMethod]
    public void Lot2_WorkforceExports_LockWebFileNamesSheetsAndFullPagination()
    {
        var helper = Read("src", "CongTy.Desktop", "Operations", "OfficeDataExportFile.cs");
        var timesheet = Read("src", "CongTy.Desktop", "Workforce", "TimesheetViewModel.Export.cs");
        var leave = Read("src", "CongTy.Desktop", "Workforce", "LeaveViewModel.Export.cs");
        var overtime = Read("src", "CongTy.Desktop", "Workforce", "OvertimeCloseoutViewModel.Export.cs");
        var violation = Read("src", "CongTy.Desktop", "Workforce", "AttendanceViolationViewModel.Export.cs");
        var schedule = Read("src", "CongTy.Desktop", "Workforce", "WorkScheduleViewModel.Export.cs");

        foreach (var file in new[] { "bang-cong-theo-ngay.xlsx", "bang-cong-thang.xlsx", "danh-sach-nghi-phep.xlsx", "danh-sach-tang-ca.xlsx", "xu-ly-vi-pham-cham-cong.xlsx", "ca-va-lich-lam-viec.xlsx" })
            StringAssert.Contains(timesheet + leave + overtime + violation + schedule, file);
        foreach (var sheet in new[] { "Tổng hợp nhân sự", "Bảng công từng ngày", "Lịch sử chấm công", "Ca mẫu", "Lịch tuần" })
            StringAssert.Contains(timesheet + schedule, sheet);
        foreach (var source in new[] { timesheet, leave, overtime, violation })
        {
            StringAssert.Contains(source, "for (var page = 0; page < 200; page++)");
            StringAssert.Contains(source, "Pagination.HasNext");
        }
        StringAssert.Contains(helper, "GuardSpreadsheetText");
        StringAssert.Contains(helper, "StartsWith('=')");
    }

    [TestMethod]
    public void Lot2_MasterExportsAndCustomerTemplates_AreFilterScoped()
    {
        var employees = Read("src", "CongTy.Desktop", "Access", "EmployeeDirectoryViewModel.Export.cs");
        var partners = Read("src", "CongTy.Desktop", "Partners", "PartnerViewModel.Export.cs");
        var employeeService = Read("src", "CongTy.ApiClient", "EmployeeDirectoryReadService.cs");
        var partnerService = Read("src", "CongTy.ApiClient", "PartnerService.cs");

        StringAssert.Contains(employees, "VisibleEmployees.Select");
        StringAssert.Contains(employees, "danh-sach-nhan-vien.xlsx");
        StringAssert.Contains(partners, "CustomerRows.Select");
        StringAssert.Contains(partners, "SupplierRows.Select");
        StringAssert.Contains(partners, "danh-sach-khach-hang.xlsx");
        StringAssert.Contains(partners, "danh-sach-nha-cung-cap.xlsx");
        StringAssert.Contains(partners, "mau-nhap-khach-hang.xlsx");
        StringAssert.Contains(partners, "mau-nhap-khach-hang.csv");
        StringAssert.Contains(employeeService, "ListAllAsync<EmployeeDirectoryData>");
        StringAssert.Contains(partnerService, "ListAllAsync<CustomerData>");
        StringAssert.Contains(partnerService, "ListAllAsync<SupplierData>");
    }

    [TestMethod]
    public void Lot2_OpeningManualInboundAndAdjustment_SupportLockedXlsxTemplates()
    {
        var openingVm = Read("src", "CongTy.Desktop", "Inventory", "OpeningBalanceViewModel.cs");
        var openingPresentation = Read("src", "CongTy.Desktop", "Inventory", "OpeningBalancePresentation.cs");
        var openingView = Read("src", "CongTy.Desktop", "Inventory", "OpeningBalanceView.xaml.cs");
        var inbound = Read("src", "CongTy.Desktop", "Inventory", "ManualInboundView.xaml.cs");
        var adjustment = Read("src", "CongTy.Desktop", "Inventory", "InventoryAdjustmentView.xaml.cs");

        StringAssert.Contains(openingVm, "SpreadsheetMatrixReader.ReadAsync");
        StringAssert.Contains(openingPresentation, "ParseMatrix");
        StringAssert.Contains(openingView, "mau-ton-dau-ky.xlsx");
        StringAssert.Contains(openingView, "\"SKU\", \"Số lượng\", \"Vị trí\"");
        StringAssert.Contains(inbound, "mau-nhap-kho-thu-cong.xlsx");
        StringAssert.Contains(inbound, "\"SKU\", \"Số lượng\", \"Giá vốn\"");
        StringAssert.Contains(adjustment, "mau-dieu-chinh-ton-hang-loat.xlsx");
        StringAssert.Contains(adjustment, "\"SKU\", \"Tồn thực tế\"");
        StringAssert.Contains(openingView, "CSV (*.csv)");
        StringAssert.Contains(inbound, "CSV (*.csv)");
        StringAssert.Contains(adjustment, "CSV (*.csv)");
    }

    [TestMethod]
    public void Lot2_DoesNotCreateDuplicateAdjustmentOrPayrollExports()
    {
        var root = RepoRoot();
        Assert.IsFalse(File.Exists(Path.Combine(root, "src", "CongTy.Desktop", "Workforce", "AttendanceAdjustmentViewModel.Export.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(root, "src", "CongTy.Desktop", "Workforce", "PayrollFoundationViewModel.Export.cs")));
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CongTy.Desktop.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        Assert.Fail("Không tìm thấy root Desktop.");
        return string.Empty;
    }
}
