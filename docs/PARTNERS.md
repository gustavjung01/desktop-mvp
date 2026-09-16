# Đối tác — Công Ty Desktop

## Phạm vi Lô 3.2

Workspace **Đối tác** triển khai native WPF cho dữ liệu master hiện có của Công Ty:

### Khách hàng

- danh sách, tìm kiếm và lọc theo trạng thái, nhóm khách, nhân viên phụ trách;
- tạo, sửa, kích hoạt/ngừng hoạt động;
- nhóm khách hàng;
- địa chỉ khách hàng, địa chỉ mặc định và link định vị HTTPS;
- ảnh khách hàng dùng chung với MCP Thị trường;
- Tổng quan 360° đọc trực tiếp từ canonical backend;
- nhập khách hàng mới và cập nhật hàng loạt từ `.xlsx` / `.csv` theo luồng xem trước → thực hiện.

### Nhà cung cấp

- danh sách, tìm kiếm, lọc trạng thái;
- tạo, sửa, kích hoạt/ngừng sử dụng;
- người liên hệ;
- địa chỉ;
- điều khoản thanh toán;
- nhân viên phụ trách mua.

Lịch sử đơn bán, thu tiền, công nợ chi tiết, giao/trả và lịch sử mua nhà cung cấp thuộc các domain Sales, Accounting, Delivery và Purchasing. Desktop không tự tính các dữ liệu này trong Lô 3.2.

## Authority và quyền

Desktop chỉ hiển thị workspace khi `/api/internal-auth/me` trả một trong:

- `core.customer.read`
- `core.supplier.read`

Mutation tương ứng yêu cầu:

- `core.customer.write`
- `core.supplier.write`

Nhân viên phụ trách chỉ được tải khi tài khoản có `core.employee.read`.

Backend Công Ty vẫn là authority cuối cho mọi kiểm tra permission và business rule.

## Idempotency và concurrency

Mọi POST create dùng shared `CanonicalIdempotencyKeyProvider`.

- key chỉ dùng `[A-Za-z0-9._-]`;
- tối đa 128 ký tự;
- một editor giữ nguyên key cho đến khi lưu thành công hoặc người dùng hủy;
- retry cùng thao tác không tạo key khác.

PATCH master data dùng `expectedUpdatedAt` từ bản ghi backend.

Bulk import/update dùng `operationKey` **do backend trả ở bước dry-run preview** làm `Idempotency-Key` ở bước apply. Desktop không thay key này bằng key tự sinh.

## Import / cập nhật hàng loạt

Desktop đọc file cục bộ bằng .NET, không gửi file nguyên bản lên backend.

Hỗ trợ:

- `.xlsx` — đọc sheet đầu tiên;
- `.csv`;
- tối đa 10.000 dòng dữ liệu;
- tối đa 100 cột.

Desktop gửi canonical matrix:

```text
mappings[]
rows[{ rowNumber, cells[], expectedUpdatedAt? }]
```

Preview luôn chạy trước. Chỉ các dòng backend xác nhận hợp lệ mới được apply. Với bulk update, `expectedUpdatedAt` từ preview được gửi lại khi apply.

## Ảnh khách hàng

Ảnh khách hàng đi theo canonical R2 flow:

```text
GET  /api/customers/{id}/media
POST /api/customers/{id}/media   action=prepare
PUT  <presigned HTTPS URL>
POST /api/customers/{id}/media   action=finalize
```

Desktop nhận tối đa 3 ảnh/khách hàng; mỗi ảnh JPEG, PNG hoặc WebP tối đa 5 MB và cạnh tối đa 1600 px. Kích thước ảnh được đọc local trước khi finalize.

Presigned URL không đi qua request logger của Công Ty Desktop. Ảnh nguồn MCP được đọc chung nhưng Desktop không tự sửa nguồn MCP.

## Tổng quan 360°

Desktop dùng:

```text
GET /api/customers/{id}/overview?period=30d|90d|365d|all
```

Doanh số và công nợ chỉ hiển thị khi backend trả permission tương ứng. Desktop không tự tính lại số liệu.

## Export

Current Công Ty Web không có canonical export riêng trên màn Khách hàng hoặc Nhà cung cấp. Export doanh nghiệp nằm trong khu Dữ liệu & sao lưu / Office Data Exchange, vì vậy Lô 3.2 không tự tạo API export danh mục riêng.
