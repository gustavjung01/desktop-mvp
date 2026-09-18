# Danh mục nhân sự — Lô 1 audit

Nguồn chuẩn đã đối chiếu trước khi triển khai Desktop:

- Công Ty source: \`binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa\`
- Web: \`/access/employees\`
- Workspace: \`npp-core/web/app/access/employees/employee-workspace.tsx\`
- Contract: \`npp-core/web/lib/employee-types.ts\`
- Web gateway: \`npp-core/web/lib/employee-gateway.ts\`
- Backend routes: \`npp-core/api/src/routes/employees.js\`
- Backend service: \`npp-core/api/src/services/employee.js\`

## Contract canonical

- \`GET /api/employees?limit=1000&offset=0\`
- \`GET /api/employees/:id\`
- \`GET /api/organization/branches?limit=1000&offset=0\`
- quyền hồ sơ: \`core.employee.read\`
- quyền quản lý: \`core.employee.write\`
- quyền đọc chi nhánh để hiển thị đơn vị công tác: \`core.branch.read\`

Nhân sự là hồ sơ nghiệp vụ, không phải tài khoản đăng nhập. Mã nhân sự là bất biến sau khi tạo.

## Parity Lô 1

- 3 card: Tổng hồ sơ / Đang làm việc / Đã phân công.
- tìm theo mã, họ tên, chức danh, điện thoại, email, mã/tên chi nhánh.
- lọc trạng thái và chi nhánh, có lựa chọn Chưa phân công.
- bảng: Mã nhân sự / Họ và tên / Đơn vị công tác / Liên hệ / Trạng thái / Cập nhật / Thao tác.
- loading, empty, error và cập nhật dữ liệu.
- form tạo/sửa giữ đủ field Web để review parity, nhưng Lô 1 chỉ là draft tại Desktop.
- workspace Desktop: 52, dưới Quản trị hệ thống → Nhân sự và phân quyền.

## Boundary Lô 2

Lô 1 không gọi:

- \`POST /api/employees\`
- \`PATCH /api/employees/:id\`
- Idempotency-Key
- expectedUpdatedAt / optimistic concurrency
- thay đổi trạng thái thật

Mutation, retry và conflict được review riêng ở Lô 2. Không sửa backend, database hoặc migration.