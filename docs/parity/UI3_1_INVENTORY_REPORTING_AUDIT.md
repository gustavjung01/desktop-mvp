# UI-3.1 — Audit Báo cáo tồn kho theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue: #31.
- Desktop baseline ban đầu: `main@dbe451787973452f65605cc77c07b58c09684846`.
- Desktop main trước khi chốt UI-3.1: `3acd7799ac15be929cb6fecc426bc51126c655b6` sau UI-2.2.
- Công Ty Web/backend đã re-audit trước khi chốt: `NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Các commit Web từ baseline audit ban đầu đến SHA trên không thay đổi màn `npp-core/web/app/components/inventory-reporting-workspace.tsx`, contract báo cáo tồn kho hoặc route hold.
- Không deploy, không migration, không thay production DB.

## Web → Desktop parity matrix

| Khu vực | Công Ty Web | Desktop trước UI-3.1 | UI-3.1 |
| --- | --- | --- | --- |
| Bộ lọc | Từ ngày → Đến ngày → Kho → Chậm luân chuyển → Áp dụng → Đặt lại | Có nhưng ngưỡng chỉ hiện số | Giữ đúng thứ tự, ngưỡng hiển thị “N ngày”, Enter áp dụng |
| Sáu tab | Tổng quan → Tồn hiện tại → Luân chuyển → Chậm luân chuyển → Lô & hạn dùng → Cần kiểm tra | Có tên tab | Giữ đúng thứ tự và bổ sung nội dung bên trong |
| Thẻ tổng hợp | Nhãn + số + giải thích | Chỉ nhãn + số | Đủ 6 thẻ + giải thích |
| Trạng thái dữ liệu | Có trạng thái cập nhật sổ kho/tồn/giá vốn | Có chuỗi kỹ thuật ngắn | Dùng ngôn ngữ văn phòng, có loading/error |
| Tổng quan | Heading + mô tả + bảng + empty | Chỉ bảng | Đủ |
| Tồn hiện tại | Heading + mô tả + quy đổi + xem đơn giữ | Thiếu quy đổi và drill-down | Đủ |
| Luân chuyển | Heading + mô tả + 2 bảng + empty | Có 2 bảng, thiếu mô tả/empty | Đủ |
| Chậm luân chuyển | Heading + ngưỡng + empty | Chỉ bảng | Đủ |
| Lô & hạn dùng | Heading + tuổi hàng + empty | Chỉ ngày sản xuất | Đủ tuổi hàng khi backend có dữ liệu |
| Cần kiểm tra | Heading + mô tả + empty | Chỉ bảng | Đủ |
| Export | Xuất theo dataset báo cáo | Desktop CSV | Giữ CSV native, đồng bộ cột Quy đổi |
| Keyboard | Form/controls dùng bàn phím | Ctrl+F ở report rời màn | F5 refresh, Ctrl+F mở Kho, Enter mở hold, Esc đóng popup |
| Hold drill-down | GET `/api/inventory/holds` | Không có | Gọi canonical API, đủ loading/error/empty |
| Nguồn số liệu | Báo cáo dùng backend canonical | Không nói rõ trên UI | Có dòng nguyên tắc nguồn số liệu |

## Contract đã đối chiếu

### Báo cáo tồn kho
`GET /api/reporting/inventory`

Filter:
- `from`
- `to`
- `warehouseId`
- `slowDays`

Payload:
- `summary`
- `periodFlow`
- `movementTypes`
- `warehouseSummary`
- `currentPositions`
- `slowMoving`
- `expiryLots`
- `exceptions`
- `projectionState`

### Đơn đang giữ hàng
`GET /api/inventory/holds?warehouseId=...&baseVariantId=...`

Web và Desktop cùng dùng:
- tổng đang giữ;
- danh sách đơn;
- khách hàng;
- mã hàng bán → mã hàng gốc nếu khác;
- kho;
- hình thức giao/nhận;
- số lượng giữ.

Route là read-only và cho phép quyền `core.reporting.inventory.read`; Desktop không dựng lại danh sách hold từ dữ liệu đơn bán.

## Quyết định kỹ thuật

- Không sửa backend/DB vì backend đã có contract canonical.
- Không sửa shared Shell/navigation trong UI-3.1.
- Không thêm nút giả sang Giá vốn tồn kho khi target Desktop chưa được triển khai ở UI-3.7.
- Quy đổi ở bảng Tồn hiện tại dùng metadata đơn vị từ balance nhưng số lượng dùng chính quantity của report; không lấy nhầm quantity của một dòng vị trí.
- Danh sách kho filter không được co lại sau khi tải report theo một kho.
- Khi refresh report lỗi, thông báo lỗi hiển thị rõ; dữ liệu report đã tải trước đó không bị dựng lại bằng dữ liệu giả.

## Gate

UI-3.1 chỉ được coi là đạt sau khi:
1. diff cuối chỉ nằm trong Inventory/client/contracts/test/audit;
2. Desktop CI xanh trên exact SHA;
3. parity baseline với Công Ty Web main hiện hành pass;
4. Native startup smoke pass;
5. không merge/deploy/migration trong bước này.
