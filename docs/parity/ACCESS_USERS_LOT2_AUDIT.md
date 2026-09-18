# Người dùng — Lô 2 mutation audit

Nguồn chuẩn:

- Công Ty source: `binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa`
- Web: `/access/users`
- Web workspace: `npp-core/web/app/access/users/user-workspace.tsx`
- Backend route: `npp-core/api/src/routes/access-users.js`
- Backend service: `npp-core/api/src/services/access-users.js`
- Xác thực nội bộ: `npp-core/api/src/routes/internal-workforce-auth.js`

## Contract mutation

- Tạo tài khoản: `POST /api/access/users`
  - quyền `core.user.write`
  - bắt buộc Idempotency-Key
  - `loginName, employeeId, isActive`
- Gán vai trò: `PATCH /api/access/users/:id/roles`
  - quyền `core.user-role.write`
  - bắt buộc Idempotency-Key
  - `roleIds, expectedUpdatedAt`
- Trạng thái: `PATCH /api/access/users/:id`
  - quyền `core.user.write`
  - bắt buộc Idempotency-Key
  - `isActive, expectedUpdatedAt`
- Mật khẩu: `PUT /api/internal-auth/users/:id/credential`
  - quyền `core.user.write`
  - `password`, từ 10 đến 256 ký tự
  - route backend hiện không có idempotency replay contract; Desktop không giả lập contract bằng khóa tự chế.
  - đặt mật khẩu sẽ thu hồi các phiên đăng nhập cũ của tài khoản.

## Luồng tạo an toàn

Desktop giữ đúng luồng Web:

1. tạo tài khoản với `isActive=false`;
2. gán vai trò;
3. đặt mật khẩu;
4. nếu người dùng chọn hoạt động thì mới kích hoạt.

Nếu bước sau tạo thất bại, Desktop không tạo lại identity. Biểu mẫu chuyển sang chế độ hoàn tất tài khoản đã tạo, giữ vai trò/mật khẩu/trạng thái mong muốn để retry.

## Idempotency

Các endpoint có contract Idempotency-Key dùng `ICanonicalIdempotencyKeyProvider`. Khóa gửi API không được tự ghép. Cùng một intent đang retry reuse đúng khóa đã tạo; intent mới tạo khóa mới. Ký tự khóa tuân `[A-Za-z0-9._-]`.

## Concurrency và bảo vệ

- vai trò và trạng thái dùng `expectedUpdatedAt`.
- `CONFLICT` khóa lưu tiếp cho tới khi tải lại dữ liệu.
- `SECURITY_OWNER_PROTECTED` hiển thị bằng ngôn ngữ văn phòng.
- tên đăng nhập và nhân sự liên kết không đổi sau khi tạo.
- vai trò đã ngừng nhưng đang gán vẫn hiển thị để có thể thu hồi.

## Boundary

Lô 2 không sửa phạm vi chi nhánh/kho. `/api/internal-auth/users/:id/scopes` thuộc Lô 3.
Không sửa backend, database hoặc migration.
