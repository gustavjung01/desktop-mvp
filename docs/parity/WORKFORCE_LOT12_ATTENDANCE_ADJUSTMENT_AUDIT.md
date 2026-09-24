# Workforce Desktop — Lô 12 Điều chỉnh công

Ngày audit: 2026-09-24.

## Baseline

- Desktop base: `gustavjung01/desktop-mvp@6bde0ff0f4c64923d9ce1f77305a8afc622927d2`.
- Push CI trên chính merge commit Lô 11: run `35971514040` (#534) — success.
- Trước branch không có PR Desktop đang mở.
- Web/API authority: `binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3`.
- Parent nghiệp vụ: Issue Web #1110.

## Web/API contract đã audit

Điều chỉnh công dùng các endpoint backend canonical:

- `GET /api/workforce/attendance/adjustments`;
- `POST /api/workforce/attendance/adjustments`;
- `POST /api/workforce/attendance/adjustments/review`;
- `POST /api/workforce/attendance/adjustments/direct`;
- `GET /api/workforce/attendance/period-locks`;
- `POST /api/workforce/attendance/period-locks`.

Permission authority:

- `core.attendance.self-adjust-request`: xem yêu cầu của bản thân và tự gửi yêu cầu;
- `core.attendance.adjust`: xem phạm vi được cấp, duyệt/từ chối và điều chỉnh trực tiếp;
- `core.attendance.lock`: xem/khóa kỳ công và cho phép xử lý ngoại lệ trên kỳ đã khóa;
- riêng `core.attendance.lock` không tự cấp quyền mở màn Điều chỉnh công.

Scope và validation do backend giữ authority:
- self-only ép employee của tài khoản;
- quản lý bị giới hạn theo company/branch scope;
- mỗi lần xem hoặc khóa tối đa 93 ngày;
- từ khóa nhân sự tối đa 80 ký tự;
- ngày điều chỉnh không được ở tương lai;
- phải có ít nhất giờ vào hoặc giờ ra;
- giờ điều chỉnh chỉ thuộc ngày công hoặc ngày kế tiếp đối với ca qua ngày;
- giờ ra không được sớm hơn giờ vào;
- lý do tối đa 1.000 ký tự;
- từ chối bắt buộc có ý kiến;
- một ngày không được có thêm điều chỉnh trực tiếp khi còn yêu cầu chờ xử lý;
- kỳ đã khóa chặn yêu cầu mới của nhân viên;
- duyệt/điều chỉnh trực tiếp trên kỳ đã khóa cần đồng thời quyền `core.attendance.lock`.

Backend ghi Attendance Event nguồn `ADJUSTMENT` theo kiểu append-only và tự audit before/after. Desktop không sửa hay thay thế lịch sử chấm công gốc.

## Idempotency

Bốn mutation Desktop dùng canonical `Idempotency-Key`:

- `desktop-attendance-adjustment-submit`;
- `desktop-attendance-adjustment-review`;
- `desktop-attendance-adjustment-direct`;
- `desktop-attendance-period-lock`.

Key được giữ theo serialized logical payload. Retry cùng payload reuse key cũ; chỉ xóa key khỏi cache sau khi mutation thành công. Payload thay đổi tạo logical slot mới và key mới.

## Desktop implementation

Native workspace **Nhân sự → Điều chỉnh công** thay placeholder cũ.

Surface:
- summary yêu cầu trong kỳ, chờ duyệt và kỳ đã khóa;
- filter thời gian, trạng thái, nhân sự và chi nhánh theo capability;
- lịch sử yêu cầu + phân trang;
- tự gửi yêu cầu của nhân viên;
- quản lý duyệt/từ chối với optimistic `expectedVersion`;
- điều chỉnh trực tiếp cho nhân sự;
- lịch sử kỳ khóa và tạo khóa kỳ;
- hỗ trợ ca qua ngày bằng lựa chọn “Ngày kế tiếp” cho giờ vào/ra;
- deep-link hai chiều với Bảng công; từ chi tiết ngày công mở đúng action, ngày và nhân sự theo capability.

Danh sách nhân sự cho điều chỉnh trực tiếp dùng `EmployeeDirectoryReadService` hiện có; mutation backend vẫn là authority cuối cùng cho scope.

Layout Desktop giữ summary/filter cố định; các vùng danh sách và panel thao tác cuộn trong phần nội dung.

## Boundary

- không sửa Web/backend;
- không sửa DB/migration;
- không thêm nghiệp vụ ngoài Web;
- không deploy production.
