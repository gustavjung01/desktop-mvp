# Workforce Desktop — Lô 4: Nghỉ và đơn nghỉ

## Baseline đã audit

- Desktop repo: `gustavjung01/desktop-mvp`.
- Desktop `main` trước Lô 4: `87d2062778ab81b15e71f46bd034041656a47c26`.
- Push-CI baseline: run `35731537582`, event `push`, conclusion `success`.
- Web chuẩn: `binhnxwjfjxm/NPP-Platform`.
- Web `main` được audit trực tiếp: `9cd5ed9c52932d3b078647e8e754b927af0bd27c`.
- Nghiệp vụ gốc: Web Issue #1140, Web PR #1148. PR cũ chỉ dùng để định vị phạm vi; contract cuối cùng được đối chiếu lại trên Web `main`.

## API contract hiện hành

Desktop chỉ gọi contract đã tồn tại, không dựng endpoint riêng:

- `GET /api/workforce/leave-types`
- `POST /api/workforce/leave-types`
- `POST /api/workforce/leave-types/update`
- `GET /api/workforce/leave/requests`
- `POST /api/workforce/leave/requests`
- `POST /api/workforce/leave/requests/review`
- `POST /api/workforce/leave/requests/cancel`
- `GET /api/workforce/leave/balances`
- `POST /api/workforce/leave/balances/entries`

Các mutation dùng canonical `Idempotency-Key`. Desktop giữ key theo logical payload trong attempt slot; retry cùng payload reuse key cũ và chỉ bỏ slot sau response thành công.

## Permission parity

Các permission canonical lấy từ access snapshot:

- `core.leave.self.read`
- `core.leave.self.request`
- `core.leave.read`
- `core.leave.approve`
- `core.leave-type.manage`

GET đơn nghỉ tự chuyển self-only theo capability backend. Người chỉ có quyền gửi đơn không được biến thành người duyệt ở client.

## Lifecycle đơn nghỉ

- `SUBMITTED`: Chờ duyệt.
- `APPROVED`: Đã duyệt.
- `REJECTED`: Từ chối.
- `CANCELLED`: Đã hủy.
- Nhân viên có `core.leave.self.request` chỉ tự hủy đơn của chính mình khi còn `SUBMITTED`.
- Người có `core.leave.approve` có thể duyệt/từ chối `SUBMITTED`, và hủy `SUBMITTED` hoặc `APPROVED` trong scope.
- Mutation review/cancel gửi `expectedVersion`; optimistic concurrency và period-lock được backend kiểm soát.
- Chế độ không cần duyệt có thể auto-approve ngay khi submit; Desktop hiển thị status backend trả về, không tự đổi trạng thái.

## Số dư và sổ phép

Sổ phép là append-only. Desktop không UPDATE/DELETE ledger và không tự tính engine số dư.

Manual entry canonical mà Desktop cho phép ghi:

- `OPENING_GRANT` — Cấp đầu kỳ.
- `ACCRUAL` — Phát sinh định kỳ.
- `ADJUSTMENT` — Điều chỉnh.
- `CARRY_OVER` — Chuyển năm.
- `EXPIRY` — Hết hạn.
- `COMPENSATORY` — Nghỉ bù.

Entry hệ thống do backend sở hữu:

- `USAGE`: đơn được duyệt tạo usage theo các ngày làm việc thực sự áp dụng.
- `REVERSAL`: hủy đơn đã duyệt tạo reversal tương ứng; lịch sử `USAGE` cũ không bị sửa/xóa.

Quy tắc ngày làm việc, ngày nghỉ Công Ty, effective-dated employment/assignment, negative balance, locked period và future-minimum balance đều để backend là authority. Desktop chỉ làm validation UX tương ứng với Web rồi gửi payload canonical.

## UI Desktop

Workspace `Nghỉ và đơn nghỉ` có ba vùng:

1. Đơn nghỉ: summary, filter ngày/trạng thái/nhân sự/chi nhánh, tạo đơn, duyệt/từ chối, hủy và phân trang.
2. Số dư / Sổ phép: số dư theo ngày, lịch sử biến động và form ghi phát sinh thủ công theo permission.
3. Chế độ nghỉ: danh sách loại nghỉ và cấu hình `tracksBalance` / `allowNegativeBalance` cùng các cờ Web hiện hành.

Loading, empty, error, permission và nhãn tiếng Việt bám pattern văn phòng của Desktop và ngôn ngữ Web.

## Boundary

- Không kéo Tăng ca, Chốt công hoặc Tính lương vào Lô 4.
- Không tự dựng balance engine hoặc leave lifecycle ở Desktop.
- không sửa Web/backend/DB/migration.
- không deploy production trong task này.
