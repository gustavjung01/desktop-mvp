# UI-3.5 — Audit Điều chỉnh và xử lý tồn theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu UI-3.5 từ `main@c9de14ab7fa19767c4f1e8e2a32ccdabaae08ab2` sau khi UI-3.4 đã merge.
- Trong lúc làm, chat khác merge PR #45 Nhà cung cấp; main mới là `569f564f2f36b2bfb1c746677d8774b4bdeaaf66`. UI-3.5 phải đồng bộ main mới trước CI/PR cuối.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@9e5ec08793bc482f1179173276dbe44372526643`.
- Web chuẩn:
  - `npp-core/web/app/inventory/adjustments/page.tsx`
  - `npp-core/web/app/inventory/adjustments/workspace.tsx`
  - `npp-core/web/app/inventory/adjustments/adjustment-tabs.tsx`
  - `npp-core/web/app/inventory/adjustments/bulk/bulk-workspace.tsx`
- Backend chuẩn:
  - `npp-core/api/src/routes/inventory-adjustments.js`
  - `npp-core/api/src/services/inventory-adjustment.js`
  - `npp-core/api/src/services/inventory-adjustment-bulk.js`
- Desktop trước UI-3.5 chỉ có submenu vô hiệu hóa, chưa có workspace nghiệp vụ.
- Không sửa backend, DB, migration, provider hoặc production deploy.

## Matrix Công Ty Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.5 |
| --- | --- | --- |
| 1 | Tiêu đề Điều chỉnh tồn | Cùng tiêu đề, kicker và mô tả nghiệp vụ |
| 2 | Phiếu điều chỉnh | Tab 1, mặc định khi mở từ sidebar |
| 3 | Điều chỉnh thủ công | Tab 2 |
| 4 | Điều chỉnh hàng loạt | Tab 3 |
| 5 | Phiếu: lọc Trạng thái / Loại phiếu | Đủ, cùng thứ tự |
| 6 | Danh sách phiếu | Số phiếu, loại, kho, trạng thái, thời gian |
| 7 | Chi tiết phiếu | Kho, nguồn, người lập/gửi/duyệt, cập nhật tồn |
| 8 | DRAFT | Hủy / Gửi duyệt |
| 9 | SUBMITTED | Hủy / Duyệt |
| 10 | APPROVED | Hủy / Cập nhật tồn kho |
| 11 | POSTED | Hoàn tác phiếu |
| 12 | Dòng phiếu | Sản phẩm, SKU, lô, vị trí, vị trí nhận, số lượng, ảnh hưởng tồn |
| 13 | Điều chỉnh thủ công | Loại phiếu → Điều chỉnh → Kho → Sản phẩm/Lô/Vị trí → Vị trí nhận khi cần → Số lượng → Lý do → Diễn giải |
| 14 | 4 loại xử lý | Điều chỉnh thủ công / Chuyển cách ly / Chuyển hư hỏng / Tiêu hủy |
| 15 | Điều chỉnh hàng loạt bước 1 | Tải tệp mẫu CSV |
| 16 | Bước 2 | Chọn tệp .xlsx hoặc .csv |
| 17 | Bước 3 | Kiểm tra dữ liệu bằng backend preview |
| 18 | Xem trước | Dòng, SKU, tên hàng, vị trí, lô, tồn hệ thống, tồn thực tế, chênh lệch, kết quả, trạng thái |
| 19 | Sửa Lô/Vị trí | Dropdown khi backend trả lựa chọn; sửa xong bắt buộc kiểm tra lại |
| 20 | Kết quả kiểm tra | Sẵn sàng, tăng, giảm, không chênh lệch, cần xử lý |
| 21 | Bước 4 | Chọn lý do tăng/giảm theo dữ liệu + diễn giải |
| 22 | Lập phiếu hàng loạt | Có thể tạo riêng phiếu tăng và phiếu giảm, sau đó mở phiếu vừa tạo |
| 23 | Keyboard Desktop | F5 làm mới; Ctrl+1/2/3 chuyển 3 tab |

## Backend contract

### Đọc
- `GET /api/inventory/adjustments?status=&documentKind=&limit=&offset=`
- `GET /api/inventory/adjustments/reasons`
- `GET /api/inventory/adjustments/{id}`
- Tồn hiện tại dùng canonical `/api/inventory/balances`.
- Kho/vị trí dùng canonical organization API hiện có.

### Mutation
- `POST /api/inventory/adjustments` — `core.inventory-adjustment.create`
- `POST /api/inventory/adjustments/{id}/submit` — `core.inventory-adjustment.submit`
- `POST /api/inventory/adjustments/{id}/approve` — `core.inventory-adjustment.approve`
- `POST /api/inventory/adjustments/{id}/post` — `core.inventory-adjustment.post`
- `POST /api/inventory/adjustments/{id}/cancel` — `core.inventory-adjustment.cancel`
- `POST /api/inventory/adjustments/{id}/reverse` — `core.inventory-adjustment.reverse`
- `POST /api/inventory/adjustments/bulk-preview` — quyền đọc, không mutation tồn
- `POST /api/inventory/adjustments/bulk-confirm` — tạo phiếu, dùng canonical idempotency

Đọc màn dùng `core.inventory-adjustment.read`.

Desktop dùng `ICanonicalIdempotencyKeyProvider` chung. Retry cùng intent/fingerprint trong cùng phiên reuse đúng key cũ. Không tự ghép dữ liệu nghiệp vụ thành giá trị header Idempotency-Key.

## Quy tắc nghiệp vụ giữ nguyên

- Tồn kho không thay đổi khi lập phiếu, gửi duyệt hoặc duyệt.
- Chỉ bước **Cập nhật tồn kho** của phiếu APPROVED mới ghi thay đổi tồn.
- Backend kiểm tra revision và exact stock scope trước các bước nhạy cảm.
- Người lập không được tự duyệt nếu backend không cấp ngoại lệ phù hợp.
- Hủy bắt buộc có lý do và chỉ trước khi đã cập nhật tồn.
- Hoàn tác chỉ áp dụng phiếu POSTED; nếu đã có giao dịch tồn downstream thì phải lập điều chỉnh mới thay vì sửa lịch sử.
- Chuyển cách ly chỉ nhận vị trí loại cách ly; chuyển hư hỏng chỉ nhận vị trí hư hỏng.
- File hàng loạt tối đa 200 dòng; tối thiểu có SKU và Tồn thực tế.
- Hàng loạt chỉ lập phiếu sau preview hợp lệ; thay đổi Lô/Vị trí làm preview cũ hết hiệu lực.
- Nếu file có cả tăng và giảm, backend tạo riêng phiếu Tăng và phiếu Giảm.
- Không đưa revision, movement ID, scope watermark hoặc thuật ngữ kỹ thuật ra giao diện.

## Boundary

- Chỉ UI-3.5 Điều chỉnh và xử lý tồn.
- Không làm UI-3.6 Nhập kho thủ công.
- Shared Shell chỉ thay phần cần thiết để bật submenu/host/topbar thật của UI-3.5.
- Không sửa backend, DB, migration hoặc deploy production.
