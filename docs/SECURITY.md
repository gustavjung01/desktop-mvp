# Bảo mật Công Ty Desktop

## Authority

Backend Công Ty là authority cho:

- authentication;
- role;
- permission;
- branch/warehouse/territory scope;
- business authorization;
- session revocation;
- audit/outbox.

Desktop chỉ dùng authorization state để quyết định UI. Backend vẫn kiểm quyền cho mọi API.

## Deny-by-default

Desktop không có bảng permission riêng để cấp quyền.

`AccessStateService`:

- chỉ coi user authenticated sau khi `/api/internal-auth/me` hợp lệ;
- chỉ cho action khi exact permission backend đã cấp tồn tại;
- navigation không được khai báo rõ bị từ chối mặc định;
- scope giữ từ canonical `/me` response.

Ẩn nút không thay thế backend authorization.

## Dữ liệu local

Được lưu trong settings thường:

- tên cấu hình;
- tên Công Ty hiển thị;
- HTTPS API Base URL;
- theme và preference client-safe.

Không được lưu trong settings thường:

- password;
- raw session token;
- database credential;
- server API token;
- storage secret.

Session token nằm trong Windows Credential Manager và dùng key riêng theo installation endpoint.

## Logging

HTTP logging chỉ ghi method, path, status, duration và request ID. Không log Authorization header hoặc payload đăng nhập.

Redactor tiếp tục che Bearer token, password, token, secret, API key và database URL nếu chuỗi nhạy cảm vô tình đi vào log.

## 401 / 403

- 401: phiên không còn hiệu lực; Desktop xóa credential local và yêu cầu đăng nhập lại.
- 403: Desktop fail closed và hiển thị người dùng chưa được cấp quyền.
- 503/mất mạng: không giả định session hợp lệ và không tự suy ra rằng session đã bị thu hồi.

## Request ID

Mọi request giữ `X-Request-ID` canonical. Khi backend trả lỗi, UI có thể hiển thị “Mã đối chiếu” để hỗ trợ tra log.
