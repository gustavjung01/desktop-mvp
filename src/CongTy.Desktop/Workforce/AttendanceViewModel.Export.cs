using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public sealed partial class AttendanceViewModel
{
    public ApiDownloadFile ExportTodayAttendance()
    {
        if (!CanReadToday || _today is null)
            throw new InvalidOperationException("Chưa có dữ liệu chấm công hôm nay để xuất.");

        var rows = _today.Events.Select(item => OfficeDataExportFile.Row(
            _today.Employee.Code,
            _today.Employee.FullName,
            _today.Employee.BranchName,
            WorkSchedulePresentation.DateTimeText(item.OccurredAt, _today.Policy.Timezone),
            OfficeDataExportFile.AttendanceAction(item.EventType),
            OfficeDataExportFile.MovementReason(item.MovementReason),
            OfficeDataExportFile.AttendanceSource(item.Source),
            item.PointName,
            OfficeDataExportFile.Validation(item.ValidationStatus),
            item.Note)).ToArray();

        return OfficeDataExportFile.Xlsx(
            "lich-su-cham-cong-hom-nay.xlsx",
            new OfficeExportSheet(
                "Lịch sử chấm công",
                ["Mã nhân viên", "Nhân viên", "Chi nhánh", "Thời điểm", "Thao tác", "Lý do ra ngoài", "Hình thức ghi nhận", "Nơi ghi nhận", "Kết quả xác minh", "Ghi chú"],
                rows));
    }
}
