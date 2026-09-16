# UI-4.2 — Audit Phiếu giao hàng

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@3802cd20b9ac52eb72e47873988f73dbdcf5881c`; Desktop CI #383 PASS.
- Không có PR Desktop mở khi bắt đầu.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Route chuẩn: `/inventory/delivery-orders`.
- Web title: **Bàn giao giao nhận**; sidebar label: **Phiếu giao hàng**.
- Backend: `/api/delivery-orders`.
- Không có DB/migration/deploy trong UI-4.2.

## Matrix Web → Desktop

| Thứ tự | Web | Desktop |
| --- | --- | --- |
| 1 | Kicker Kho và giao nhận | Shell giữ đúng |
| 2 | Bàn giao giao nhận | Shell title giữ đúng |
| 3 | Hero Hàng sẵn sàng lập chứng từ | Giữ đúng thứ tự |
| 4 | Hàng khách trả / Làm mới | Giữ vị trí; Hàng khách trả disabled tới UI-4.7, Làm mới hoạt động |
| 5 | 4 tổng hợp | Đơn còn đóng gói / Chứng từ nháp / Sẵn sàng bàn giao / Đã xuất vật lý |
| 6 | Lập chứng từ | Queue nhóm theo đơn + kho; nhập số lượng từng allocation; tạo nháp |
| 7 | Theo dõi & xử lý | Queue chứng từ + detail + lifecycle actions |
| 8 | Xác nhận | Chỉ draft + permission confirm |
| 9 | Hủy nháp | Lý do bắt buộc + permission cancel |
| 10 | Nhận tại quầy | Người nhận + ghi chú + pickup-handover |
| 11 | Giao thủ công | Người nhận + ghi chú + manual-handover |
| 12 | Đảo xuất kho | Lý do bắt buộc + reverse-inventory-issue |
| 13 | In | Phiếu giao hàng / Phiếu đóng gói trên detail đã có số |
| 14 | F5 | Làm mới cùng workflow hiện tại |
| 15 | Empty/error/disabled | Có trạng thái riêng, không tạo action giả |

## Permissions

- read: `core.delivery-order.read`
- create: `core.delivery-order.create`
- confirm: `core.delivery-order.confirm`
- cancel: `core.delivery-order.cancel`
- pickup handover: `core.delivery-order.pickup-handover`
- manual handover: `core.delivery-order.manual-handover`
- reverse issue: `core.delivery-order.reverse-inventory-issue`

## Idempotency

Mọi mutation dùng `ICanonicalIdempotencyKeyProvider`; key chỉ chứa ký tự hợp lệ. ViewModel cache key theo intent + payload fingerprint, vì vậy retry cùng thao tác và cùng payload reuse đúng key. Pickup/manual giữ nguyên `handedOverAt` khi retry sau lỗi.

## Ngôn ngữ

UI không hiển thị UUID, permission key, API/table name hay từ kỹ thuật “canonical/source”. “Delivery Order” trên màn làm việc được đổi thành **Phiếu giao hàng**; trạng thái và action dùng tiếng Việt văn phòng.
