# UI-3.8 — Audit Tra cứu tồn kho theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop audit bắt đầu từ `main@cbeb44b2f5a763f7d215238ec3da7edc9eb1f67c`.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@a25c3db2655c79468e287f3261b5435133ff2a29`.
- Web chuẩn:
  - `npp-core/web/app/inventory/balances/page.tsx`
  - `npp-core/web/app/inventory/balances/inventory-balances-workspace.tsx`
  - `npp-core/web/lib/inventory-types.ts`
- Backend chuẩn:
  - `GET /api/inventory/balances`
  - `GET /api/inventory/balances/history?warehouseId=...&baseVariantId=...&scope=warehouse&limit=51&offset=...`
- Permission: `core.inventory.read`.
- Không có mutation ở màn này.

## Kết luận re-audit Desktop cũ

Desktop đã có logic đọc tồn/lịch sử bên trong `InventoryView`, nhưng **chưa đạt UI-3.8** vì Tra cứu tồn kho chỉ là một tab nằm trong workspace cũ gộp chung Báo cáo tồn kho và Lô hàng.

UI-3.8 sửa theo hướng:
- tạo workspace Tra cứu tồn kho độc lập;
- sidebar Tra cứu tồn kho mở đúng workspace độc lập;
- giữ service/API canonical hiện có;
- không sửa backend/DB;
- không làm lại Báo cáo tồn kho hoặc Lô hàng trong lô này.

## Matrix Web → Desktop UI-3.8

| Thứ tự | Công Ty Web | Desktop |
| --- | --- | --- |
| 1 | Tab Tồn kho | Tab đầu, mặc định |
| 2 | Tab Lịch sử kho | Tab thứ hai |
| 3 | Search | Tìm theo sản phẩm, SKU, kho, vị trí hoặc lô |
| 4 | Pagination tồn | 100 dòng/trang, Trang trước/Trang sau |
| 5 | Làm mới tồn | Nút Làm mới dữ liệu |
| 6 | Bảng tồn | STT / Kho-vị trí / Sản phẩm-SKU / Lô / Hạn dùng / Tồn kho / Đã giữ cho đơn / Có thể xuất / Xem lịch sử |
| 7 | Quy cách đóng gói | Hiển thị 1 đơn vị đóng gói = số đơn vị cơ sở khi có |
| 8 | Breakdown số lượng | Hiển thị số kiện + số lẻ khi đủ điều kiện |
| 9 | Lọc dòng | Chỉ hiện dòng on-hand hoặc reserved khác 0, giống Web |
| 10 | Xem lịch sử | Chuyển tab Lịch sử kho theo SKU + kho |
| 11 | Tồn hiện tại tại kho | Cộng tất cả vị trí/lô của cùng SKU cơ sở trong kho |
| 12 | Pagination lịch sử | 50 dòng/trang |
| 13 | Bảng lịch sử | Ngày ghi nhận / Nhân viên / Thao tác / Số lượng thay đổi / Tồn kho / Mã chứng từ / Kho |
| 14 | Mã chứng từ | Click mở chi tiết chứng từ kho |
| 15 | Dialog chi tiết | Loại + số chứng từ, thời gian, nhân viên, thao tác, delta, tồn sau, kho, vị trí, lô, ghi chú |
| 16 | Empty state | Chưa có tồn / chưa chọn SKU-kho / chưa có lịch sử |
| 17 | Keyboard Desktop | F5 làm mới tab hiện tại, Ctrl+F về tìm tồn, Esc đóng chi tiết |

## Ngôn ngữ và UI

- Dùng ngôn ngữ văn phòng, không hiện movement ID, baseVariantId hay UUID kỹ thuật.
- Trạng thái/nhấn mạnh chỉ dùng màu text khi cần; không bọc badge màu.
- Relative layout giữ đúng Web: local tabs + action ở trên, bảng ở dưới, history summary nằm trước history table.

## Boundary

- Chỉ UI-3.8 Tra cứu tồn kho.
- Không làm UI-3.9 Chính sách lô trong cùng PR.
- Không backend, DB, migration hoặc production deploy.
