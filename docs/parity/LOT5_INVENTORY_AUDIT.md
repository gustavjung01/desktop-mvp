# Lô 5 — audit Kho/Tồn kho Desktop theo Công Ty Web

Ngày audit: 2026-09-15

## Baseline

- Desktop: `main@287859b4fac2a00bd2e025099f8307cc7ef19da6`
- Desktop CI #177: PASS trên đúng baseline.
- Công Ty Web/backend được đối chiếu trên `NPP-Platform/main@af855ad5cc903cf2d7c133a10a6d7eba985be47d`.
- Không có migration, không deploy, không thay đổi production trong Lô 5.

## Ma trận nghiệp vụ

| Phạm vi | Công Ty Web | Backend canonical | Desktop trước Lô 5 | Xử lý |
| --- | --- | --- | --- | --- |
| Tồn theo SKU/kho | `/inventory/balances` | `GET /api/inventory/balances` · `core.inventory.read` | Chưa có workspace | Bổ sung |
| Lịch sử biến động | Tab **Lịch sử kho**; mở từ dòng tồn | `GET /api/inventory/balances/history`, `scope=warehouse` | Chỉ có popup trong đơn bán | Bổ sung workspace |
| Vị trí/lô | Kho/vị trí/lô/HSD trên dòng tồn; danh mục lô riêng | `GET /api/inventory/lots` · `core.inventory.lot.read` | Chỉ có cấu hình kho/vị trí trong Tổ chức nội bộ | Bổ sung |
| Nhập – xuất – tồn | Tab **Luân chuyển** | `GET /api/reporting/inventory` | Chưa có | Bổ sung |
| Báo cáo tồn | Bộ lọc + 6 tab Web | `core.reporting.inventory.read` | Chưa có | Bổ sung |
| Xuất file | Web chưa có nút xuất riêng | Không có inventory-export endpoint riêng | Chưa có | Xuất CSV native từ dataset báo cáo đã tải, cần `core.reporting.export` |

## Bố cục khóa theo Web

### Tra cứu tồn kho

Hai tab theo đúng Web:

1. **Tồn kho**
2. **Lịch sử kho**

Bảng tồn giữ thứ tự thông tin: Kho/vị trí → Sản phẩm/SKU → Lô → Hạn dùng → Tồn kho → Đã giữ cho đơn → Có thể xuất → thao tác.

Lịch sử khi mở từ một dòng tồn luôn gọi canonical history với `warehouseId + baseVariantId + scope=warehouse`. Không thu hẹp theo vị trí hoặc lô, vì Web đã khóa contract này.

### Báo cáo tồn kho

Bộ lọc giữ đúng thứ tự: Từ ngày → Đến ngày → Kho → Chậm luân chuyển → Áp dụng → Đặt lại.

Sáu tab giữ đúng thứ tự:

1. Tổng quan
2. Tồn hiện tại
3. Luân chuyển
4. Chậm luân chuyển
5. Lô & hạn dùng
6. Cần kiểm tra

Desktop thêm **Xuất file** ở hàng hành động báo cáo. Đây là tiện ích Desktop; dữ liệu xuất là dataset canonical đã tải, không tạo công thức hay nguồn số liệu riêng.

### Danh mục lô

Danh mục lô ở workspace Kho/Tồn kho. **Sơ đồ kho/vị trí kho** vẫn thuộc Tổ chức nội bộ, không bị đổi nghĩa hoặc nhập nhằng với tồn theo vị trí.

## Keyboard Desktop

- `F5`: làm mới khu vực đang mở.
- `Ctrl+F`: đưa con trỏ vào ô tìm kiếm tương ứng.
- `Enter`: mở lịch sử từ dòng tồn / mở chi tiết dòng lịch sử.
- `Esc`: đóng chi tiết lịch sử.

## Ranh giới Lô 5

- Chỉ đọc tồn, lịch sử, lô và báo cáo; không tạo mutation tồn mới.
- Không chỉnh production DB, không migration.
- Không tự deploy.
- Deny-by-default theo các permission canonical.
- Giao diện dùng ngôn ngữ văn phòng; không đưa thuật ngữ kỹ thuật Core/NPP ra UI.
- Giá vốn chuyên sâu, kiểm kho, điều chuyển, nhập đầu kỳ và mutation kho nằm ngoài phạm vi Lô 5 này.
