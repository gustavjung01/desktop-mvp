# UI-4.3 — Audit Lập và xếp chuyến

Ngày audit: 2026-09-16

## Baseline thực tế

- Desktop: `main@17c4c20d42d620a4589f62a74fb7062b62d3cd39`.
- Desktop CI trên baseline: run #405 — PASS.
- Khi bắt đầu có PR #65 thuộc UI-6.3 chạy song song; UI-4.3 làm trên branch riêng `agent/ui4-trip-planning`.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Route Web chuẩn: `/logistics/trips`.
- Backend chuẩn: `/api/logistics/**`.
- Không có thay đổi database, migration hay deploy trong UI-4.3.

## Ranh giới nghiệp vụ

UI-4.3 chỉ **lập và xếp chuyến**. Màn này không xuất kho, không cho xe xuất phát, không ghi kết quả giao và không đối soát cuối chuyến. Các bước đó thuộc UI-4.4 trở đi.

## Matrix Web → Desktop

| Thứ tự | Web chuẩn | Desktop UI-4.3 |
| --- | --- | --- |
| 1 | Kicker `Giao nhận` | Shell: `GIAO NHẬN` |
| 2 | Title `Điều phối giao hàng` | Giữ đúng |
| 3 | Tab `Lập chuyến` | Giữ đúng |
| 4 | Tab `Gán chuyến` | Giữ đúng |
| 5 | Tab con `Tạo & danh sách` | Giữ đúng |
| 6 | Tab con `Chi tiết chuyến` | Giữ đúng |
| 7 | Danh mục điều phối | Tuyến giao → Phương tiện → Tài xế |
| 8 | Tạo tuyến | Mã tuyến → Tên tuyến → Mô tả → Kho áp dụng |
| 9 | Tạo xe | Mã xe → Biển số → Loại xe |
| 10 | Tạo tài xế | Chọn nhân sự thật → thông tin bằng lái; mã/tên/SĐT lấy từ hồ sơ nhân sự |
| 11 | Tạo chuyến | Kho → Tuyến → Xe → Tài xế → Giờ dự kiến → Ghi chú |
| 12 | Danh sách chuyến | Số chuyến → tuyến/kho → trạng thái/điểm/phiếu |
| 13 | Chi tiết chuyến nháp | Lưu kế hoạch; xếp điểm; bỏ phiếu; chuyển sang đã lập kế hoạch |
| 14 | Chuyến đã lập kế hoạch | Chỉ đọc; có Mở lại chỉnh sửa và Khóa kế hoạch |
| 15 | Chuyến đã khóa | Chỉ đọc; không sửa xe/tài xế/điểm/phiếu |
| 16 | Gán chuyến | Tuyến → Chuyến nháp → nhiều phiếu cùng kho |
| 17 | Phiếu thiếu số | Hiện `Thiếu mã phiếu giao`, không cho tích |
| 18 | Empty/loading/error/disabled | Có trạng thái riêng; F5 tải lại |
| 19 | Mutation retry | Cùng intent/payload reuse cùng key; key chỉ xóa sau thành công |

## Quyền backend thật

- `core.logistics-route.read`
- `core.logistics-route.manage`
- `core.vehicle.read`
- `core.vehicle.manage`
- `core.driver-profile.read`
- `core.driver-profile.manage`
- `core.delivery-trip.read`
- `core.delivery-trip.create`
- `core.delivery-trip.plan`
- `core.delivery-trip.assign`
- `core.delivery-trip.lock`

UI-4.3 không dùng `core.delivery-trip.dispatch`.

## API contract

### Read
- `GET /api/logistics/warehouses`
- `GET /api/logistics/routes?active=true`
- `GET /api/logistics/vehicles?active=true`
- `GET /api/logistics/drivers?active=true`
- `GET /api/logistics/driver-employees?limit=1000`
- `GET /api/logistics/eligible-delivery-orders`
- `GET /api/logistics/trips`
- `GET /api/logistics/trips/{tripId}`

### Mutation
- `POST /api/logistics/routes`
- `POST /api/logistics/vehicles`
- `POST /api/logistics/drivers`
- `POST /api/logistics/trips`
- `PUT /api/logistics/trips/{tripId}`
- `POST /api/logistics/trips/{tripId}/assign`
- `POST /api/logistics/trips/{tripId}/unassign`
- `POST /api/logistics/trips/{tripId}/reorder`
- `POST /api/logistics/trips/{tripId}/plan`
- `POST /api/logistics/trips/{tripId}/reopen`
- `POST /api/logistics/trips/{tripId}/lock`

## Idempotency

Desktop dùng `ICanonicalIdempotencyKeyProvider` cho toàn bộ mutation. Intent cache gồm thao tác + đối tượng + revision/payload fingerprint. Retry sau lỗi không xóa intent key; chỉ xóa sau khi backend trả thành công. Batch gán sort ID trước khi tạo intent để cùng tập phiếu luôn tái sử dụng đúng key.

## Ngôn ngữ

UI chỉ hiển thị ngôn ngữ văn phòng. Không hiển thị UUID, permission key, API path, tên bảng, thuật ngữ canonical hay tên implementation.
