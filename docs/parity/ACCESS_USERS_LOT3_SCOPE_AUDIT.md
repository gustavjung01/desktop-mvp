# Người dùng — Lô 3 phạm vi chi nhánh & kho

Nguồn chuẩn đã audit:
- Công Ty source: binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa
- Web: /access/users/scopes
- Workspace: npp-core/web/app/access/users/scopes/user-scope-workspace.tsx
- Web proxy: npp-core/web/app/api/access/users/[id]/scopes/route.ts
- Backend: npp-core/api/src/routes/internal-workforce-auth.js
- Scope service: npp-core/api/src/internal-workforce-auth.js
- Organization routes: npp-core/api/src/routes/organization.js

## Canonical backend

- Người dùng: GET /api/access/users?limit=1000&offset=0
- Chi nhánh: GET /api/branches?limit=1000&offset=0
- Kho hàng: GET /api/warehouses?limit=1000&offset=0
- Lưu phạm vi: PUT /api/internal-auth/users/:id/scopes
- Payload: scopes.branchIds, scopes.warehouseIds, scopes.territoryIds=[]
- Quyền: core.user.read, core.branch.read, core.warehouse.read
- Quyền lưu: core.user-role.write

Web proxy /api/access/users/:id/scopes không phải backend canonical cho Desktop.

## Quy tắc UI/nghiệp vụ

- Mục Người dùng giữ hai tab con: Tài khoản / Phạm vi chi nhánh & kho.
- Danh sách tài khoản bên trái, phạm vi bên phải.
- Chọn kho tự chọn chi nhánh chứa kho.
- Bỏ chi nhánh tự bỏ toàn bộ kho thuộc chi nhánh đó.
- Bỏ một kho không tự bỏ chi nhánh.
- Chi nhánh/kho ngừng sử dụng vẫn hiển thị với nhãn ngừng sử dụng / lịch sử.
- Phạm vi trống hợp lệ; thông báo rõ tài khoản sẽ không thấy dữ liệu theo kho.
- Tài khoản có owner_kind dùng phạm vi toàn Công Ty, kể cả lịch sử; không cấp tay.
- Số kho hiệu lực của Owner bằng toàn bộ kho trong Công Ty.

## Idempotency

Web yêu cầu canonical Idempotency-Key và chuyển header xuống backend.
Desktop dùng ICanonicalIdempotencyKeyProvider, cùng intent retry dùng lại đúng key.
Backend scope route hiện audit giao dịch nhưng không triển khai replay-idempotency riêng, vì vậy Desktop không suy diễn guarantee replay phía server.

## Boundary

- Không thêm territory scope vì registry canonical chưa cấu hình.
- Không sửa backend, database hoặc migration.
- Không deploy production.
