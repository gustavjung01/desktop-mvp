# UI-3.6 — Audit Nhập kho thủ công theo Công Ty Web

Ngày chốt audit: 2026-09-15

## Baseline

- Master issue Desktop: #31.
- Desktop bắt đầu UI-3.6 từ `main@04b592a0b0733ae35ab0c2a0ad94a91ca706b6aa`, sau khi UI-3.5 đã merge.
- Công Ty Web/backend audit trực tiếp tại `NPP-Platform/main@a25c3db2655c79468e287f3261b5435133ff2a29`.
- Web chuẩn:
  - `npp-core/web/app/inventory/manual-inbounds/page.tsx`
  - `npp-core/web/app/inventory/manual-inbounds/manual-inbound-workspace.tsx`
- Backend chuẩn:
  - `npp-core/api/src/routes/manual-inbound.js`
  - `npp-core/api/src/services/manual-inbound-preparation.js`
  - `npp-core/api/src/services/manual-inbound-operator-preview.js`
  - `npp-core/api/src/services/manual-inbound-confirmation.js`
  - `npp-core/api/src/services/manual-inbound-history.js`
  - `npp-core/api/src/services/manual-inbound-product-search.js`
  - `npp-core/api/src/services/manual-inbound.js`
- Desktop trước UI-3.6 chỉ có submenu vô hiệu hóa, chưa có workspace nghiệp vụ.
- Không sửa backend, DB, migration, provider hoặc production deploy.

## Matrix Công Ty Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-3.6 |
| --- | --- | --- |
| 1 | Header Nhập kho thủ công | Cùng tên và nhóm Tồn kho và lô hàng |
| 2 | Thông tin chứng từ | Card đầu tiên bên trái |
| 3 | Kho nhập | Kho trong phạm vi được cấp |
| 4 | Nhà cung cấp | Danh sách nhà cung cấp đang hoạt động, có thể để trống |
| 5 | Loại nhập | Nhập hàng thủ công / Khách trả ngoài chứng từ / Hàng thu hồi / Khác |
| 6 | Ngày chứng từ | DatePicker native |
| 7 | Số chứng từ / hóa đơn tham chiếu | Có, không bắt buộc |
| 8 | Ghi chú | Có; loại Khác bắt buộc khi kiểm tra |
| 9 | Hàng nhập | Card thứ hai bên trái |
| 10 | Nhập trực tiếp | Tab thứ nhất |
| 11 | Tìm sản phẩm | Tìm từ ký tự đầu tiên theo Tên/SKU/mã vạch; hỗ trợ phím lên/xuống/Enter |
| 12 | Bảng nhập trực tiếp | SKU / Tên sản phẩm / ĐVT / Số lượng / Giá vốn / Xóa |
| 13 | Nhập từ file | Tab thứ hai |
| 14 | Tải mẫu CSV | Có |
| 15 | Chọn Excel/CSV | Đọc .xlsx/.csv, tối đa 500 dòng |
| 16 | Bảng file | SKU / Tên sản phẩm / ĐVT / Số lượng / Giá vốn / Xóa |
| 17 | Kiểm tra dữ liệu | Gọi preview; chưa thay đổi tồn kho |
| 18 | Kết quả kiểm tra | Số dòng sẵn sàng/cần xử lý, tổng số lượng, dòng trùng đã gộp |
| 19 | Preview | SKU, sản phẩm, ĐVT, số lượng, tồn hiện tại, tồn sau nhập, kho, vị trí, lô, HSD, giá vốn, trạng thái |
| 20 | Bổ sung thiếu | Sửa trực tiếp Vị trí / Lô / HSD / Giá vốn rồi bắt buộc kiểm tra lại |
| 21 | Xác nhận nhập | Chỉ khi preview sẵn sàng; tồn kho được cập nhật ngay tại bước này |
| 22 | Lịch sử | Cột bên phải |
| 23 | Lọc lịch sử | Loại nhập + số chứng từ tham chiếu |
| 24 | Dòng lịch sử | Tham chiếu, trạng thái, ngày, loại, kho |
| 25 | Xem biến động | Dialog native: Sản phẩm/SKU, ĐVT, Tồn trước, Biến động, Tồn sau |
| 26 | Đảo chứng từ | Ngày đảo + lý do; giữ nguyên lịch sử và ghi giao dịch đảo |
| 27 | Keyboard Desktop | F5 làm mới, Ctrl+F tìm hàng, Ctrl+1/2 chuyển cách nhập |

