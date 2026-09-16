# Feature matrix — Khách hàng Công Ty Web ↔ Desktop

> Web source checkpoint: `NPP-Platform/main@3dafbaa564e2d9715ceb91bff8e35b5dbc344b2c`  
> Desktop baseline UI-2.5: `congty-desktop/main@b54dd78bd23a9ee2b5acfa4a7495617306a439ce`  
> Phạm vi: Khách hàng, Nhóm khách hàng, Thiết lập nhanh, nhập/cập nhật hàng loạt và hồ sơ chi tiết khách hàng.

## Quy tắc

- Công Ty Web hiện hành là nguồn chuẩn cho hierarchy, relative layout, field/action/state.
- Desktop giữ backend/API canonical; không tự tính nghiệp vụ.
- Nếu tài liệu cũ mâu thuẫn source Web mới, source Web thắng và matrix này phải được cập nhật.
- Không tạo action giả tới phân hệ Desktop chưa tồn tại; **Không bỏ sót im lặng**.

## Navigation hiện hành

Màn `/customers` hiện có **5 tab quản lý hiển thị**, đúng thứ tự:
1. Khách hàng
2. Thiết lập nhanh
3. Nhập KH
4. Cập nhật KH
5. Nhóm khách hàng

Ba tab giữa được `customer-bulk-tabs-launcher.tsx` chèn vào workspace hiện hành.

**Hồ sơ khách hàng không phải tab thứ 6 của màn danh mục.** Tên khách hàng mở route chi tiết `/customers/[id]`, route này có 6 tab:
1. Tổng quan
2. Hàng đã mua
3. Đơn hàng
4. Công nợ & thanh toán
5. Giao hàng / Trả hàng
6. Thông tin & địa chỉ

Desktop dùng một content host ẩn để giữ state WPF cho route chi tiết, nhưng không render nhãn “Hồ sơ 360°” thành tab quản lý.

## Đối chiếu UI-2.5

| Khu vực Web | Contract người dùng | Desktop UI-2.5 |
| --- | --- | --- |
| Header danh mục | Quản lý khách hàng / Khách hàng + subtitle canonical | Có |
| Topbar danh mục | Cập nhật dữ liệu; Thêm khách hàng hoặc Thêm nhóm theo tab | Có, action đổi theo local tab |
| Khách hàng | 3 thẻ Tổng / Đang hoạt động / Không hoạt động | Có đúng copy |
| Khách hàng | Tìm mã, tên, liên hệ; lọc trạng thái, nhóm, nhân viên phụ trách | Có đúng 4 control |
| Bảng khách | STT / Mã-tên / Nhóm-phụ trách / Liên hệ / Thanh toán / Trạng thái / Hành động | Có đúng thứ tự |
| Bảng khách | Tên mở route chi tiết | Có |
| Bảng khách | Sửa / Địa chỉ / đổi trạng thái là action trực tiếp | Có, không dùng menu gom |
| Nhóm khách hàng | STT / Mã / Tên / Mô tả / Trạng thái / Hành động | Có |
| Nhóm khách hàng | Sửa + đổi trạng thái trực tiếp | Có |
| Tạo khách | Lưu khách + địa chỉ mặc định trong một flow | Có |
| Tạo khách | Nếu khách tạo thành công nhưng địa chỉ lỗi, retry chỉ địa chỉ | Có `_pendingCreatedCustomer`; không tạo trùng khách |
| Mutation create | Shared canonical Idempotency-Key, retry reuse key | Giữ nguyên |
| Update/status | `expectedUpdatedAt` | Giữ nguyên canonical service |
| Địa chỉ | Nút Địa chỉ mở quản lý đúng khách | Có modal trong app |
| Địa chỉ | Không cho tạo địa chỉ mới khi khách đã inactive | Có guard UI và backend vẫn là authority |
| Thiết lập nhanh | Danh sách khách bên trái + thông tin / địa chỉ / ảnh | Có |
| Ảnh khách | Prepare/finalize + retry cùng attempt key | Có; ảnh chỉ ở Thiết lập nhanh như Web hiện hành |
| Nhập KH | File → mapping → preview → apply | Có |
| Cập nhật KH | Nhận diện mã khách → old/new → expected version → apply | Có |
| Route chi tiết | Header Chi tiết khách hàng + Danh sách khách hàng / Sửa thông tin | Có |
| Chi tiết | Hero code/status/name/group/phụ trách/phone + địa chỉ giao dịch | Có |
| Tổng quan | 30d / 90d / 365d / all + 5 chỉ số | Có |
| Hàng đã mua | Tìm sản phẩm + Sản phẩm/ĐVT/Tổng SL/Doanh số/Số lần/Giá gần nhất/Mua gần nhất | Có |
| Đơn hàng | Search + paging + 7 cột lifecycle | Có |
| Công nợ & thanh toán | Summary + chứng từ công nợ + lịch sử thu tiền, permission-gated | Có |
| Giao hàng / Trả hàng | 2 bảng permission-gated + paging | Có |
| Thông tin & địa chỉ | 10 fact + ghi chú + địa chỉ | Có |
| Cross-module | Mở màn kế toán/giao nhận liên quan | Chỉ bật khi phân hệ đích Desktop tồn tại; UI-2.5 không tạo nút giả |

## Alignment hard gate

Mọi DataGrid nghiệp vụ dùng shared cell/row baseline. Với mọi `DataGridTemplateColumn`, root visual phải có `VerticalAlignment="Center"`. Nút action hàng dùng shared button style có content alignment giữa. Quy tắc này áp dụng cả Inventory, Organization, Partner và Sales, không riêng Khách hàng.

## Gate UI-2.5

- Không còn tab quản lý hiển thị “Hồ sơ 360°”.
- Danh sách/nhóm dùng direct actions đúng Web.
- Retry create khách + địa chỉ không tạo trùng khách.
- Không lặp block ảnh trong route chi tiết.
- Không còn template cell top-biased ở các bảng Desktop đã audit.
- Không backend/DB migration, không deploy production trong lô này.
