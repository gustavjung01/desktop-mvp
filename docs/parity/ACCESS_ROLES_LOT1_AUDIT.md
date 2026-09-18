# Vai trò và phân quyền — Lô 1 audit

Nguồn chuẩn đã đối chiếu trước khi triển khai Desktop:

- Công Ty source: `binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa`
- Web: `/access/roles`
- Workspace: `npp-core/web/app/access/roles/role-workspace.tsx`
- Preset: `npp-core/web/app/access/roles/role-presets.ts`
- Contract: `npp-core/web/lib/access-types.ts`
- Backend routes: `npp-core/api/src/routes/access.js`

## Lô 1

Chỉ đọc và dựng bề mặt quản trị:

- GET `/api/access/permissions`
- GET `/api/access/roles`
- GET `/api/access/roles/:id`
- quyền đọc vai trò: `core.role.read`
- quyền đọc danh mục quyền: `core.permission.read`
- quyền `core.role.write` chỉ dùng để quyết định có được mở form chuẩn bị chỉnh sửa; Lô 1 không gửi mutation.
- workspace Desktop: 51.

Parity giữ:

- 3 card Tổng vai trò / Đang sử dụng / Danh mục quyền.
- search và bộ lọc trạng thái.
- bảng Mã vai trò / Tên / Quyền / Trạng thái / Cập nhật / Thao tác.
- form tạo/sửa ở trạng thái local draft.
- 12 mẫu quyền gợi ý.
- ma trận quyền nhóm theo module canonical và dùng ngôn ngữ văn phòng.
- mã vai trò được chuẩn hóa uppercase và chỉ giữ chữ/số/gạch dưới/gạch ngang trong draft.

## Boundary Lô 2

Lô 1 tuyệt đối chưa gọi:

- POST `/api/access/roles`
- PATCH `/api/access/roles/:id`
- canonical Idempotency-Key
- expectedUpdatedAt / optimistic concurrency
- bật/tắt trạng thái thật.

Các phần trên thuộc Lô 2 để mutation, retry và conflict handling được review riêng.
