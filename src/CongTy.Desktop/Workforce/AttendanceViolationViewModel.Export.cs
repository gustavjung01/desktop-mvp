using System.Globalization;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class AttendanceViolationViewModel
{
    public async Task<ApiDownloadFile?> ExportViolationsAsync(CancellationToken cancellationToken = default)
    {
        if (FromDate is null || ToDate is null) throw new InvalidOperationException("Chọn kỳ vi phạm cần xuất.");
        var from = FromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = ToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var branch = OfficeDataExportFile.FilterValue(SelectedBranch?.Id);
        var employeeQuery = string.IsNullOrWhiteSpace(EmployeeQuery) ? null : EmployeeQuery.Trim();

        var entries = new List<AttendanceViolationHandlingEntryData>();
        var offset = 0;
        for (var page = 0; page < 200; page++)
        {
            var response = await _service.ListAsync(
                from, to, employeeQuery, string.IsNullOrWhiteSpace(branch) ? null : branch,
                100, offset, cancellationToken).ConfigureAwait(true);
            entries.AddRange(response.Entries);
            if (!response.Pagination.HasNext) break;
            if (response.Entries.Length == 0) throw new InvalidOperationException("Không thể tiếp tục phân trang Vi phạm.");
            offset += response.Entries.Length;
            if (page == 199) throw new InvalidOperationException("Danh sách vi phạm vượt giới hạn xuất an toàn 200 trang.");
        }

        var rows = entries.Select(item => OfficeDataExportFile.Row(
            OfficeDataExportFile.DateLabel(item.WorkDate), item.Employee.Code, item.Employee.Name, item.Employee.BranchName,
            item.Violation?.Label, item.Violation?.Detail, item.Violation?.Minutes?.ToString(CultureInfo.InvariantCulture),
            OfficeDataExportFile.ViolationStatus(item.Case?.Status), item.Case?.Explanation,
            OfficeDataExportFile.ViolationOutcome(item.Case?.Outcome), item.Case?.ReviewNote)).ToArray();

        return OfficeDataExportFile.Xlsx(
            "xu-ly-vi-pham-cham-cong.xlsx",
            new OfficeExportSheet(
                "Vi phạm chấm công",
                ["Ngày", "Mã nhân viên", "Nhân viên", "Chi nhánh", "Vi phạm", "Chi tiết", "Số phút", "Trạng thái xử lý", "Giải trình", "Kết luận", "Ghi chú xử lý"],
                rows));
    }
}
