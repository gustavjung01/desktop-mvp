# Cài đặt Công Ty — MCP và tuyến

Audit source-of-truth cập nhật cho Desktop Issue #95 Lô 2.

- Công Ty source: `binhnxwjfjxm/NPP-Platform@a5f76e7782e51ee2925f8d05d1e53ca58b40a367`.
- Web route: `/settings/mcp-routes`.
- Delta owner: Web PR #1236.
- Web workspace: `npp-core/web/app/settings/mcp-routes/mcp-route-settings-workspace.tsx`.
- Web server gateway: `npp-core/web/lib/mcp-route-settings-gateway.ts`.

## Owner hiện tại

`MCP và tuyến` không còn là màn báo cáo Hiệu suất nhân viên thị trường.

Màn Web hiện quản lý route master:
- đọc `GET /api/routes/data`;
- tạo `POST /api/routes`;
- sửa `PATCH /api/routes/:id`;
- xóa/archive `POST /api/routes/:id/archive`;
- mutation dùng canonical Idempotency-Key và retry cùng logical operation reuse đúng key.

Báo cáo Hiệu suất nhân viên thị trường vẫn dùng canonical Core:
- `GET /api/reporting/employee-mcp`;
- quyền `core.reporting.employee-mcp.read`;
- deep-link Web `/access/employees/performance`;
- không mutation.

## Security boundary của Desktop

Route setup Web chạy qua server-only gateway và bắt buộc:
- `MCP_API_INTERNAL_URL`;
- `MCP_API_SERVER_TOKEN`;
- private header `X-Backend-Token`;
- workforce identity được Web server dựng sau khi xác minh Core session.

MCP public gateway vẫn từ chối request thiếu private backend token. Core backend hiện không có workforce-safe proxy cho route CRUD.

Vì Desktop là client được phân phối tới máy người dùng, Desktop **không được**:
- lưu `MCP_API_SERVER_TOKEN` trong settings/credential client;
- gửi trực tiếp `X-Backend-Token`;
- tự dựng service identity để đi vòng workforce authorization;
- tự thêm contract backend chưa tồn tại.

Do đó Lô 2:
- tách báo cáo khỏi owner `MCP và tuyến`;
- giữ `MCP và tuyến` fail-closed, chỉ hiện trạng thái contract chưa khả dụng trên Desktop;
- chỉ mở route mutation khi backend có contract workforce client không cần bí mật máy chủ.

Đây là blocker contract thật, không phải thiếu UI Desktop.

## Boundary

- Không sửa Web/MCP backend/Core backend/DB/migration.
- Không deploy.
- Không hạ cấp authorization.
- Không đưa server secret xuống Desktop.
