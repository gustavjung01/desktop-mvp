# UI-5.1 — Báo cáo bán hàng: audit parity

## Baseline

- Desktop: `binhnxwjfjxm/congty-desktop`, branch `agent/ui5-1-sales-reporting-parity` từ `main@033ea5d78e8af668f32686d0b09e3c788a4bda46`.
- Công Ty Web/backend: `binhnxwjfjxm/NPP-Platform`, audit trên `main@698324a530f4c1741cd20ab0ba1ee0596fc862d8`.
- Master UI/UX: Issue #31.
- Không thay backend, database, migration hoặc production deploy trong lô này.
- Re-audit 2026-09-16 trên `NPP-Platform/main@6f40f749436c6abe9df2f7bd594362368db96a2c`: so với baseline `698324a...`, thay đổi chỉ thuộc xuất Nhập kho thủ công, mẫu in và Retail; các file Web/API/permission của Báo cáo bán hàng và Tuổi nợ giữ nguyên byte-for-byte.
- Trong lúc làm, Công Ty source tiến từ `21751ced...` lên `1d5fdb80...`. Drift mới chỉ thêm route export Lãi gộp và dịch dòng route reporting; UI-5.1 không đổi contract. Parity manifests được đồng bộ đúng source mới cùng cách với luồng UI-7 đang chạy song song.

## Source of truth đã đọc

- `npp-core/web/app/sales/reporting/page.tsx`
- `npp-core/web/app/components/sales-reporting-workspace.tsx`
- `npp-core/web/app/components/sales-reporting-export-dialog.tsx`
- `npp-core/web/lib/sales-reporting-types.ts`
- `npp-core/api/src/routes/reporting-sales-purchasing.js`
- `docs/operations/phase-8-1-sales-purchasing-reporting.md`
- Desktop hiện tại: `SalesView*`, `App.xaml.cs`, `MainWindow.xaml/.cs`, `ShellViewModel.cs`.

Khi tài liệu Phase 8.1 cũ khác source Web hiện hành, source Web/backend hiện hành là chuẩn parity.

## Contract backend

### Đọc báo cáo

`GET /api/reporting/sales`

Quyền bắt buộc: `core.reporting.sales.read`.

Bộ lọc server:
- `from`, `to`;
- `warehouseId`;
- `productGroupId`;
- `customerGroupId`;
- `includeZeroProducts`.

Phạm vi kho do backend kiểm soát. Kỳ mặc định do backend quyết định; kỳ tối đa 366 ngày.

### Xuất file chính thức

`GET /api/reporting/sales-export`

Quyền bắt buộc:
- `core.reporting.export`;
- đồng thời phải có `core.reporting.sales.read`.

Tham số xuất:
- đúng bộ lọc đã áp dụng;
- `dimension`;
- `format=xlsx|csv`;
- lặp `column` theo các cột được chọn.

Desktop gọi chính endpoint canonical và lưu bytes do backend tạo; không tự dựng Excel/CSV từ các dòng đang hiển thị.

## Matrix Web → Desktop