## Backend contract

### Chuẩn bị và đọc
- `GET /api/inventory/manual-inbounds/operator/warehouses`
- `GET /api/inventory/manual-inbounds/operator/locations?warehouseId=...`
- `GET /api/inventory/manual-inbounds/operator/suppliers`
- `GET /api/inventory/manual-inbounds/operator/products?warehouseId=...&search=...&limit=30`
- `GET /api/inventory/manual-inbounds/operator/history?inboundType=...&referenceNumber=...`
- `GET /api/inventory/manual-inbounds/operator/history-detail?documentId=...`
- `POST /api/inventory/manual-inbounds/operator/preview`

### Mutation
- `POST /api/inventory/manual-inbounds/operator/confirm` — cập nhật tồn kho sau khi backend preview lại và xác nhận hợp lệ.
- `POST /api/inventory/manual-inbounds/{id}/reverse` — endpoint reverse canonical hiện có trên backend.

### Permission
- `core.inventory-manual-inbound.read`
- `core.inventory-manual-inbound.prepare`
- `core.inventory-manual-inbound.post`
- `core.inventory-manual-inbound.reverse`

Mutation dùng `ICanonicalIdempotencyKeyProvider` chung. Retry cùng payload xác nhận hoặc cùng thao tác đảo reuse đúng key cũ cho đến khi thao tác thành công hoặc dữ liệu người dùng thay đổi.

## Ghi chú audit route đảo chứng từ

Web workspace hiện gọi `/api/inventory/manual-inbounds/operator/reverse`, nhưng backend route hiện hành trên cùng `main` chỉ công bố endpoint canonical `/api/inventory/manual-inbounds/{id}/reverse`.

Desktop giữ **đúng workflow Web** (mở chứng từ → nhập ngày đảo → nhập lý do → xác nhận đảo), nhưng gọi **backend canonical thực tế** để thao tác chạy thật. Không sửa backend và không tạo endpoint riêng cho Desktop.

## Quy tắc nghiệp vụ giữ nguyên

- Nhập kho thủ công tách biệt với Mua hàng, Kiểm kê, Điều chỉnh tồn và Tồn đầu kỳ.
- Mỗi lần kiểm tra tối đa 500 dòng.
- Tệp tối thiểu cần SKU và Số lượng; Giá vốn có thể để trống.
- Nếu giá vốn để trống, backend có thể dùng giá vốn hiện hành; nếu không có nguồn giá vốn phù hợp, preview yêu cầu bổ sung.
- Kho quản lý vị trí bắt buộc exact storage location; kho tồn chung không được nhập vị trí.
- Hàng quản lý lô/hạn dùng tuân thủ policy sản phẩm.
- Preview không thay đổi tồn.
- Xác nhận nhập mới ghi sổ kho và cập nhật tồn.
- Đảo chứng từ là giao dịch đảo, không xóa/sửa lịch sử gốc.
- Không đưa movement ID, scope watermark, request ID kỹ thuật hoặc thuật ngữ dev ra giao diện.

## Boundary

- Chỉ UI-3.6 Nhập kho thủ công.
- Không làm UI-3.7 Giá vốn tồn kho.
- Shared Shell chỉ thay phần cần thiết để bật submenu/host thật.
- Không sửa backend, DB, migration hoặc deploy production.
