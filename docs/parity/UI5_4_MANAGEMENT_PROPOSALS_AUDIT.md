# UI-5.4 — Đề xuất — Audit parity

## Baseline

- Desktop repo: `gustavjung01/desktop-mvp`
- Desktop baseline: `666aff9cff71b4b9d3afcad392d3075fa7ef5996`
- Web source of truth: `/management/proposals`
- Backend contract: `/api/management-proposals`
- Permission: `core.management-proposal.submit`

## Phạm vi 5.4

Màn Đề xuất của Công Ty gồm đúng ba luồng nghiệp vụ Web hiện hành:

1. Gửi Đề xuất mới lên Admin.
2. Xem lại các Đề xuất do chính người dùng gửi và phản hồi của Admin.
3. Khi trạng thái là `needs-info`, bổ sung nội dung / giải trình / bằng chứng rồi gửi lại.

Chỉ **Tiêu đề** và **Nội dung đề xuất** là bắt buộc. Nhóm xử lý, mức ưu tiên, đối tượng liên quan, lý do, tác động, điều kiện và bằng chứng là thông tin bổ sung.

## Contract đã đối chiếu

- GET `/api/management-proposals?source=company`
- POST `/api/management-proposals`
- POST `/api/management-proposals/{id}/resubmit`
- Trạng thái: `pending`, `needs-info`, `approved`, `rejected`
- Nhóm: `commercial`, `customer-debt`, `operations`
- Mức ưu tiên: `normal`, `high`, `critical`
- Đọc dữ liệu deny-by-default và backend chỉ trả đúng đề xuất thuộc người gửi, trừ vai trò quản trị hệ thống.

## Idempotency

Desktop dùng duy nhất `ICanonicalIdempotencyKeyProvider`.

- Gửi mới: scope `company-management-proposal`
- Gửi bổ sung: scope `company-management-proposal-resubmit`
- Cùng payload retry lại dùng đúng key cũ.
- Payload thay đổi được xem là intent mới và sinh key mới.
- Không tự ghép Idempotency-Key, không dùng ký tự ngoài `[A-Za-z0-9._-]`.

## Boundary

Không sửa backend, DB hoặc migration vì contract hiện tại đã đủ. Không triển khai UI-5.5+ trong lô này.
