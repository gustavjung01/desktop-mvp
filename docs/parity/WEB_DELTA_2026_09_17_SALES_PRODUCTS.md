# Web delta 17/09/2026 — Báo cáo bán hàng và Danh mục sản phẩm

Nguồn chuẩn đã audit trước khi sửa Desktop:

- Công Ty source: `binhnxwjfjxm/NPP-Platform@41f3ee34ac4882bf5d8294f6dcacbb284b0113aa`
- Báo cáo bán hàng:
  - `npp-core/web/app/components/sales-reporting-export-dialog.tsx`
  - `npp-core/web/lib/sales-reporting-export-gateway.ts`
  - `npp-core/web/test/sales-reporting-export-contract.test.mjs`
  - thay đổi chính ngày 17/09: các commit `41f2274`, `f27877d`, `de57745`
- Danh mục sản phẩm:
  - `npp-core/web/app/products/product-workspace.tsx`
  - `npp-core/web/app/products/product-bulk-update-workspace.tsx`
  - `npp-core/web/app/products/product-import-workspace.tsx`
  - thay đổi chính ngày 17/09: commit `533e212`

## 1. Báo cáo bán hàng — delta xuất file

Chế độ Danh sách giữ nguyên Excel/CSV và chọn cột theo chiều báo cáo.

Chế độ Phân tích mới:

- chỉ xuất Excel;
- chọn đúng hai tiêu chí trong Sản phẩm / Loại khách / Kênh bán / Nhóm hàng;
- chọn Doanh thu, Sản lượng hoặc cả hai;
- Sản lượng hỗ trợ Theo ĐVT bán / Ưu tiên Thùng / Ưu tiên ĐVT lẻ;
- sắp xếp Tên A → Z, doanh thu cao/thấp, sản lượng cao/thấp theo số liệu đã chọn;
- cột xuất được dựng từ report server hiện tại và người dùng có thể bỏ từng cột;
- ĐVT nằm trong cùng một sheet, không tách sheet theo đơn vị tính.

Desktop gọi trực tiếp backend canonical `GET /api/reporting/sales-export`.
Dimension phân tích có dạng `analysis.<row>.<column>.<revenue|quantity|both>`.
Query giữ bộ lọc đã áp dụng: from, to, warehouseId, productGroupId, customerGroupId; thêm quantityDisplay, sort và các column.

## 2. Danh mục sản phẩm — delta file workspace

Web vẫn giữ tab ngoài `Cập nhật SP`, nhưng nội dung tab đã thành `Nhập / cập nhật sản phẩm` với hai tab con:

- Nhập sản phẩm
- Cập nhật sản phẩm

Nhập sản phẩm tái sử dụng contract file-operation hiện có, gồm:

- chọn Excel/CSV;
- tải mẫu Excel/CSV;
- xuất Excel/CSV và chọn cột;
- xem trước trước khi xác nhận nhập;
- giữ canonical Idempotency-Key cho cùng intent retry.

Cập nhật theo SKU mở rộng mapping từ chỉ khối lượng sang thuộc tính cấp sản phẩm/SKU/đơn vị. Các mapping canonical được Desktop đưa đủ theo Web, gồm tên/mô tả/phân loại/nhãn hàng/trạng thái sản phẩm, thuộc tính SKU, đơn vị và quy đổi, mua hàng, định lượng nguồn và khối lượng.

Giới hạn canonical: tối đa 5.000 dòng. Dòng lỗi bị bỏ qua; dòng hợp lệ tiếp tục. Một thuộc tính chỉ được map vào một cột.

## Boundary

- Không sửa backend, database hoặc migration.
- Không thay route production.
- Không deploy production trong task này.
