using CongTy.ApiClient;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Access;

public sealed partial class EmployeeDirectoryViewModel
{
    public ApiDownloadFile ExportEmployees()
    {
        if (!CanViewEmployees) throw new InvalidOperationException("Tài khoản chưa được cấp quyền xem Nhân sự.");

        var rows = VisibleEmployees.Select(item =>
        {
            var employee = item.Source;
            return OfficeDataExportFile.Row(
                employee.Code,
                employee.FullName,
                employee.Phone,
                employee.Email,
                employee.JobTitle,
                employee.CurrentAssignment?.BranchName,
                employee.CurrentAssignment?.DepartmentName,
                employee.CurrentAssignment?.PositionName,
                employee.CurrentAssignment?.ManagerName,
                OfficeDataExportFile.EmploymentType(employee.CurrentEmployment?.EmploymentType),
                employee.IsActive ? "Đang làm việc" : "Ngừng làm việc");
        }).ToArray();

        return OfficeDataExportFile.Xlsx(
            "danh-sach-nhan-vien.xlsx",
            new OfficeExportSheet(
                "Nhân viên",
                ["Mã nhân viên", "Họ và tên", "Điện thoại", "Email", "Chức danh", "Chi nhánh", "Phòng/Bộ phận", "Vị trí công việc", "Quản lý trực tiếp", "Loại lao động", "Trạng thái"],
                rows));
    }
}
