# UI-3.10 — Audit Lô hàng theo Công Ty Web

Ngày chốt audit: 2026-09-16

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu từ `main@80f8500178aab6380d2808bdb9c8543273290a68`.
- Main CI #337 xanh trước khi bắt đầu.
- Không có PR Desktop mở tại thời điểm audit.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn:
  - `npp-core/web/app/inventory/lots/page.tsx`
  - `npp-core/web/app/inventory/inventory-scoped-workspace.tsx`
  - `npp-core/web/lib/inventory-types.ts`
  - `npp-core/web/lib/inventory-scoped-snapshot.ts`
- Backend chuẩn:
  - `npp-core/api/src/routes/inventory-core.js`
  - `npp-core/api/src/services/inventory-lots.js`

## Kết luận Desktop trước UI-3.10

Desktop đã có đọc lô hàng bằng API thật và có bảng lô nằm trong workspace tồn kho cũ. Tuy nhiên sidebar **Lô hàng** vẫn điều hướng vào tab thứ ba của workspace gộp, chưa đạt yêu cầu một màn nghiệp vụ độc lập bám route Web `/inventory/lots`.

UI-3.10 tách Lô hàng thành workspace độc lập, giữ API canonical hiện có và không tạo mutation mới.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.10 |
| --- | --- | --- |
| 1 | Tiêu đề `Lô hàng` | Tiêu đề `Lô hàng` |
| 2 | Subtitle | Theo dõi mã lô, ngày sản xuất, hạn sử dụng và thông tin liên quan của từng SKU |
| 3 | Kicker | Tồn kho, lô và nhập đầu kỳ |
| 4 | Search | Tìm theo SKU, mã lô hoặc hạn dùng |
| 5 | Làm mới | Nút `Làm mới dữ liệu` |
| 6 | Bảng | STT / SKU / Lô / Hạn dùng / Ngày SX / Tham chiếu nhà cung cấp / Tạo lúc |
| 7 | SKU | SKU chính; dòng phụ là mã sản phẩm · tên sản phẩm |
| 8 | Lô | Mã lô; normalized code chỉ hiện dòng phụ khi khác mã hiển thị |
| 9 | Ngày trống | Hiện `Không có` |
| 10 | Tham chiếu NCC trống | Hiện `—` |
| 11 | Empty state | `Chưa có lô hàng nào.` |
| 12 | Permission | `core.inventory.lot.read`, deny-by-default |
| 13 | Keyboard Desktop | F5 làm mới, Ctrl+F tìm kiếm |

## Backend contract

UI-3.10 chỉ đọc:

- `GET /api/inventory/lots?limit=1000&offset=...`
- Permission: `core.inventory.lot.read`
- Backend hỗ trợ `search` và `baseVariantId`, nhưng Công Ty Web hiện tải toàn bộ pages rồi lọc local; Desktop giữ đúng flow đó qua `IInventoryService.ListLotsAsync()`.

Route detail `GET /api/inventory/lots/{id}` tồn tại ở backend nhưng Công Ty Web hiện không có action mở chi tiết lô, nên Desktop **không tự thêm** dialog/action chi tiết.

Không có create/update/delete lot ở màn Web này. Lô canonical được sinh trong các nghiệp vụ nhập/chuyển/điều chỉnh theo chính sách lô.

## Ngôn ngữ và boundary

- Không hiện UUID, installation id, metadata hoặc permission code trên UI.
- Không thêm badge/pill trạng thái.
- Không backend, DB, migration, deploy.
- Không làm UI-3.11 Thiết lập tồn đầu kỳ trong PR này.
