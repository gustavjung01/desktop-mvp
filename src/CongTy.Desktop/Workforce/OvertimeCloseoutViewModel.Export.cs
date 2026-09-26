using System.Globalization;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class OvertimeCloseoutViewModel
{
    public async Task<ApiDownloadFile?> ExportOvertimeAsync(CancellationToken cancellationToken = default)
    {
        if (FromDate is null || ToDate is null) throw new InvalidOperationException("Chọn kỳ tăng ca cần xuất.");
        var from = FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var status = OfficeDataExportFile.FilterValue(SelectedStatus?.Value);
        var branch = OfficeDataExportFile.FilterValue(SelectedBranch?.Value);

        var requests = new List<OvertimeRequestData>();
        var offset = 0;
        for (var page = 0; page < 200; page++)
        {
            var response = await _service.ListOvertimeAsync(
                from, to, string.IsNullOrWhiteSpace(status) ? null : status,
                null, null, string.IsNullOrWhiteSpace(branch) ? null : branch,
                100, offset, cancellationToken).ConfigureAwait(true);
            requests.AddRange(response.Requests);
            if (!response.Pagination.HasNext) break;
            if (response.Requests.Length == 0) throw new InvalidOperationException("Không thể tiếp tục phân trang Tăng ca.");
            offset += response.Requests.Length;
            if (page == 199) throw new InvalidOperationException("Danh sách tăng ca vượt giới hạn xuất an toàn 200 trang.");
        }

        var rows = requests.Select(item => OfficeDataExportFile.Row(
            OfficeDataExportFile.DateLabel(item.WorkDate), item.EmployeeCode, item.EmployeeName, item.BranchName,
            item.RequestedMinutes.ToString(CultureInfo.InvariantCulture), item.ActualMinutes?.ToString(CultureInfo.InvariantCulture),
            item.ConfirmedMinutes?.ToString(CultureInfo.InvariantCulture), OfficeDataExportFile.OvertimeStatus(item.Status),
            item.Reason, item.ReviewReason, item.ActualNote, item.ConfirmNote)).ToArray();

        return OfficeDataExportFile.Xlsx(
            "danh-sach-tang-ca.xlsx",
            new OfficeExportSheet(
                "Tăng ca",
                ["Ngày", "Mã nhân viên", "Nhân viên", "Chi nhánh", "Phút đăng ký", "Phút thực tế", "Phút được tính", "Trạng thái", "Lý do đăng ký", "Ý kiến duyệt", "Ghi chú thực tế", "Ghi chú xác nhận"],
                rows));
    }
}
