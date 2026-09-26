using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Partners;

public sealed partial class PartnerViewModel
{
    private static readonly string[] CustomerTemplateHeaders =
    [
        "Mã khách hàng", "Tên khách hàng", "Nhóm khách hàng", "Nhân viên phụ trách", "Điện thoại",
        "Email", "Mã số thuế", "Thời hạn thanh toán", "Hạn mức tín dụng", "Ghi chú"
    ];

    public ApiDownloadFile ExportCustomers()
    {
        if (!CanReadCustomers) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Khách hàng.");
        var rows = CustomerRows.Select(row =>
        {
            var item = row.Source;
            return OfficeDataExportFile.Row(
                item.Code, item.Name, item.GroupName, item.ResponsibleEmployeeName, item.Phone, item.Email,
                item.TaxCode, item.PaymentTermsDays.ToString(), item.CreditLimit, item.Notes,
                item.IsActive ? "Đang hoạt động" : "Không hoạt động");
        }).ToArray();

        return OfficeDataExportFile.Xlsx(
            "danh-sach-khach-hang.xlsx",
            new OfficeExportSheet(
                "Khách hàng",
                ["Mã khách hàng", "Tên khách hàng", "Nhóm khách hàng", "Nhân viên phụ trách", "Điện thoại", "Email", "Mã số thuế", "Thời hạn thanh toán (ngày)", "Hạn mức tín dụng", "Ghi chú", "Trạng thái"],
                rows));
    }

    public ApiDownloadFile ExportSuppliers()
    {
        if (!CanReadSuppliers) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Nhà cung cấp.");
        var rows = SupplierRows.Select(row =>
        {
            var item = row.Source;
            return OfficeDataExportFile.Row(
                item.Code, item.Name, item.TaxId, item.BankName, item.BankAccount, item.PurchaseOwnerEmployeeName,
                item.AvgDeliveryDays?.ToString(), item.IsActive ? "Đang hoạt động" : "Ngừng sử dụng");
        }).ToArray();

        return OfficeDataExportFile.Xlsx(
            "danh-sach-nha-cung-cap.xlsx",
            new OfficeExportSheet(
                "Nhà cung cấp",
                ["Mã Nhà cung cấp", "Tên Nhà cung cấp", "Mã số thuế", "Ngân hàng", "Số tài khoản", "Nhân viên phụ trách mua hàng", "Thời gian giao trung bình (ngày)", "Trạng thái"],
                rows));
    }

    public static ApiDownloadFile CustomerTemplate(string format) =>
        string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            ? OfficeDataExportFile.TemplateCsv("mau-nhap-khach-hang.csv", CustomerTemplateHeaders)
            : OfficeDataExportFile.TemplateXlsx("mau-nhap-khach-hang.xlsx", "Khách hàng", CustomerTemplateHeaders);
}
