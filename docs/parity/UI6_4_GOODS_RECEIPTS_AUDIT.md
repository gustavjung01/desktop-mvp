# UI-6.4 — Audit Phiếu nhận hàng

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@4452f42e5fafd1bbcd7f3b6ad4e6477a351acab4`.
- Không có PR Desktop mở khi bắt đầu.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn: `/purchasing/goods-receipts`.
- Backend hiện có đủ contract; không cần backend, DB hoặc migration mới.

## Nguồn sự thật

- Web: `GoodsReceiptWorkspace.tsx`
- Types: `goods-receipt-types.ts`
- Backend: `routes/goods-receipts.js`, `services/goods-receipt-core.js`, `services/goods-receipt-tracking.js`
- Quyết định: `phase-5-2-partial-goods-receipt-decisions.md`, `phase-5-3-goods-receipt-variance-decisions.md`

## Matrix Web → Desktop

| Web | Desktop UI-6.4 |
| --- | --- |
| Tổng phiếu / Nháp / Đã ghi sổ / Đã đảo | 4 KPI gọn |
| Tìm kiếm | Số phiếu, PO, NCC, kho, SKU/lô |
| Lọc trạng thái | all/draft/posted/reversed |
| Danh sách | STT, số phiếu, PO, kho, ngày, trạng thái, số dòng, tổng SL, thao tác |
| Tạo phiếu | Chọn PO đủ điều kiện rồi tải chi tiết + tracking requirement |
| Sửa nháp | Tải receipt + PO canonical |
| Variance | accepted / rejected / chốt thiếu + reason/note theo quyền |
| Tracking | vị trí / lô / NSX / HSD theo policy |
| Ghi sổ | expectedRevision + idempotency |
| Đảo phiếu | expectedRevision + ngày đảo + lý do + idempotency |
| Chi tiết | Header, dòng nhận, totals, print |

## Quyền canonical

- `core.goods-receipt.read`
- `core.goods-receipt.create`
- `core.goods-receipt.update`
- `core.goods-receipt.post`
- `core.goods-receipt.reverse`
- `core.goods-receipt.variance`
- Tạo phiếu còn cần đọc PO: `core.purchase-order.read`.

## API canonical

- `GET /api/goods-receipts?limit=1000&offset=0`
- `GET /api/goods-receipts/:id`
- `GET /api/goods-receipts/tracking-requirements?purchaseOrderId=...`
- `POST /api/goods-receipts`
- `PATCH /api/goods-receipts/:id`
- `POST /api/goods-receipts/:id/post`
- `POST /api/goods-receipts/:id/reverse`
- Reuse `GET /api/purchase-orders`, `GET /api/purchase-orders/:id`, organization warehouse locations.

Backend route yêu cầu idempotency cho create/update/post/reverse. Desktop dùng shared `CanonicalIdempotencyKeyProvider`; retry cùng logical attempt/payload reuse đúng key cũ. Không tự ghép key.

Update/post/reverse đều dùng `expectedRevision` để khóa optimistic concurrency.

## Quy tắc nghiệp vụ khóa

- Chỉ PO `approved` hoặc `partially_received` được nhận.
- Một receipt thuộc một PO và kho của PO.
- received = accepted + rejected.
- Accepted không vượt remaining.
- Rejected không giảm remaining và không vào tồn.
- Chốt thiếu đóng phần remaining còn lại, không tạo inventory.
- Rejected > 0 hoặc chốt thiếu bắt buộc reason code + reason note khi có quyền variance.
- Accepted mới post vào inventory.
- Draft sửa được; posted/reversed chỉ đọc nội dung.
- Post cấp số `PURCHASE_RECEIPT`; draft chưa có số.
- Reverse chỉ cho receipt posted và có thể bị chặn nếu đã có supplier return.
- Tracking policy phải được kiểm trước khi post: vị trí/lô/HSD theo policy.
- Phiếu trả NCC thuộc UI-6.5; UI-6.4 không kích hoạt workflow chưa được triển khai.

## Hạ tầng

Không cần backend, DB hoặc migration mới.
