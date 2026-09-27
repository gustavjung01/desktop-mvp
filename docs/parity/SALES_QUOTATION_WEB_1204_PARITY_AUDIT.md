# Bán hàng → Báo giá — Desktop parity theo Web live

## Baseline

- Desktop source: `main@40aebaf2e828ffd48197c1d7e1204fd2fd827f1f`, CI `36248467819` success, không có PR mở.
- Web PR #1204 đã merge tại `5bba1a3289932bdfe17e1ec679b7aabf9784d28b`.
- Web live khi làm Desktop: `d0c5a6d27883af0e65238306a7f3dd63b569d271`.
- Commit sau #1204 đổi UX Báo giá: tìm khách trực tiếp, tìm/chọn nhiều SKU theo contract Ra đơn, sửa đơn giá tại dòng mà không ghi ngược bảng giá.

## Delta Desktop trước khi sửa

Desktop đã có engine Báo giá cũ nằm trong tab `Nhập/xuất dữ liệu`: free-text SKU, N+1 variants theo từng product và UI generic. Contract `POST /api/file-operations/quotation` và canonical Idempotency-Key đã tồn tại.

Lô này **di chuyển/refactor**, không tạo engine Báo giá thứ hai:

- bỏ Báo giá khỏi Nhập/xuất dữ liệu;
- Nhập/xuất dữ liệu còn Sản phẩm & SKU, Giá bán, Kiểm kê, Biến động kho, Biểu mẫu văn phòng;
- tạo workspace riêng Bán hàng → Báo giá.

## Contract Web/API khóa

- references: product categories, sales channels, customer groups, customers;
- all/category: `GET /api/products?active=true&limit=1000&offset=...`, bounded đến offset 10000;
- variants: `POST /api/products/variants/query`, batch tối đa 500 product IDs;
- SKU scope: `GET /api/sales-orders/sku-search?search=...&limit=30&offset=0`;
- quote: `POST /api/file-operations/quotation`;
- quote payload giữ `skus, quantity, currencyCode, channelId, customerGroupId, customerId, format=tabular`;
- max 1.000 SKU; quantity > 0 và tối đa 6 số lẻ;
- canonical Idempotency-Key: retry cùng điều kiện sau lỗi reuse key; thay điều kiện reset; thành công reset;
- manual unit price là local quotation override, không mutation pricing;
- export chính kết quả đang hiển thị ra XLSX/CSV với 8 cột Web.

## Permission thực tế

- quote file-operation: `core.product.read + core.price.read`;
- SKU search: `core.sales-order.read`;
- customers/customer-groups: `core.customer.read`.

Desktop deny-by-default và chỉ mở màn khi đủ cả bốn quyền vì màn live cần đủ bốn contract.

## Không thay đổi

Không sửa Web/backend/DB/migration. Không thêm mutation pricing. Không deploy production.
