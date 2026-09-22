# Workforce Desktop — Lô 2 cơ cấu tổ chức & hồ sơ

Ngày audit: 2026-09-22.

## Baseline

- Desktop base: `gustavjung01/desktop-mvp@db58b68dd7d3d25c0456fb48cfb4a1962bd54efa`.
- Web/backend authority: `binhnxwjfjxm/NPP-Platform@4186ea9638470d2f89882f51de8fa0aa51347654`.
- Kế hoạch: Issue #1140, **Lô 2 — Cơ cấu tổ chức & hồ sơ**.
- Web implementation: PR #1143, merge commit `482d11d561221ae3848a11cccebc217640f219f6`.
- Khi bắt đầu Lô 2 Desktop không có PR Desktop đang mở.

## Contract canonical đã khóa

Lô 2 phía Web/backend đã có đầy đủ contract; Desktop chỉ tiêu thụ, không tạo mô hình thứ hai:

- Phòng/Bộ phận canonical: `shared.hr_departments`;
- Vị trí công việc canonical: `shared.hr_positions`;
- Phòng/Bộ phận, Vị trí và Quản lý trực tiếp mở rộng **cùng shared.employee_assignments** effective-dated từ Lô 1;
- assignment dùng `department_id`, `position_id`, `manager_employee_id`;
- resolver `resolveEmployeeAtDate` tiếp tục trả cơ cấu tại ngày nghiệp vụ;
- `employees.job_title` chỉ là compatibility/current projection; Vị trí công việc canonical là nguồn nghiệp vụ;
- quản lý trực tiếp dùng canonical `employeeId`, không suy từ tên/chức danh/role.

Không tạo lịch sử tổ chức song song như employee_department_history hoặc employee_manager_history.

## API Desktop sử dụng

Không thêm endpoint/backend:

- `GET /api/employees/organization` trả `departments / positions / managers`;
- `POST /api/employees/organization` tạo/cập nhật/ngừng/dùng lại Phòng/Bộ phận hoặc Vị trí;
- `POST /api/employees` và `PATCH /api/employees/:id` nhận `departmentId / positionId / managerEmployeeId` trong assignment effective-dated hiện có.

Quyền giữ theo route employee hiện hành:

- GET: `core.employee.read`;
- mutation: `core.employee.write`;
- deny-by-default từ backend vẫn là authority.

Mọi mutation organization dùng canonical `Idempotency-Key`; retry cùng logical payload reuse key của attempt slot. Toggle dùng `expectedUpdatedAt` để giữ optimistic concurrency.

## Desktop Lô 2

Desktop bổ sung trong chính workspace **Danh mục nhân sự**:

- nút/popup **Cơ cấu tổ chức**, không sinh tab sidebar mới;
- danh mục Phòng/Bộ phận: mã, tên, cấp trên, trạng thái, thêm mới, ngừng/dùng lại;
- danh mục Vị trí công việc: mã, tên, Phòng/Bộ phận, trạng thái, thêm mới, ngừng/dùng lại;
- editor hồ sơ chọn Phòng/Bộ phận, Vị trí công việc, Quản lý trực tiếp;
- chọn Vị trí canonical tự đồng bộ compatibility `jobTitle`; text chức danh cũ chỉ còn là gợi ý để chuẩn hóa khi chưa chọn Vị trí;
- thay đổi Chi nhánh/Phòng/Bộ phận/Vị trí/Quản lý đều đi qua cùng assignment ngày hiệu lực + lý do của Lô 1;
- lịch sử điều chuyển hiển thị Chi nhánh, Phòng/Bộ phận, Vị trí, Quản lý trực tiếp từ snapshot assignment;
- danh sách nhân sự hiển thị Vị trí/Phòng/Bộ phận/Quản lý trực tiếp hiện tại.

Backend đã chặn các invariant nghiệp vụ và Desktop không tự viết lại:

- self-manager;
- reporting-line cycle;
- position/department mismatch;
- department hierarchy cycle;
- không ngừng Phòng/Bộ phận khi còn đơn vị con hoặc Vị trí đang hoạt động;
- optimistic concurrency.

## Không làm

- không sửa Web/backend/DB/migration;
- không tạo migration Desktop;
- không tạo approval engine/team scope mới;
- không tạo tab **Cơ cấu tổ chức** riêng ở sidebar;
- không deploy production trong task Desktop này.
