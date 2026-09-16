# UI-2.7 — Audit Danh mục sản phẩm theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Desktop bắt đầu phần Product từ work dở `work-ui2-products`, nhưng nhánh này được tạo trước PR #44 nên không được merge trực tiếp.
- Mốc bàn giao ban đầu: `main@569f564`.
- Trong lúc hoàn thiện, UI-3.5 merge PR #46; baseline cuối trước commit UI-2.7 là `main@04b592a`.
- UI-2.7 được ghép lại trên baseline mới để giữ nguyên Kiểm kê kho, Điều chỉnh tồn, Shell và App của luồng UI-3.
- Trước khi merge UI-2.7, UI-3.6 PR #47 đã vào `main`; Product được hợp nhất lại trên `main@5bb2b0f` để giữ nguyên Nhập kho thủ công.
- Không sửa backend, database, migration hoặc production deploy.

## Công Ty Web → Desktop

Workspace giữ đúng thứ tự 6 khu vực:

1. Sản phẩm
2. Thiết lập nhanh
3. Cập nhật SP
4. Loại sản phẩm
5. Nhãn hàng
6. Đơn vị và quy đổi

### Sản phẩm

- Lọc tìm kiếm, trạng thái, hiển thị bán hàng và khả năng đặt hàng.
- Bảng mã, ảnh, tên, loại, nhãn hàng, trạng thái.
- Tạo/sửa/ngừng sử dụng sản phẩm.
- Mở quản lý SKU từ từng sản phẩm.

### Thiết lập nhanh

- Tạo/chọn sản phẩm.
- Tạo/chọn SKU và quy cách.
- Chính sách lô/hạn cho SKU tồn chuẩn.
- Đơn vị, hệ số quy đổi và mã vạch.
- Giá bán theo bảng giá hiện có.
- Ảnh sản phẩm.
- Xem tồn hiện tại; không sửa tồn từ màn này.

### Cập nhật SP

- Nhận tệp .xlsx hoặc .csv.
- Cột đầu là SKU; không tạo SKU mới.
- Chọn thuộc tính cập nhật.
- Nhận diện SKU, xem trước thay đổi rồi mới xác nhận cập nhật.

### Loại sản phẩm / Nhãn hàng

- Danh sách, tạo, sửa, trạng thái sử dụng và thông tin hiển thị bán hàng.

### Đơn vị và quy đổi

- Danh mục đơn vị dùng chung.
- Thiết lập theo SKU: đơn vị, hệ số, mô tả quy cách, mã vạch và kiểm tra quy đổi số lượng.

## Contract và idempotency

- Desktop dùng `ICanonicalIdempotencyKeyProvider` dùng chung.
- Retry cùng intent/fingerprint trong cùng phiên reuse đúng canonical key.
- Không tự ghép giá trị header `Idempotency-Key`.
- Upload ảnh dùng contract hiện hành: `PUT /api/products/{id}/image/upload` với WebP và canonical key.
- Fingerprint ảnh gồm SHA-256 của bytes WebP để không reuse key cho nội dung ảnh khác.
- Xóa ảnh dùng canonical DELETE helper.
- Cập nhật hàng loạt dùng operation key backend trả về cho bước apply.

## Runtime boundary

- UI-3.5 Điều chỉnh tồn tiếp tục dùng workspace index 9.
- UI-3.6 Nhập kho thủ công dùng workspace index 10.
- Product dùng workspace index 11.
- Giữ nguyên `StocktakeHost`, `AdjustmentHost`, `ManualInboundHost`, navigation và topbar UI-3.
- Không merge `work-ui2-products` vào main.
