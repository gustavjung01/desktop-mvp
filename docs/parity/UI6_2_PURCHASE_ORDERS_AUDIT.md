# UI-6.2 — Audit Đơn mua hàng theo Công Ty Web

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@3802cd20b9ac52eb72e47873988f73dbdcf5881c`.
- Không có PR Desktop mở khi bắt đầu.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn: `/purchasing/purchase-orders`.
- Không cần backend, DB hoặc migration mới.

## Nguồn sự thật

Web:
- `app/purchasing/purchase-orders/page.tsx`
- `PurchaseOrderWorkspace.tsx`
- `components/PurchaseOrderList.tsx`
- `components/PurchaseOrderEditorV2.tsx` → V5 → V4
- `lib/purchase-order-types.ts`

Backend:
- `api/src/routes/purchase-orders.js`
- `api/src/services/purchase-order.js`
- `api/src/routes/supplier-purchase-prices.js`

## Endpoint canonical

- `GET /api/purchase-orders` — danh sách.
- `GET /api/purchase-orders/:id` — chi tiết.
- `POST /api/purchase-orders` — tạo nháp.
- `PATCH /api/purchase-orders/:id` — cập nhật nháp với `expectedRevision`.
- `POST /api/purchase-orders/:id/submit` — gửi duyệt.
- `POST /api/purchase-orders/:id/approve` — duyệt và cấp số.
- `POST /api/purchase-orders/:id/cancel` — hủy với lý do.
- `GET /api/purchase-orders/sku-search` — tìm SKU mua hàng hợp lệ.
- `POST /api/supplier-purchase-prices/resolve` — lấy giá mua theo nhà cung cấp/SKU/đơn vị/số lượng/ngày đặt.
- `GET /api/goods-receipts?purchaseOrderId=...` — lịch sử nhận hàng của đơn.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-6.2 |
| --- | --- | --- |
| 1 | Shell Mua hàng / Đơn mua hàng | Shell dùng title/subtitle hiện có, workspace không lặp header |
| 2 | Cập nhật dữ liệu | Nút Cập nhật dữ liệu |
| 3 | Tạo đơn mua hàng | Nút Tạo đơn mua hàng khi có quyền |
| 4 | Tổng đơn / Đơn nháp / Chờ duyệt | 3 KPI gọn, 2 dòng, shared summary style |
| 5 | Tìm kiếm | Tìm số đơn, NCC, kho, SKU/tên hàng |
| 6 | Trạng thái | Bộ lọc 7 trạng thái canonical |
| 7 | Danh sách | STT, số đơn, ngày đặt, NCC, kho, số dòng, tổng giá trị, trạng thái, cập nhật |
| 8 | Xem | Chi tiết header + dòng hàng + tổng tiền + lịch sử phiếu nhận |
| 9 | Sửa | Chỉ draft + quyền update |
| 10 | Gửi duyệt | Chỉ draft + quyền submit |
| 11 | Duyệt | Chỉ pending_approval + approve + price.read |
| 12 | Hủy | draft/pending/approved + lý do |
| 13 | Editor header | NCC, kho nhận, ngày đặt, dự kiến nhận, tham chiếu NCC, ghi chú |
| 14 | Thêm SKU | Tìm nhanh và chọn từ kết quả canonical `sku-search` |
| 15 | Giá mua | Resolve theo NCC + SKU + unit + qty + ngày đặt; không dùng giá bán |
| 16 | Giá thủ công | Chỉ khi có `core.purchase-order.price.override`, bắt buộc lý do |
| 17 | Dòng mua | SKU, số lượng, đơn giá, chiết khấu, thuế, ghi chú |
| 18 | Lưu nháp | POST create / PATCH update với expectedRevision khi edit |
| 19 | Idempotency | Shared `CanonicalIdempotencyKeyProvider`; retry cùng payload reuse key |
| 20 | Optimistic concurrency | update/submit/approve/cancel gửi expectedRevision |
| 21 | Bulk | Web có browse/bulk XLSX; Desktop triển khai sau cùng trong chính UI-6.2, không tách route mới |
| 22 | In đơn | Web có print sheet; Desktop dùng print native từ cùng detail contract |

## Quyền

- `core.purchase-order.read`
- `core.purchase-order.create`
- `core.purchase-order.update`
- `core.purchase-order.submit`
- `core.purchase-order.approve`
- `core.purchase-order.cancel`
- `core.purchase-order.price.read`
- `core.purchase-order.price.override`

Frontend fail-closed; backend vẫn là security boundary.

## Trạng thái

- Nháp
- Chờ duyệt
- Đã duyệt
- Đã nhận một phần
- Đã nhận đủ
- Đã đóng
- Đã hủy

Chỉ `draft` được sửa. Số PO chính thức chỉ được cấp khi duyệt.

## Idempotency bắt buộc

Mọi mutation dùng shared canonical provider. Key chỉ thuộc `[A-Za-z0-9._-]`.
Retry cùng logical attempt và cùng payload phải reuse đúng key cũ. Khi payload thay đổi mới tạo key mới.
Không tự ghép key từ ID, trạng thái, thời gian hoặc text người dùng.
