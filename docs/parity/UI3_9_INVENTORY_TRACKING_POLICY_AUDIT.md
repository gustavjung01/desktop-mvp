# UI-3.9 — Audit Chính sách lô theo Công Ty Web

Ngày chốt audit: 2026-09-16

## Baseline

- Master issue Desktop: #31.
- UI-3.8 đã merge vào Desktop `main@ecde603696bf734697247b6bf70d815a75779f5b`.
- Có PR #53 đang mở ở lô UI-2.8 của chat song song; UI-3.9 bắt đầu từ **main mới nhất** và trước merge phải audit lại main để không mất code bên kia.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn:
  - `npp-core/web/app/inventory/tracking-policies/page.tsx`
  - `npp-core/web/app/inventory/tracking-policies/tracking-policy-workspace.tsx`
  - `npp-core/web/lib/inventory-types.ts`
  - `npp-core/web/lib/inventory-policy-types.ts`
- Backend chuẩn:
  - `npp-core/api/src/routes/inventory-core.js`
  - `npp-core/api/src/routes/inventory-tracking-policy-candidates.js`
  - `npp-core/api/src/services/inventory-lots.js`
  - `npp-core/api/src/services/inventory-tracking-policy-candidates.js`

## Kết luận Desktop trước UI-3.9

Sidebar đã có dòng **Chính sách lô** nhưng đang bị vô hiệu hóa. Desktop chưa có workspace riêng, chưa có contract candidate/policy và chưa có mutation lưu chính sách.

UI-3.9 phải mở đúng workspace độc lập; không gộp vào Tra cứu tồn kho hay Lô hàng.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.9 |
| --- | --- | --- |
| 1 | Header Chính sách quản lý lô | Shell dùng cùng tiêu đề, subtitle và nhóm Tồn kho & lô hàng |
| 2 | Tìm kiếm | Tìm SKU bất kỳ, SKU tồn chuẩn hoặc tên hàng |
| 3 | Làm mới dữ liệu | Nút chính ở bên phải thanh tìm kiếm |
| 4 | Bố cục hai cột | Bảng SKU/chính sách bên trái, form thiết lập bên phải |
| 5 | Bảng | STT / SKU tồn chuẩn / Trạng thái / Lô / Hạn dùng / action |
| 6 | Trạng thái | Đã thiết lập / Chưa thiết lập |
| 7 | Quản lý lô | Không quản lý theo lô / Bắt buộc quản lý theo lô |
| 8 | Hạn sử dụng | Không quản lý / Có thể nhập / Bắt buộc nhập |
| 9 | Action dòng | Sửa hoặc Thiết lập, nạp chính sách hiện tại vào form |
| 10 | SKU tồn chuẩn | Dropdown toàn bộ candidate canonical |
| 11 | Khi lô = NONE | Hạn sử dụng bị khóa và ép NONE |
| 12 | Lưu | PUT policy, có expectedVersion khi cập nhật |
| 13 | Thành công | Cập nhật bảng + version hiện tại, không cần tải lại toàn màn |
| 14 | Conflict | Báo chính sách vừa thay đổi hoặc không thể nới lỏng vì đã có dữ liệu lô/hạn |
| 15 | Empty/error/permission | Có đầy đủ trạng thái và chặn action theo quyền |

Desktop bổ sung F5 làm mới, Ctrl+F tìm kiếm, Ctrl+S lưu. Keyboard không đổi flow Web.

## Backend contract

### Read

- `GET /api/inventory/tracking-policies?limit=1000&offset=0`
- `GET /api/inventory/tracking-policies/candidates?limit=2000&offset=0`

Permission:
- `core.inventory.tracking-policy.read`

### Save

- `PUT /api/inventory/tracking-policies/{baseVariantId}`
- Payload:
  - `baseVariantId`
  - `lotTrackingMode: NONE | REQUIRED`
  - `expiryTrackingMode: NONE | OPTIONAL | REQUIRED`
  - `expectedVersion` khi cập nhật policy đã tồn tại
- Permission:
  - `core.inventory.tracking-policy.manage`

Backend khóa:
- expiry khác NONE chỉ hợp lệ khi lot = REQUIRED;
- update policy hiện hữu bắt buộc đúng expectedVersion;
- không được nới lỏng quản lý lô khi đã có dữ liệu lot/movement/reservation;
- không được bỏ quản lý hạn khi đã có hạn dùng canonical.

## Idempotency

Desktop dùng shared `ICanonicalIdempotencyKeyProvider`, scope `inventory-policy-save`.

Fingerprint gồm:
`baseVariantId | lotTrackingMode | expiryTrackingMode | expectedVersion`.

Nếu cùng thao tác thất bại rồi retry với payload không đổi, Desktop reuse đúng key cũ. Sau khi lưu thành công hoặc payload thay đổi mới dùng key mới.

## Ngôn ngữ và phạm vi

- Sidebar giữ tên ngắn **Chính sách lô** theo master issue.
- Header dùng **Chính sách quản lý lô** đúng Công Ty Web.
- Không hiện UUID, version kỹ thuật, idempotency key hoặc tên permission trên UI.
- Vị trí kho không được cấu hình ở màn này; Web ghi rõ vị trí thuộc Cơ cấu Công Ty → Kho hàng.
- Không sửa backend, DB, migration hoặc deploy.
- Không làm UI-3.10 Lô hàng trong PR này.
