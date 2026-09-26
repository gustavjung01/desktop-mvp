using System.Globalization;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class LeaveViewModel
{
    public async Task<ApiDownloadFile?> ExportLeaveAsync(CancellationToken cancellationToken = default)
    {
        if (FromDate is null || ToDate is null) throw new InvalidOperationException("Chọn kỳ nghỉ phép cần xuất.");
        var from = FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var status = OfficeDataExportFile.FilterValue(SelectedStatus?.Value);
        var branch = OfficeDataExportFile.FilterValue(SelectedBranch?.Value);
        var employeeQuery = string.IsNullOrWhiteSpace(EmployeeQuery) ? null : EmployeeQuery.Trim();

        var requests = new List<LeaveRequestData>();
        var offset = 0;
        for (var page = 0; page < 200; page++)
        {
            var response = await _service.ListLeaveRequestsAsync(
                from, to, string.IsNullOrWhiteSpace(status) ? null : status, employeeQuery,
                string.IsNullOrWhiteSpace(branch) ? null : branch, 100, offset, cancellationToken).ConfigureAwait(true);
            requests.AddRange(response.Requests);
            if (!response.Pagination.HasNext) break;
            if (response.Requests.Length == 0) throw new InvalidOperationException("Không thể tiếp tục phân trang Nghỉ phép.");
            offset += response.Requests.Length;
            if (page == 199) throw new InvalidOperationException("Danh sách nghỉ phép vượt giới hạn xuất an toàn 200 trang.");
        }

        var rows = requests.Select(item => OfficeDataExportFile.Row(
            item.EmployeeCode, item.EmployeeName, item.BranchName, item.LeaveTypeNameSnapshot,
            OfficeDataExportFile.DateLabel(item.DateFrom), OfficeDataExportFile.DateLabel(item.DateTo),
            OfficeDataExportFile.LeavePart(item.DayPart), item.LeaveIsPaidSnapshot ? "Có" : "Không",
            item.LeaveCountsAsWorkdaySnapshot ? "Có" : "Không", OfficeDataExportFile.LeaveStatus(item.Status),
            item.Reason, item.RequestSource == "MANUAL_PAPER" ? "Phiếu giấy / nhập thủ công" : "Phiếu điện tử",
            item.ManualApproverName, item.ReviewReason, item.CancelReason)).ToArray();

        return OfficeDataExportFile.Xlsx(
            "danh-sach-nghi-phep.xlsx",
            new OfficeExportSheet(
                "Nghỉ phép",
                ["Mã nhân viên", "Nhân viên", "Chi nhánh", "Chế độ nghỉ", "Từ ngày", "Đến ngày", "Phần ngày", "Hưởng lương", "Tính ngày công", "Trạng thái", "Lý do", "Nguồn ghi nhận", "Người duyệt phiếu giấy", "Ý kiến xử lý", "Lý do hủy"],
                rows));
    }
}
