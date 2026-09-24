# Workforce Desktop — Lô 13 Xử lý vi phạm chấm công

Ngày audit: 2026-09-24.

## Baseline

- Desktop base: `gustavjung01/desktop-mvp@e354ceda7cd23d8f4a8d8b4f61b2d5d9b83adf89`.
- Push CI trên chính merge commit Lô 12: run `35974509620` (#540) — success.
- Trước branch không có PR Desktop đang mở.
- Web/API authority: `binhnxwjfjxm/NPP-Platform@af4acc24bc411d3206d07256f6119472068c01c3`.
- Parent nghiệp vụ: Issue Web #1110 và comment kế hoạch bổ sung nghỉ/vắng/vi phạm.

## Boundary nghiệp vụ

Violation truth được **derive từ Bảng công hiện tại**, không phải Attendance Event mới và không phải nguồn tính lương.

Hồ sơ xử lý chỉ lưu workflow:
- snapshot vi phạm tại lúc nhân viên giải trình;
- nội dung giải trình;
- trạng thái xem xét;
- kết luận quản lý;
- optimistic version;
- audit/request lineage.

Không sửa/xóa Attendance Event. Không tự phạt tiền, trừ lương hoặc đổi thu nhập.

## API contract đã audit

Canonical backend routes:

- `GET /api/workforce/attendance/violations`;
- `POST /api/workforce/attendance/violations/explain`;
- `POST /api/workforce/attendance/violations/review`.

Danh sách:
- mặc định tháng hiện tại;
- `from`, `to`, tối đa 93 ngày;
- `employeeQuery` tối đa 80 ký tự theo Timesheet contract;
- `branchId` theo scope;
- pagination `limit/offset`;
- entry gồm violation hiện tại và case snapshot nếu có.

Loại vi phạm canonical:
- `LATE`;
- `EARLY_LEAVE`;
- `MISSING_ATTENDANCE`;
- `UNEXCUSED_ABSENCE`.

Workflow case:
- `EXPLANATION_SUBMITTED`;
- `UNDER_REVIEW`;
- `RESOLVED`.

Kết luận:
- `EXCUSED` — Chấp nhận giải trình;
- `CONFIRMED` — Xác nhận vi phạm.

`CONFIRMED` bắt buộc backend derive lại Timesheet và chỉ cho kết luận nếu vi phạm vẫn còn hiện tại. Nếu dữ liệu công đã thay đổi và vi phạm không còn thì backend trả `VIOLATION_CHANGED`.

## Permission authority

GET danh sách cho phép:
- `core.attendance.read` hoặc `core.attendance-violation.resolve` => scoped/company read;
- `core.attendance.self.read` hoặc `core.attendance-violation.self-explain` => self-only nếu không có scoped read.

Mutation:
- `core.attendance-violation.self-explain` cho nhân viên gửi giải trình của chính mình;
- `core.attendance-violation.resolve` cho quản lý bắt đầu xem xét và kết luận trong scope.

Desktop trước Lô 13 thiếu `core.attendance.read` và `core.attendance.self.read` ở menu Vi phạm. Lô 13 sửa parity defect này. Tài khoản chỉ có quyền đọc được mở workspace ở chế độ chỉ xem.

## Idempotency

Hai mutation Desktop dùng shared canonical generator:

- `desktop-attendance-violation-explain`;
- `desktop-attendance-violation-review`.

Logical slot = mutation kind + serialized payload. Retry cùng payload reuse key cũ; key chỉ bỏ khỏi cache sau success. `START_REVIEW` và `CONCLUDE` là payload khác nhau nên có logical key riêng.

Review luôn gửi `expectedVersion`; concurrent review/conclusion bị backend chống bằng optimistic concurrency.

## Desktop implementation

Native workspace **Nhân sự → Xử lý vi phạm công** thay placeholder.

Surface:
- 4 summary: vi phạm hiện tại / chờ xem xét / đang xem xét / đã kết luận;
- filter thời gian, nhân sự, chi nhánh theo scope;
- bảng hồ sơ + pagination;
- read-only mode cho quyền chỉ xem;
- nhân viên gửi giải trình tối đa 2.000 ký tự;
- quản lý `START_REVIEW`;
- quản lý kết luận `EXCUSED` hoặc `CONFIRMED` kèm ghi chú bắt buộc tối đa 2.000 ký tự;
- hiển thị snapshot khi violation hiện tại đã thay đổi;
- Bảng công có deep-link **Mở xử lý vi phạm** khi ngày đang có violation.

Layout Desktop: header/summary/filter/banner cố định; DataGrid là vùng dữ liệu chính tự cuộn; modal giải trình/kết luận nằm trong workspace.

## Boundary repo

- chỉ sửa Desktop;
- không sửa Web/backend;
- không sửa DB/migration;
- không thêm payroll/penalty;
- không deploy production.
