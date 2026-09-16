# UI-3.3 — Audit Chuyển kho theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu UI-3.3 trên `main@5e42da430edd16aa87cadfa8e1c56f8cd2b39340`, sau UI-2.5 và fix quyền UI-3.2.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@ccd0458b46ca8f3eaf1b9af9582da737d83ffd78` ngày 2026-09-15.
- Web chuẩn: `npp-core/web/app/inventory/transfers/transfer-workspace.tsx`.
- Các commit NPP từ checkpoint UI-3.2 `3dafbaa` đến `ccd0458` không thay đổi workspace/route/permission Chuyển kho.
- Backend chuẩn:
  - `npp-core/api/src/routes/inventory-transfers.js`
  - `npp-core/api/src/routes/inventory-transfer-receipts.js`
- Desktop trước UI-3.3 chưa có màn Chuyển kho; submenu còn bị vô hiệu hóa.
- Không sửa backend, DB, migration hoặc production deploy.

## Matrix Công Ty Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.3 |
| --- | --- | --- |
| 1 | Header Chuyển kho + Làm mới + Tạo phiếu chuyển kho | Đúng hierarchy; action ở topbar Desktop |
| 2 | 4 thẻ: Phiếu nháp, Chờ xuất kho, Đã xuất chuyển, Dòng đang đi đường | Đủ |
| 3 | Phiếu chuyển kho / Hàng đang đi đường | Segmented native, không tạo tab nghiệp vụ giả |
| 4 | Tìm kiếm + Trạng thái chứng từ + Đặt lại | Đúng thứ tự; trạng thái chỉ hiện ở chế độ Phiếu |
| 5 | Danh sách phiếu / dòng đang đi đường dạng card | Đủ STT, trạng thái, ngày, kho, lượng, lô |
| 6 | Tạo phiếu: ngày, kho xuất, kho nhận, ghi chú | Đủ |
| 7 | Hàng chuyển: tồn thực tế, số lượng, khả dụng, thêm/xóa dòng | Đủ, chặn vượt khả dụng |
| 8 | Chi tiết phiếu + In phiếu | Đủ; Desktop dùng PrintDialog native |
| 9 | Nháp: hủy / duyệt | Đúng permission và revision |
| 10 | Đã duyệt: hủy / xuất chuyển | Đúng permission và revision |
| 11 | Nhận hàng tại kho đích | Đủ resolution của hàng đang đi đường |
| 12 | Lần nhận: vị trí, nhận đạt, hư hỏng, thừa, ghi chú | Đủ |
| 13 | Lịch sử nhận | Đủ trạng thái, dòng nhận và bằng chứng xử lý |
| 14 | Duyệt hư hỏng | Mutation thật |
| 15 | Đảo lần nhận | Mutation thật |
| 16 | Đóng phần thiếu | Mutation thật; lý do bắt buộc; không sửa số xuất gốc |
| 17 | Loading / partial error / empty / disabled | Đủ; load bốn nguồn độc lập và retry khi chưa đủ |

## Backend contract

### Đọc
- `GET /api/inventory/transfers?limit=500`
- `GET /api/inventory/transfers/in-transit?limit=1000`
- `GET /api/inventory/transfers/{id}`
- `GET /api/inventory/transfers/{id}/receipts`
- Tồn khả dụng: canonical Inventory balances hiện có.
- Vị trí kho đích: canonical Warehouse locations hiện có.

### Mutation
- `POST /api/inventory/transfers` — `core.inventory-transfer.create`
- `POST /api/inventory/transfers/{id}/approve` — `core.inventory-transfer.approve`
- `POST /api/inventory/transfers/{id}/dispatch` — `core.inventory-transfer.dispatch`
- `POST /api/inventory/transfers/{id}/cancel` — `core.inventory-transfer.cancel`
- `POST /api/inventory/transfers/{id}/receipts` — `core.inventory-transfer.receive`
- `POST /api/inventory/transfers/{id}/receipts/{receiptId}/approve-damage` — `core.inventory-transfer.damage-approve`
- `POST /api/inventory/transfers/{id}/receipts/{receiptId}/reverse` — `core.inventory-transfer.reverse`
- `POST /api/inventory/transfers/{id}/close-short` — `core.inventory-transfer.resolve`

Đọc màn dùng `core.inventory-transfer.read`.

Desktop dùng `ICanonicalIdempotencyKeyProvider` chung. Cùng intent/fingerprint trong cùng phiên reuse đúng key; đổi phiên thì xóa cache key. Không tự ghép Idempotency-Key từ dữ liệu nghiệp vụ.

## Quy tắc nghiệp vụ giữ nguyên

- Chỉ chọn tồn khả dụng thực tế của kho xuất.
- Kho xuất và kho nhận phải khác nhau.
- Xuất chuyển ghi hàng vào trạng thái đang đi đường; không giả thành một kho trung gian.
- Chỉ hàng nhận đạt vào tồn khả dụng kho đích.
- Hàng hư hỏng và thừa được giữ riêng để xử lý.
- Đóng phần thiếu tạo bằng chứng xử lý riêng; không sửa số lượng xuất gốc.
- Đảo lần nhận mở lại phần đang đi đường tương ứng theo backend canonical.
- Không thêm action sửa phiếu nháp vì Công Ty Web hiện hành không expose flow update trong workspace này.

## Keyboard Desktop

- `F5`: làm mới.
- `Ctrl+F`: focus tìm kiếm.
- `Esc`: đóng lần nhận → đóng tạo phiếu → đóng chi tiết theo thứ tự ngữ cảnh.

## Ngôn ngữ ứng dụng

- Dùng “Ghi sổ kho nguồn”, không đưa `inventoryMovementId`/“Movement nguồn” ra giao diện.
- Không dùng thuật ngữ developer trong nội dung người dùng nhìn thấy.

## Boundary

- Chỉ UI-3.3 Chuyển kho.
- Không làm UI-3.4 Kiểm kê kho.
- Shared Shell chỉ thay phần cần thiết để bật submenu/host/topbar thật của Chuyển kho.
- Không sửa backend/DB/migration/deploy.
