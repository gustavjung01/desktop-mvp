# UI-4.5 — Audit Kết quả lần giao

Ngày audit: 2026-09-16

## Baseline thực tế

- Desktop: `main@0fad7548977e24068def23c906b537178712d4b7`.
- Desktop CI baseline: run #428 — PASS.
- UI-4.4 đã merge ở PR #67; sau đó UI-6.4 PR #68 đã merge.
- Khi bắt đầu UI-4.5 không có PR mở.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Route Web chuẩn: `/logistics/delivery-attempts`.
- Không backend/DB/migration/deploy trong UI-4.5.

## Ranh giới nghiệp vụ

UI-4.5 là màn **điều phối chỉ đọc**.

Desktop:
- chỉ đọc các chuyến đã xuất phát;
- đọc kết quả lần giao do tài xế đã ghi;
- đọc bằng chứng giao hàng tùy chọn khi có quyền;
- không ghi thay tài xế;
- không tự nhập hàng về kho;
- không đối soát/đóng chuyến.

Không có mutation và không cần Idempotency-Key trong màn này.

## Matrix Web → Desktop

| Thứ tự | Web chuẩn | Desktop UI-4.5 |
| --- | --- | --- |
| 1 | Kicker `Điều phối giao hàng` | Shell `ĐIỀU PHỐI GIAO HÀNG` |
| 2 | Title `Theo dõi kết quả lần giao` | Giữ đúng |
| 3 | Action `Bàn giao chuyến` | Topbar về UI-4.4 |
| 4 | Hai cột | Chuyến đã xuất phát trái → kết quả phải |
| 5 | Danh sách | Chỉ trip `dispatched` |
| 6 | Card chuyến | Số chuyến → kho/tài xế → xe/số phiếu |
| 7 | Header chi tiết | Chuyến → số kết quả / tổng phiếu |
| 8 | Kết quả | Điểm → phiếu → khách → kết quả |
| 9 | Metadata | Thời điểm → lý do → giao lại → ghi chú |
| 10 | Kết quả chuẩn | Giao đủ / Giao một phần / Không giao được / Hẹn giao lại |
| 11 | Bằng chứng | Mở/ẩn theo từng kết quả |
| 12 | Loại bằng chứng | Ảnh / chữ ký / mã xác nhận / xác nhận thủ công |
| 13 | Ảnh | Mở liên kết tải tạm thời nếu backend cấp |
| 14 | Không có bằng chứng | Kết quả vẫn hợp lệ |
| 15 | Empty/loading/error/disabled | Có đủ; F5 tải lại |
| 16 | Hàng chưa giao đủ | Vẫn là hàng đang theo xe; không tự nhập kho |

## Quyền backend thật

Mở màn:
- `core.delivery-trip.read`
- `core.delivery-attempt.read`

Xem bằng chứng:
- `core.delivery-trip.read`
- `core.pod.read`

Desktop không dùng:
- `core.delivery-attempt.record`
- `core.pod.attach`

## API contract

### Danh sách chuyến
- `GET /api/logistics/trips?status=all`
- Desktop lọc `status == dispatched`.

### Kết quả lần giao
- `GET /api/logistics/trips/{tripId}/attempts`
- Trả:
  - trip summary
  - danh sách attempts

Attempt fields dùng trên UI:
- stopSequence
- deliveryOrderNumber
- customerCode/customerName
- result
- attemptedAt
- reasonCode
- note
- rescheduledFor

### Bằng chứng
- `GET /api/logistics/trips/{tripId}/attempts/{attemptId}/pod`
- Chỉ GET trong Desktop.
- Ảnh có thể có `downloadUrl` tạm thời; nếu storage không cấp URL thì chỉ hiện thông báo ảnh đã lưu.

## Ngôn ngữ văn phòng

Web còn dùng một số cụm kỹ thuật trong notice. Desktop thay bằng:
- `read-only` → `chỉ đọc`;
- `dispatch lineage` → `hàng đang theo xe`;
- `Inventory IN` → `nhập hàng về kho`;
- `POD` → `bằng chứng giao hàng`.

Không hiển thị permission key, API path hoặc thuật ngữ implementation trên UI.
