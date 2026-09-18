# Cài đặt Công Ty — MCP và tuyến

Audit source of truth trước triển khai Desktop.

- Công Ty source: `binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa`
- Web route: `/settings/mcp-routes`
- Web workspace: `npp-core/web/app/components/employee-mcp-reporting-workspace.tsx`
- Backend API: `GET /api/reporting/employee-mcp`
- Quyền đọc: `core.reporting.employee-mcp.read`
- Bộ lọc canonical: `from`, `to`; backend mặc định từ đầu tháng đến ngày hiện tại và giới hạn tối đa 366 ngày.
- Phạm vi MCP do backend canonical quyết định. Owner/implementation owner được phạm vi installation; tài khoản nhân viên được map theo `shared.employees.code`. Desktop không tự suy diễn chi nhánh, địa bàn hoặc nhân viên.

## Parity UI

Desktop giữ đúng năm phần của Web:

1. Tổng quan.
2. Tuyến và phiên.
3. Điểm bán và lượt ghé.
4. Nhu cầu và đơn hàng.
5. Hiệu quả hoạt động, kèm chất lượng dữ liệu và đối soát.

Giữ hai thao tác liên quan của Web:

- Đề nghị mở mã khách: nối tới màn Desktop đã có.
- Danh mục nhân sự: hiển thị đúng ngữ cảnh nhưng chưa nối cho tới khi màn Quản trị hệ thống tương ứng được triển khai; không điều hướng sang màn sai nghiệp vụ.

## Boundary

- Chỉ đọc báo cáo; không có mutation.
- Không cần Idempotency-Key.
- Không sửa backend, database hoặc migration.
- Không tính lại KPI/business rule trên Desktop; Desktop chỉ định dạng dữ liệu canonical backend trả về.
