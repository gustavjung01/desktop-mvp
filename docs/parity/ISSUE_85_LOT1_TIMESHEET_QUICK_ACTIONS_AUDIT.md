# Issue #85 — Lô 1: Thao tác nhanh Bảng công

## Baseline

- Desktop main trước Lô 1: `4758a29efa8f953e6d3ffda664439b598c7282ab`.
- Web authority: `384c58186067d0bc4bc0705c6442d33775df6695`.
- Issue: #85.
- Desktop main push CI trước khi branch: run `36092350588` — success.

## Delta đã triển khai

- Popup Chi tiết ngày công có Thao tác nhanh theo Web:
  - CHECK_IN — Chấm vào.
  - TEMP_EXIT — Ra ngoài.
  - RETURN — Quay lại.
  - CHECK_OUT — Kết thúc làm việc.
  - END_EXTERNAL_WORK — Kết thúc công việc bên ngoài.
- Mục đích ra ngoài: WORK_BUSINESS, PERSONAL, BREAK, OTHER; OTHER bắt buộc ghi chú.
- Quick attendance chỉ bật cho ngày hiện tại khi `capabilities.canManage` và đúng quy tắc kỳ khóa.
- Ngày cũ không ghi sự kiện “bây giờ”; quản lý sửa giờ vào/ra bằng direct adjustment có lý do và audit.
- Self-service yêu cầu điều chỉnh cũ được giữ nguyên để không regression.

## API contract

Desktop dùng trực tiếp core canonical:
- `POST /api/workforce/attendance/manual`.
- `POST /api/workforce/attendance/adjustments/direct`.

Không tạo alias BFF của Web trên Desktop.

## Idempotency

- Quick action dùng logical slot `timesheet-attendance-quick-action|<payload>`.
- Direct adjustment dùng logical slot `timesheet-attendance-direct-adjustment|<payload>`.
- Cùng logical payload retry reuse cùng canonical key.
- Chỉ xóa slot sau mutation thành công; lỗi retry giữ key cũ.

## Boundary

- Không sửa Web.
- Không sửa backend.
- Không sửa DB/schema/migration.
- Không deploy production.
