# UI-4.6 — Audit Đối soát cuối chuyến

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@9faf5fa9a26713beb49489920f5a207fb2ab0e9f`.
- Công Ty Web/backend: `NPP-Platform/main@21751ced15027071dcbb5b9f0204e49058a9f50a`.
- Route Web: `/logistics/trip-reconciliation`.
- Không backend, DB, migration hay deploy trong UI-4.6.

## Ranh giới

UI-4.6 xử lý đúng hàng chưa giao còn trên xe của một chuyến đã xuất phát:
1. chọn chuyến;
2. kiểm tra chênh lệch;
3. kho thực nhận hàng còn trên xe;
4. chỉ khi mọi phiếu có kết quả và hàng trên xe về 0 mới đóng chuyến.

Đây không phải Hàng khách trả UI-4.7. Hàng khách trả là luồng thương mại riêng sau khi khách đã nhận hàng/công nợ đã phát sinh.

## Matrix Web → Desktop

| Web | Desktop |
| --- | --- |
| Kicker `Điều phối giao hàng` | `ĐIỀU PHỐI GIAO HÀNG` |
| Title `Đối soát cuối chuyến` | Giữ đúng |
| Action `Kết quả lần giao` | Topbar + action khi thiếu kết quả |
| 4 bước | Chọn chuyến → Kiểm tra chênh lệch → Nhận hàng trả về → Đóng chuyến |
| Queue | Chỉ `dispatched` / `closed` |
| Detail header | Chuyến + in đối soát + trạng thái |
| Summary | Kho → Tài xế → Xe |
| Việc tiếp theo | Số dòng còn xe + số dòng thiếu kết quả + hướng dẫn |
| Bảng chênh lệch | Phiếu/hàng → Kết quả → Xuất → Đã giao → Đã về → Còn xe |
| Nhận hàng về | Số thực nhận từng dòng → thời điểm → ghi chú → xác nhận |
| Đóng chuyến | Thời điểm → ghi chú → chốt |
| Lịch sử | Thời điểm → mã nhập kho → số dòng/ghi chú |
| Closed | chỉ đọc |
| Keyboard | F5 tải lại |

## Quyền thật

- đọc: `core.delivery-trip.read` + `core.delivery-trip.reconciliation-read`;
- nhận hàng về: `core.delivery-trip.return-receive`;
- đóng chuyến: `core.delivery-trip.close`.

## API

- `GET /api/logistics/trips?status=all`
- `GET /api/logistics/trips/{tripId}/reconciliation`
- `POST /api/logistics/trips/{tripId}/return-receipts`
- `POST /api/logistics/trips/{tripId}/close`

Hai POST bắt buộc Idempotency-Key.

## Idempotency

Desktop chỉ dùng `ICanonicalIdempotencyKeyProvider`.
- nhận hàng: intent gồm chuyến + thời điểm + ghi chú + tập dòng/số lượng đã sort;
- đóng chuyến: intent gồm chuyến + thời điểm + ghi chú;
- retry lỗi giữ nguyên key;
- chỉ xóa key khi backend trả thành công.

## Invariant backend đã đối chiếu

- chỉ chuyến `dispatched` nhận hàng quay về;
- mỗi dòng nhận lại phải có kết quả lần giao;
- không nhận vượt phần còn trên xe;
- nhập kho giữ đúng source lineage/vị trí/lô;
- đóng chuyến chỉ khi mọi assignment có kết quả và outstanding = 0;
- chuyến đóng trở thành `closed`.

## Ngôn ngữ

Desktop dùng ngôn ngữ văn phòng: “nhận hàng về kho”, “hàng còn trên xe”, “đối soát”, “đóng chuyến”. Không hiển thị permission/API/tên bảng/thuật ngữ implementation.
