# UI-4.4 — Audit Bàn giao và xuất phát

Ngày audit: 2026-09-16

## Baseline thực tế

- Desktop: `main@4452f42e5fafd1bbcd7f3b6ad4e6477a351acab4`.
- Desktop CI baseline: run #417 — PASS.
- PR #66 UI-4.3 đã merge; sau đó PR #65 UI-6.3 đã merge. Khi bắt đầu UI-4.4 không còn PR mở.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Route Web chuẩn: `/logistics/dispatch`.
- Không có thay đổi database, migration hay deploy trong UI-4.4.

## Ranh giới nghiệp vụ

UI-4.4 chỉ xác nhận bàn giao vật lý và cho xe xuất phát. Một lần xác nhận ghi xuất kho cho toàn bộ phiếu của chuyến trong cùng giao dịch backend.

Không ghi:
- giao thành công/thất bại;
- bằng chứng giao hàng;
- GPS;
- COD;
- đối soát cuối chuyến.

Các nghiệp vụ đó thuộc UI-4.5 trở đi.

## Matrix Web → Desktop

| Thứ tự | Web chuẩn | Desktop UI-4.4 |
| --- | --- | --- |
| 1 | Kicker `Giao nhận` | Shell: `GIAO NHẬN` |
| 2 | Title `Bàn giao và cho xe xuất phát` | Giữ đúng |
| 3 | Action `Quay lại lập kế hoạch` | Topbar Desktop, về UI-4.3 |
| 4 | Hai cột | Hàng đợi trái → Chi tiết bàn giao phải |
| 5 | Hàng đợi | Chỉ chuyến `locked` / `dispatched` |
| 6 | Card chuyến | Số chuyến → kho/trạng thái → điểm/phiếu → xe/tài xế |
| 7 | Tóm tắt | Kho → Xe → Tài xế chính → Khối lượng việc |
| 8 | In | In phiếu chuyến giao hàng native Desktop |
| 9 | Người nhận mặc định | Tài xế chính đã chốt |
| 10 | Người nhận khác | Chỉ hiện ô nhập khi chọn `Người nhận khác` |
| 11 | Thời điểm | `yyyy-MM-dd HH:mm`, gửi ISO timestamp |
| 12 | Ghi chú | tối đa 2.000 ký tự |
| 13 | Checklist | khóa kế hoạch → xuất toàn bộ phiếu → lỗi một phiếu thì toàn chuyến không đổi |
| 14 | Mutation | `Bàn giao và cho xe xuất phát` |
| 15 | Sau thành công | read-only: Xuất phát → Người nhận → Mã bàn giao → Ghi chú |
| 16 | Điểm giao | Điểm → phiếu giao → khách hàng |
| 17 | Ghi xuất kho | Phiếu → khách → mã ghi xuất kho |
| 18 | Empty/loading/error/disabled | Có đủ; F5 tải lại |
| 19 | Retry | cùng chuyến + thời điểm + người nhận + ghi chú reuse đúng key |

## Ngôn ngữ văn phòng

Web chuẩn còn một số chữ kỹ thuật. Desktop giữ nghiệp vụ nhưng đổi chữ hiển thị:
- `Inventory OUT` → `ghi xuất kho`;
- `dispatch` → `bàn giao`;
- `POD` → `bằng chứng giao hàng`.

Không hiển thị API path, permission key hay tên implementation trên UI.

## Quyền backend thật

- GET danh sách / chi tiết: `core.delivery-trip.read`.
- POST bàn giao: `core.delivery-trip.dispatch`.
- Backend tự kiểm soát capability ghi xuất kho của phiếu và thực hiện transaction toàn chuyến.

## API contract

### Read
- `GET /api/logistics/trips?status=all`
- `GET /api/logistics/trips/{tripId}/dispatch`

### Mutation
- `POST /api/logistics/trips/{tripId}/dispatch`
- Header: `Idempotency-Key` bắt buộc.
- Payload:
  - `dispatchedAt`
  - `handoverReceiverName`
  - `handoverNote`

## Backend invariants đã đối chiếu

- Chuyến phải `locked`.
- Xe phải active và `AVAILABLE`.
- Tài xế chính phải active.
- Phải có ít nhất một phiếu.
- Mọi phiếu phải còn `ready_to_dispatch`, đúng chế độ giao tận nơi và đúng kho chuyến.
- Ghi xuất kho toàn chuyến trong transaction; lỗi một phiếu rollback toàn bộ.
- Replay cùng Idempotency-Key + payload trả lại kết quả cũ.
- Sau thành công chuyến chuyển `dispatched`.

## Idempotency Desktop

Desktop dùng `ICanonicalIdempotencyKeyProvider`. Intent gồm:
`tripId + dispatchedAt + handoverReceiverName + handoverNote`.

Retry sau lỗi giữ nguyên key. Key chỉ bị xóa sau khi backend trả thành công. Không tự ghép Idempotency-Key và không dùng ký tự ngoài contract canonical.