| Web | Desktop UI-5.1 |
| --- | --- |
| Header Báo cáo bán hàng | Shell title/subtitle, không lặp trong workspace |
| Xuất báo cáo | Topbar → cửa sổ chọn XLSX/CSV + cột |
| Mở đơn bán hàng | Topbar → màn Đơn bán hàng hiện có |
| Xem báo cáo lãi gộp | Giữ đúng action ở topbar, disabled có chủ đích tới khi UI-5.2 có màn thật; không tạo điều hướng giả |
| Kỳ nhanh | Hôm nay / 7 ngày / Tháng này / Tháng trước, chọn là áp dụng |
| Từ ngày / Đến ngày | DatePicker |
| Kho | Danh sách `scopeWarehouses` từ chính báo cáo |
| Chiều phân tích | Khách hàng / Loại khách / Kênh bán / Sản phẩm / Nhóm hàng / Nhân viên bán hàng |
| Nhóm khách | Chỉ hiện ở chiều Khách hàng |
| Nhóm sản phẩm + mã không phát sinh | Chỉ hiện ở chiều Sản phẩm |
| Tiền tệ | Lọc cục bộ trên chiều đang xem |
| So với kỳ trước | Tất cả / so sánh được / mới / không phát sinh |
| Tìm trong danh sách | Mã hoặc tên, tối đa 80 ký tự |
| Xóa lọc / Áp dụng | Giữ đúng flow Web |
| Lưu chế độ xem | Lưu local dimension/search/currency/comparison; không lưu bộ lọc server |
| KPI | Doanh thu / Đơn đã chốt / Khách mua / Mặt hàng đã bán |
| Chi tiết chiều phân tích | Bảng đầy đủ doanh thu, chỉ tiêu động, tỷ trọng, kỳ trước, thay đổi, Xem |
| Đối soát + chất lượng dữ liệu | Trạng thái đối soát, số chênh lệch, cảnh báo |
| Tổng theo tiền tệ | `breakdownTotals`, không cộng chéo tiền tệ |
| Chi tiết một dòng | 8 chỉ tiêu như Web |
| Doanh thu theo ngày | Chuỗi biểu đồ theo từng tiền tệ + bảng ngày |
| Loading/error/empty | Busy/disabled, lỗi văn phòng và empty state tách đúng ngữ cảnh |
| Phím tắt Desktop | F5 cập nhật; Ctrl+F vào tìm kiếm; Escape đóng chi tiết/cửa sổ xuất |

## Quy tắc số liệu

- Không cộng chéo tiền tệ.
- Sản lượng chỉ đi cùng đơn vị của dòng/variant.
- So sánh kỳ trước dùng payload canonical từ backend.
- Export dùng bộ lọc **đã áp dụng**, không dùng draft chưa bấm Áp dụng.
- Dữ liệu chi tiết cục bộ chỉ lọc search/currency/comparison; nhóm khách, nhóm sản phẩm và kho phải reload backend như Web.

## Quyền và trạng thái

- Không có `core.reporting.sales.read`: không cho mở màn.
- Có quyền đọc nhưng thiếu `core.reporting.export`: xem báo cáo bình thường, nút xuất không khả dụng.
- Backend lỗi: hiển thị thông báo văn phòng, giữ request id qua `CanonicalErrorMessages`.
- Export lỗi hoặc reconciliation không đạt: không tạo file giả.

## Song song UI-7

Trong lúc UI-5.1 đang làm, PR #73 `UI-7.1 — Tuổi nợ` đang chạy song song trên branch `agent/ui7-aging`.

Hai PR cùng chạm:
- `App.xaml.cs`;
- `MainWindow.xaml`;
- `MainWindow.xaml.cs`;
- `ShellViewModel.cs`.

Vì vậy trước merge UI-5.1 phải re-audit `main` và PR/branch UI-7. Nếu UI-7 merge trước thì UI-5.1 phải sync main, giữ nguyên màn Tuổi nợ và dời/đối chiếu workspace index nếu cần, sau đó chạy lại exact-head CI. Không merge bằng CI của cây trước khi hợp nhất.

## Gate

- Có parity/regression tests riêng.
- CI phải xanh trên exact head.
- Không backend/DB/migration/deploy.
- Visual gate cuối vẫn cần đối chiếu app thật/screenshot với Web theo Issue #31.


## Tích hợp UI-7.1 trước merge

- UI-7.1 đã merge vào `main@033ea5d78e8af668f32686d0b09e3c788a4bda46`.
- Tuổi nợ giữ workspace index 31; Báo cáo bán hàng dùng workspace index 32.
- Bốn file dùng chung `App.xaml.cs`, `ShellViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs` được hợp nhất trên cây `main`, giữ nguyên code UI-7.1.
- Parity manifests giữ bản mới trên `main` theo Công Ty source `698324a530f4c1741cd20ab0ba1ee0596fc862d8`.
- Exact-head CI phải xanh lại sau commit tích hợp trước khi merge UI-5.1.
