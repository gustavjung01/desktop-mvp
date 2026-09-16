# UI-4.7 — Audit Hàng khách trả

Ngày audit: 2026-09-16

## Baseline

- UI-4.7 được làm stacked trên UI-4.6 exact-head `103c08909d4bec2c438eacba36377292c1632252`.
- UI-4.6 PR #71 exact-head CI #448/#449 PASS trước khi bắt đầu commit UI-4.7.
- Desktop main tại thời điểm bắt đầu UI-4.6: `9faf5fa9a26713beb49489920f5a207fb2ab0e9f`.
- Công Ty Web/backend: `NPP-Platform/main@21751ced15027071dcbb5b9f0204e49058a9f50a`.
- Route Web: `/inventory/customer-returns`.
- Không backend, DB, migration hay deploy trong UI-4.7.

## Ranh giới nghiệp vụ

Hàng khách trả là luồng thương mại cho hàng đã xuất và đã phát sinh công nợ.

- Tạo phiếu trả nháp không làm tăng tồn.
- Chỉ khi kho xác nhận thực nhận mới ghi nhập kho.
- Có thể hủy phiếu nháp.
- Nếu nguồn chưa phát sinh công nợ vì khách chưa thực nhận hàng, không dùng Hàng khách trả; phải xử lý tại Đối soát cuối chuyến UI-4.6.

## Matrix Web → Desktop

| Web | Desktop |
| --- | --- |
| Kicker `Kho và bán hàng` | `KHO VÀ BÁN HÀNG` |
| Title `Hàng khách trả` | Giữ đúng |
| Subtitle | Lập phiếu từ đúng dòng đã xuất; chỉ xác nhận thực nhận mới tăng tồn kho |
| Action `Bàn giao giao nhận` | Topbar về UI-4.2 |
| Hero | Nguồn trả bất biến → Nhận hàng khách trả có đối chiếu |
| Tổng hợp | Dòng còn có thể trả → Phiếu nháp → Đã nhận kho → Đã hủy |
| Tab 1 | `Lập phiếu trả` |
| Tab 2 | `Nhận & xử lý` |
| Tab lập phiếu | queue dòng đã xuất trái → builder phải |
| Builder | Số lượng → Mã lý do → Lý do chi tiết → Ghi chú → Tạo phiếu nháp |
| Lý do | Hư hỏng/không nhận → Sai hàng → Khiếu nại chất lượng → Khác |
| Tab xử lý | queue phiếu trái → detail phải |
| Phiếu nháp | thực nhận từng dòng → xác nhận nhập kho hoặc hủy |
| Phiếu đã nhận | chỉ đọc + mã nhập kho |
| Phiếu đã hủy | chỉ đọc + lý do hủy |
| Lỗi nguồn chưa có công nợ | action `Mở Đối soát cuối chuyến` |
| Keyboard | F5 làm mới |

## API backend thật

Desktop gọi trực tiếp backend, không gọi proxy route của Web:

- `GET /api/delivery-orders/customer-returns/eligibility?limit=1000`
- `GET /api/delivery-orders/customer-returns?limit=500`
- `GET /api/delivery-orders/customer-returns/{id}`
- `POST /api/delivery-orders/customer-returns`
- `POST /api/delivery-orders/customer-returns/{id}/receive`
- `POST /api/delivery-orders/customer-returns/{id}/cancel`

## Quyền

- `core.customer-return.read`
- `core.customer-return.create`
- `core.customer-return.receive`
- `core.customer-return.cancel`

## Invariants backend

### Tạo phiếu
- nguồn là dòng đã ghi xuất kho;
- phiếu giao ở trạng thái phù hợp;
- đã phát sinh công nợ;
- một phiếu trả chỉ thuộc một khách hàng và một kho;
- số lượng không vượt phần còn có thể trả.

### Nhận kho
- chỉ phiếu `draft`;
- gửi `expectedRevision`;
- mỗi số thực nhận từ 0 đến số yêu cầu;
- ít nhất một dòng thực nhận > 0;
- backend giữ đúng source lineage/vị trí/lô và ghi nhập kho.

### Hủy
- chỉ phiếu `draft`;
- bắt buộc lý do.

## Idempotency

Toàn bộ mutation dùng `ICanonicalIdempotencyKeyProvider`.

- create: nguồn + số lượng + mã/lý do + ghi chú;
- receive: phiếu + revision + ngày chứng từ + tập dòng/số thực nhận đã sort;
- cancel: phiếu + revision + lý do.

Retry cùng intent/payload giữ key cũ. Chỉ xóa key sau thành công.

## Ngôn ngữ

Desktop không dùng `Delivery Order`, `Inventory IN`, `movement line` trên UI. Dùng: phiếu giao, nhập kho, dòng hàng đã xuất, mã nhập kho.
