# UI-6.5 — Audit Phiếu trả nhà cung cấp

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@0fad7548977e24068def23c906b537178712d4b7`.
- UI-4.5 đang mở ở PR #69; UI-6.5 được stack tạm trên exact head UI-4.5 để không làm mất code chat song song, sau đó phải retarget về `main`.
- Công Ty Web/backend audit lại: `NPP-Platform/main@21751ced15027071dcbb5b9f0204e49058a9f50a`.
- Web chuẩn: `/purchasing/supplier-returns`; luồng từ chi tiết Phiếu nhận hàng đã ghi sổ sang màn này cũng là chuẩn Web.
- Backend hiện có đủ contract; không cần backend, DB hoặc migration mới.

## Lifecycle

`draft -> pending_approval -> approved -> posted -> reversed`

Nhánh hủy: `draft/pending_approval/approved -> cancelled`.

Chỉ draft được sửa. Post tạo inventory movement OUT. Reverse tạo chứng từ bù.

## Quyền canonical

- `core.supplier-return.read`
- `core.supplier-return.create`
- `core.supplier-return.update`
- `core.supplier-return.submit`
- `core.supplier-return.approve`
- `core.supplier-return.cancel`
- `core.supplier-return.post`
- `core.supplier-return.reverse`

Tám quyền trên là permission canonical của Phiếu trả nhà cung cấp. Riêng thao tác **tạo mới từ danh sách Phiếu nhận hàng đã ghi sổ** còn phụ thuộc `core.goods-receipt.read`, vì Web hiện hành nạp nguồn bằng `listGoodsReceipts(... status: 'posted')` và endpoint Phiếu nhận hàng có quyền đọc riêng. Desktop phải fail-closed: vẫn cho đọc Phiếu trả nếu có quyền đọc Phiếu trả, nhưng khóa tạo và thông báo rõ khi thiếu quyền đọc Phiếu nhận hàng.

## API canonical

- `GET /api/supplier-returns?limit=1000&offset=0`
- `GET /api/supplier-returns/:id`
- `GET /api/supplier-returns/source-lines?goodsReceiptId=:id`
- `POST /api/supplier-returns`
- `PATCH /api/supplier-returns/:id`
- `POST /api/supplier-returns/:id/submit`
- `POST /api/supplier-returns/:id/approve`
- `POST /api/supplier-returns/:id/cancel`
- `POST /api/supplier-returns/:id/post`
- `POST /api/supplier-returns/:id/reverse`

Backend route yêu cầu Idempotency-Key cho mọi mutation. Desktop dùng shared `CanonicalIdempotencyKeyProvider`; retry cùng logical attempt/payload reuse đúng key cũ. Không tự ghép key. Cache retry phải nhận diện đúng payload thật của từng action; field ẩn không thuộc payload không được làm đổi logical attempt.

PATCH và mọi action submit/approve/cancel/post/reverse đều dùng `expectedRevision`.

## Quy tắc nghiệp vụ

- Chỉ tạo từ goods receipt đã `posted`.
- Source line phải có acceptedQuantity > 0.
- `returnableQuantity = acceptedQuantity - postedReturnQuantity`.
- `returnQuantity > 0` và không vượt returnableQuantity.
- Mỗi dòng trả bắt buộc `reasonCode` và `reasonNote`.
- Supplier và warehouse được backend snapshot từ source lines và phải đồng nhất.
- Submit đọc khóa lại source receipt; nếu nguồn không còn posted thì thất bại.
- Post chỉ từ approved và tạo xuất kho `SUPPLIER_RETURN_ISSUE`.
- Reverse chỉ từ posted, bắt buộc ngày đảo và lý do không rỗng.
- Ghi chú khi ghi sổ là tùy chọn; dấu bắt buộc chỉ hiển thị cho Hủy và Đảo.
- Supplier return active ở pending_approval/approved/posted chặn reverse phiếu nhận hàng nguồn.
- Chỉ cấp số series `SUPPLIER_RETURN` lúc post.
- Không mở payable/payment/credit-note UI trong UI-6.5.

## UI parity

- KPI: Tổng phiếu / Nháp / Chờ duyệt / Đã ghi sổ.
- Tìm kiếm và lọc trạng thái.
- Danh sách: số phiếu, NCC, kho, ngày trả, trạng thái, số dòng, tổng SL, thao tác.
- Editor tạo/sửa từ Phiếu nhận hàng đã ghi sổ + source-lines; tạo mới mặc định nạp Phiếu nhận hàng đã ghi sổ mới nhất giống Web.
- Từ chi tiết Phiếu nhận hàng đã ghi sổ có action **Tạo phiếu trả NCC** và chuyển sang editor Phiếu trả với đúng phiếu nguồn.
- Khi đổi Phiếu nhận hàng nguồn liên tiếp, request source-lines cũ phải bị hủy/loại bỏ; response cũ tuyệt đối không được ghi đè nguồn mới.
- Chi tiết dòng nguồn, SKU, lý do, số lượng trả/còn trả.
- Action: Gửi duyệt / Duyệt / Hủy / Ghi sổ / Đảo.
- In native Desktop theo chứng từ.

## Hạ tầng

Không cần backend, DB hoặc migration mới.
