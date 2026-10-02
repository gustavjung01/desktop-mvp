# Web delta 29/09–01/10/2026 — Lô 1 Kho + Bán hàng

## Baseline

- Desktop: `main@f544770df000e08dbb0738d6f0ad1e19e05f1b08`.
- Web authority: `binhnxwjfjxm/NPP-Platform@a5f76e7782e51ee2925f8d05d1e53ca58b40a367`.
- Web deltas audited: PR #1215, #1225, #1226, #1227, #1255.
- Issue Desktop: #95.

## Delta thực sự cần code Desktop

### Tra cứu / Lịch sử kho

Web #1226 bổ sung trực tiếp vào canonical history read-model:
- `customer_code`;
- `customer_name`;
- `sales_order_number`.

Desktop phải đọc ba field additive này, hiển thị Đơn/chứng từ + Khách hàng trong bảng và hiển thị Đơn bán hàng + Khách hàng trong popup chứng từ.

Xuất lịch sử là thao tác riêng với generic export:
- cùng một kho;
- từ 1 đến 20 SKU;
- lấy toàn bộ các trang history canonical, không chỉ 50 dòng đang render;
- mỗi SKU một sheet XLSX;
- cột: ngày, SKU, khách, mã khách, đơn bán, chứng từ, loại chứng từ, thao tác, delta, tồn sau, nhân viên, kho, vị trí, lô.

Không thêm backend/DB vì `GET /api/inventory/balances/history` hiện tại đã đủ dữ liệu.

### Export decimal

Web #1227 chuẩn hóa ở writer dùng chung. Desktop áp dụng cùng nguyên tắc ở `OfficeDataExportFile`:
- chỉ compact chuỗi decimal bằng xử lý chuỗi chính xác;
- không dùng float làm nguồn nghiệp vụ;
- giữ mã/chứng từ như `00123`, `SO-202609-000948`;
- áp dụng cho XLSX và CSV dùng chung.

### In SALES_ORDER

Web #1255 bỏ `required` cho Tên sản phẩm / SKU / Số lượng / ĐVT / Đơn giá / Thành tiền.

Desktop không cần dựng renderer mới:
- `PrintFieldOption` lấy `Required` từ backend;
- `SalesOrderPrintPreview.BuildLines` đã lọc từng cột độc lập bằng `DocumentPrintTemplateRuntime.Shows(template, column.Key)`.

Lô này chỉ khóa regression để không tái hard-code các cột hàng hóa.

### Ra đơn Công Ty

Web #1215 có hai phần:
- channel fixed fallback là business rule backend;
- UI giảm render/search race.

Desktop tiếp tục gọi canonical `ResolvePriceAsync`, không tự tính giá fallback. WPF TextBox Ghi chú dùng default LostFocus binding nên gõ từng ký tự không cập nhật toàn form/không gọi API. Tìm khách chỉ cập nhật search list; cùng customer id không reload vì setter fail-fast.

Gap Desktop thực tế là request địa chỉ cũ có thể về muộn sau khi đổi khách nhanh. Lô này cancel request cũ và chỉ nhận kết quả nếu customer id vẫn đúng.

## Boundary

- Không sửa Web/backend/DB/migration.
- Không deploy production.
- Không tạo pricing rule thứ hai trên Desktop.
- Không thay lifecycle đơn bán.
- Không thay canonical Idempotency-Key hiện hữu.
