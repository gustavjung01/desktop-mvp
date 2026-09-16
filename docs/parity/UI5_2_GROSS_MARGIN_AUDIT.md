# UI-5.2 — Lãi gộp: audit parity

## Baseline

- Desktop: `gustavjung01/desktop-mvp`, branch `agent/ui5-2-gross-margin` từ `main@36332eb79727a16f9adee5d6656c75fc546e3715`.
- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform`, audit trên `main@0f645e584378e6763720594bd3604828644cb583`.
- Web chuẩn: `/sales/gross-margin`.
- Không thay backend, database, migration hoặc production deploy.

## Source of truth

- `npp-core/web/app/sales/gross-margin/page.tsx`
- `npp-core/web/app/components/gross-margin-reporting-workspace.tsx`
- `npp-core/web/app/components/gross-margin-reporting-export-dialog.tsx`
- `npp-core/web/lib/finance-reporting-types.ts`
- `npp-core/web/lib/finance-reporting-gateway.ts`
- `npp-core/web/lib/gross-margin-reporting-export-gateway.ts`
- `npp-core/api/src/routes/reporting-sales-purchasing.js`
- `npp-core/api/src/routes/reporting-finance.js`
- `npp-core/api/src/services/reporting-gross-margin-export.js`

## Backend contract

### Đọc

`GET /api/reporting/gross-margin`

- quyền: `core.reporting.gross-margin.read`;
- bộ lọc: `from`, `to`, `warehouseId`;
- mặc định kỳ: đầu tháng hiện tại → hôm nay theo giờ Việt Nam;
- tối đa 366 ngày;
- phạm vi kho do backend kiểm tra và deny-by-default.

### Xuất

`GET /api/reporting/gross-margin-export`

- quyền: `core.reporting.export` và đồng thời phải có `core.reporting.gross-margin.read`;
- nội dung: `customers | skus | lines | exceptions`;
- định dạng: `xlsx | csv`;
- lặp `column` theo cột chọn;
- Desktop tải bytes từ backend, không tự dựng file từ bảng đang hiển thị.

## Web → Desktop

| Web | Desktop UI-5.2 |
| --- | --- |
| Header Lãi gộp | Shell title/subtitle, không lặp trong workspace |
| Xuất báo cáo | Topbar → cửa sổ chọn nội dung, Excel/CSV và cột |
| Đơn bán hàng | Topbar → màn Đơn bán hàng hiện có |
| Giá vốn | Topbar + action trong tab Theo SKU → màn Giá vốn tồn kho hiện có |
| Từ ngày / Đến ngày / Kho | DatePicker + kho lấy từ dữ liệu báo cáo như Web |
| KPI | Doanh thu thuần so sánh được / Giá vốn / Lãi gộp / Biên lãi gộp |
| Đối soát | Hiển thị đủ số dòng so sánh được, thiếu liên kết chứng từ, thiếu giá vốn, bất thường giá vốn, chưa quy đổi VND |
| Tab 1 | Theo khách hàng |
| Tab 2 | Theo SKU |
| Tab 3 | Ngoại lệ |
| Empty | Giữ đúng thông điệp của Web |
| Loading/error | Có trạng thái riêng, không giữ dữ liệu cũ sau đổi phiên/quyền |
| Keyboard | F5 tải lại, Esc đóng cửa sổ xuất |

## An toàn phiên và quyền

UI-5.2 là báo cáo chỉ đọc. Không có mutation và không cần Idempotency-Key. ViewModel hủy request đang chạy và tăng generation khi session/access đổi; response hoặc file từ phiên cũ không được áp dụng/lưu.

## Ngôn ngữ văn phòng

UI không hiển thị UUID, permission key, API path, tên bảng, `canonical`, `Phase 7` hoặc `cost fact`. Ngoại lệ dùng nhãn nghiệp vụ giống Web:
- Doanh thu không phải VND
- Thiếu liên kết xuất/nhập kho
- Chưa có dữ liệu giá vốn
- Dữ liệu giá vốn có bất thường
