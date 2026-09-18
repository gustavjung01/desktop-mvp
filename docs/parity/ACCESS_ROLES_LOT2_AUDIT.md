# Vai trò và phân quyền — Lô 2 mutation

Lô 2 hoàn thiện mutation trên nền Lô 1 đã merge.

## Contract canonical

- `POST /api/access/roles`
  - code
  - name
  - description
  - isActive
  - webLoginChallengeRequired
  - permissionKeys
- `PATCH /api/access/roles/:id`
  - sửa vai trò: name, description, isActive, webLoginChallengeRequired, permissionKeys, expectedUpdatedAt
  - bật/tắt: isActive, expectedUpdatedAt
- quyền ghi: `core.role.write`
- POST/PATCH bắt buộc canonical `Idempotency-Key`.
- Desktop dùng `ICanonicalIdempotencyKeyProvider`; không tự tạo header key.
- cùng một intent retry giữ cùng key; chỉ bỏ key khi mutation thành công.
- permissionKeys được sắp ổn định trước khi tạo fingerprint.

## Optimistic concurrency

- PATCH luôn gửi `expectedUpdatedAt` lấy từ role đang hiển thị.
- `CONFLICT` / HTTP 409 không được ghi đè.
- form sửa bị khóa lưu sau conflict và hiện **Tải lại dữ liệu**.
- bật/tắt gặp conflict giữ nguyên dữ liệu hiện tại, yêu cầu cập nhật trước khi thử lại.
- `DUPLICATE_CODE` cũng là HTTP 409 nhưng không được coi là optimistic conflict; Desktop hiển thị lỗi canonical của backend.

## Trạng thái

- Ngừng sử dụng không xóa vai trò.
- Có hộp xác nhận riêng:
  - Đưa vai trò vào sử dụng.
  - Ngừng sử dụng vai trò.
- Nội dung xác nhận nêu rõ vai trò ngừng sử dụng vẫn được giữ để đối soát và lịch sử chứng từ.

## Boundary

- Không sửa backend.
- Không sửa database/migration.
- Không tự ghi audit/outbox; backend canonical thực hiện audit/outbox trong transaction.
- Không deploy production.
