# Workforce Desktop — Lô 1 lịch sử lao động theo ngày hiệu lực

Ngày audit: 2026-09-22.

## Baseline và nguồn chuẩn

- Desktop base: `gustavjung01/desktop-mvp@7497511ba1c44ebd78b0a818626810c1d39f2c75`.
- Web/backend authority: `binhnxwjfjxm/NPP-Platform@4186ea9638470d2f89882f51de8fa0aa51347654`.
- Kế hoạch: Issue #1140, **Lô 1 — P0 lịch sử lao động**.
- Web implementation gốc: PR #1142, merge commit `efe679db3ef767c92a0156f6f7def1a1185938f6`.
- Migration 149 và backend runtime cho Lô 1 đã được triển khai ở phía NPP-Platform trước task Desktop này.
- Khi bắt đầu task Desktop không có PR Desktop đang mở.

## Contract canonical đã audit

Backend hiện giữ:

- `shared.employees.id` là canonical employee identity;
- employment history effective-dated trong `shared.employee_employments`;
- assignment history effective-dated trong `shared.employee_assignments`;
- resolver dùng chung `resolveEmployeeAtDate(employeeId, businessDate)`;
- khoảng hiệu lực dùng ngày nghiệp vụ dạng `YYYY-MM-DD`, biên inclusive;
- `shared.employees.branch_id` chỉ là current/compatibility projection, không phải historical source;
- Bảng công server chỉ sinh ngày có employment hiệu lực và resolve/scope theo **branch-at-date**;
- legacy backfill có provenance `AUDIT_DERIVED`, `LEGACY_ESTIMATED`, `LEGACY_CURRENT_ONLY`; dữ liệu suy đoán không được trình bày như HR đã xác nhận.

API Desktop dùng contract hiện có, không thêm endpoint:

- `GET /api/employees?limit=1000&offset=0`: danh sách/current projection;
- `GET /api/employees/:id`: hồ sơ chi tiết + `employment_history`, `assignment_history`, `current_employment`, `current_assignment`;
- `POST /api/employees`: nhận `employmentStartDate`, `employmentType`, `assignmentEffectiveFrom`, `assignmentReason`;
- `PATCH /api/employees/:id`: xác nhận/cập nhật lịch sử bằng `confirmEmployment`, `employmentEffectiveFrom/To`, `employmentEndReason`, `confirmAssignment`, `assignmentEffectiveFrom`, `assignmentReason`;
- cùng PATCH khi đổi trạng thái nhận `employmentEffectiveDate`, `employmentReason`, `employmentType`.

Mutation vẫn dùng canonical `Idempotency-Key`; retry cùng logical payload reuse đúng key từ attempt slot hiện có.

## Phạm vi Desktop Lô 1

Desktop thực hiện:

- thêm ngày bắt đầu làm việc và hình thức lao động khi tạo hồ sơ;
- tải hồ sơ chi tiết trước khi mở editor thay vì suy lịch sử từ row danh sách;
- hiển thị lịch sử lao động và lịch sử điều chuyển canonical;
- hiển thị rõ **Cần HR xác nhận** cho legacy history chưa được xác nhận;
- cho phép HR xác nhận khoảng lao động và assignment hiện tại theo ngày hiệu lực;
- khi đổi chi nhánh, gửi ngày hiệu lực + lý do để backend tạo assignment history mới, không update lịch sử cũ;
- khi ngừng/đưa trở lại làm việc, bắt buộc ngày hiệu lực + lý do; rehire gửi hình thức lao động;
- dùng DatePicker/ngôn ngữ văn phòng, không bắt người dùng nhập chuỗi ngày kỹ thuật.

## Boundary Bảng công

Desktop hiện chưa có workspace Bảng công, nên Lô 1 không dựng một historical resolver client-side và không nhân đôi business rule backend.

Gate lịch sử Bảng công đã nằm ở backend canonical:

- employee chỉ xuất hiện trong employment period hiệu lực;
- branch filter/scope dùng branch-at-date;
- Attendance Event không bị rewrite;
- historical query không suy lịch sử từ `shared.employees.branch_id`.

Khi Desktop triển khai Bảng công ở lô màn hình tương ứng, client phải tiêu thụ kết quả backend này và **không suy lịch sử từ shared.employees.branch_id** hoặc current employee projection.

## Không làm trong task này

- không tạo migration Desktop;
- không sửa Web/backend/DB/migration;
- không deploy production;
- không kéo Phòng/Bộ phận, Vị trí, Quản lý trực tiếp của Lô 2 vào phạm vi Lô 1;
- không tạo API/resolver lịch sử thứ hai ở Desktop.
