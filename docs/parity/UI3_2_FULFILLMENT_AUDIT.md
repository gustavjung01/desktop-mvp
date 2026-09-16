# UI-3.2 — Audit Chuẩn bị hàng theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue: #31.
- Desktop bắt đầu UI-3.2 từ `main@344a0946c2ce5ab8b59cece745206b847ffc89f9`, sau đó rebase/chốt trên `main@b54dd78bd23a9ee2b5acfa4a7495617306a439ce` sau UI-2.4.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`.
- Web chuẩn: `npp-core/web/app/inventory/fulfillment/fulfillment-workspace.tsx`.
- Backend chuẩn: `npp-core/api/src/routes/fulfillment-operations.js`.
- Desktop trước UI-3.2 chưa có màn Chuẩn bị hàng; submenu còn bị vô hiệu hóa.
- UI-2.4 đã được giữ nguyên khi rebase: không khôi phục menu Vị trí kho độc lập và giữ chỉnh canh giữa row/nút.
- Không sửa backend, không migration, không deploy.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.2 |
| --- | --- | --- |
| 1 | Thanh tổng hợp đơn sau lọc, giá trị, chờ phân bổ, đang xử lý, đóng gói + Làm mới | Giữ nguyên hierarchy và nội dung |
| 2 | Cột trái “Đơn cần chuẩn bị” | Master list bên trái, rộng cố định theo layout Desktop |
| 3 | Tìm đơn, khách, SKU | Có Ctrl+F để focus thêm, không đổi flow |
| 4 | Kênh bán → Trạng thái → Kho → Xóa lọc | Đúng thứ tự |
| 5 | Danh sách đơn: STT, số đơn, trạng thái, khách, tổng tiền, kênh | Đủ |
| 6 | Header đơn: đơn bán hàng, khách, kênh, kho, ngày giao, tổng tiền, tạm tính/CK/thuế | Đủ |
| 7 | Phân bổ toàn đơn | Mutation thật |
| 8 | Sản phẩm trong đơn | Đủ 10 cột như Web |
| 9 | Phân bổ theo số lượng / phân bổ đủ | Mutation thật, validate số lượng |
| 10 | Đơn khác giữ | Drill-down hold, loại chính đơn hiện tại |
| 11 | Chi tiết sản phẩm + 6 chỉ số | Đủ |
| 12 | Vị trí có thể lấy | Tối đa 12 vị trí như Web |
| 13 | Hàng đã phân bổ | Phân bổ / soạn / đóng gói / trạng thái / thao tác |
| 14 | Soạn / Đóng gói | Mutation thật, đúng permission |
| 15 | Loading / empty / error / notice / disabled | Đủ; action mutation khóa khi đang xử lý |

Không có local tab phụ ở màn Chuẩn bị hàng.

## Contract backend

### Đọc
- `GET /api/inventory/fulfillment-work?limit=500`
- `GET /api/inventory/fulfillment-demands/{demandId}/suggestions`
- `GET /api/inventory/holds?warehouseId=...&baseVariantId=...&excludeSalesOrderId=...`

Quyền đọc màn:
- `core.fulfillment.read` hoặc `core.fulfillment.pick`.

### Mutation
- `POST /api/inventory/fulfillment-orders/{salesOrderId}/allocate` — `core.fulfillment.allocate`
- `POST /api/inventory/fulfillment-demands/{demandId}/allocate` — `core.fulfillment.allocate`
- `POST /api/inventory/fulfillment-allocations/{allocationId}/pick` — `core.fulfillment.pick`
- `POST /api/inventory/fulfillment-allocations/{allocationId}/pack` — `core.fulfillment.pack`

Desktop dùng `ICanonicalIdempotencyKeyProvider` có sẵn. Key do shared provider sinh theo contract `[A-Za-z0-9._-]`; cùng một intent/fingerprint trong phiên thao tác reuse đúng key cũ. Không tự ghép idempotency key từ dữ liệu nghiệp vụ.

## Keyboard Desktop

- `F5`: làm mới hàng đợi.
- `Ctrl+F`: focus ô tìm đơn.
- `Enter` trên dòng sản phẩm: mở/refresh chi tiết sản phẩm.
- `Esc`: đóng popup đơn khác đang giữ hàng.

Keyboard là tăng tốc native Desktop, không thay đổi workflow Web.

## Chỉnh nhẹ UI-3.1 theo yêu cầu

Sáu thẻ tổng quan Báo cáo tồn kho được rút từ 3 dòng xuống 2 dòng:
- giữ tên thẻ;
- giữ số liệu;
- câu giải thích chuyển sang tooltip;
- giảm margin dọc.

Không thay contract hoặc nội dung nghiệp vụ. Chiều cao tiết kiệm được trả lại cho vùng tab/bảng chính bên dưới.

## Boundary

- Không làm UI-3.3 Chuyển kho.
- Không thêm action backend ngoài action đang hiện trên Công Ty Web.
- Không sửa DB/migration.
- Chỉ sửa shared Shell ở mức cần thiết để bật đúng submenu Chuẩn bị hàng và host màn thật.
