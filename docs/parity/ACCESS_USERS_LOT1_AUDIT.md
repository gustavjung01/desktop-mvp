# Người dùng — Lô 1 read/UI audit

Nguồn chuẩn đã đối chiếu trước khi triển khai Desktop:

- Công Ty source: `binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa`
- Web tài khoản: `/access/users`
- Web phạm vi: `/access/users/scopes`
- Workspace tài khoản: `npp-core/web/app/access/users/user-workspace.tsx`
- Workspace phạm vi: `npp-core/web/app/access/users/scopes/user-scope-workspace.tsx`
- Contract: `npp-core/web/lib/access-types.ts`
- Backend users: `npp-core/api/src/routes/access-users.js`
- Backend service: `npp-core/api/src/services/access-users.js`

## Contract canonical Lô 1

- `GET /api/access/users?limit=1000&offset=0`
- `GET /api/access/users/:id`
- nhân sự: `GET /api/employees?limit=1000&offset=0`
- vai trò: `GET /api/access/roles?limit=1000&offset=0`
- quyền xem người dùng: `core.user.read`
- quyền đọc nhân sự: `core.employee.read`
- quyền đọc vai trò: `core.role.read`

## Parity UI

Mục sidebar chỉ có một mục `Người dùng`. Bên trong Web có hai tab con:

- `Tài khoản`
- `Phạm vi chi nhánh & kho`

Lô 1 triển khai tab Tài khoản ở chế độ đọc và giữ tab Phạm vi hiển thị trong cấu trúc để không làm sai navigation; nghiệp vụ phạm vi được bật ở Lô 3.

Tab Tài khoản gồm:

- 3 card: Tổng tài khoản / Đang hoạt động / Ngừng sử dụng.
- tìm theo tên đăng nhập, nhân sự và tên vai trò.
- lọc trạng thái.
- bảng: Tên đăng nhập / Nhân sự / Vai trò / Trạng thái / Cập nhật / Hành động.
- loading, empty, error.
- workspace Desktop: 53.

## Boundary

Lô 1 không gọi POST/PATCH/PUT, không thay mật khẩu, không thay vai trò, không đổi trạng thái, không cập nhật phạm vi và không dùng Idempotency-Key.

Mutation tài khoản thuộc Lô 2; phạm vi chi nhánh & kho thuộc Lô 3.
