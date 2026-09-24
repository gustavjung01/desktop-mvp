# Workforce Desktop — Lô 11 Bảng công

Ngày audit: 2026-09-24.

## Baseline

- Desktop base: `gustavjung01/desktop-mvp@9170ecf99410537d407130d9ca1eaa2713adadbc`.
- Push CI trên chính merge commit Lô 10: run `35969118375` (#528) — success.
- Trước branch không có PR Desktop đang mở.
- Web/API authority: `binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3`.
- Parent nghiệp vụ: Issue Web #1110.

## Web/API contract đã audit

Bảng công Web dùng một read endpoint canonical:

- `GET /api/workforce/attendance/timesheet`.

Permission mở endpoint:
- ưu tiên `core.attendance.read` cho phạm vi được cấp;
- nếu không có thì `core.attendance.self.read` và backend ép `selfOnly`;
- `core.attendance.reconcile` và `core.attendance.lock` không tự cấp quyền đọc Bảng công.

Query/filter:
- `view=employee|monthly`;
- `from`, `to`;
- `employeeQuery` tối đa 80 ký tự khi không self-only;
- `branchId` trong scope;
- pagination `limit/offset`;
- tối đa 93 ngày mỗi lần xem.

Read model canonical đã bao gồm:
- policy + schedule effective-dated;
- Attendance Event;
- leave đã duyệt/chờ duyệt;
- adjustment đã duyệt/chờ duyệt;
- period lock;
- violation evaluation;
- trạng thái ngày công và tổng hợp tháng.

Desktop không tự tính lại day status hoặc payroll.

## Desktop implementation

Native workspace **Nhân sự → Bảng công** thay placeholder cũ.

Surface:
- summary kỳ/phạm vi/số nhân sự;
- hai chế độ Theo ngày / Theo tháng;
- filter kỳ/tháng, nhân sự, chi nhánh theo scope;
- pagination;
- bảng tổng hợp nhân sự;
- ma trận tháng 31 ngày;
- chi tiết nhân sự theo từng ngày;
- chi tiết ngày công: giờ vào/ra, thực tế/được tính, nghỉ phép, nguồn dữ liệu, policy, adjustment, lock, violation, event gốc.

Layout Desktop:
- summary + filter cố định;
- chỉ vùng bảng dữ liệu chính cuộn;
- chi tiết mở trong overlay của workspace, không đổi route.

## Boundary

Lô 11 là read-only:
- không có mutation;
- không cần tạo Idempotency-Key;
- không sửa Web/backend;
- không sửa DB/migration;
- không deploy production.

Lô 12 và Lô 13 sẽ nối action Điều chỉnh công và Xử lý vi phạm theo contract mutation riêng; Lô 11 chỉ hiển thị capability/trạng thái hiện có từ read model.
