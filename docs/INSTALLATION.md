# Cài đặt Công Ty Desktop

## Phạm vi Lô 2

Công Ty Desktop kết nối trực tiếp tới Công Ty API qua HTTPS:

```text
Công Ty Desktop -> HTTPS -> Công Ty API -> PostgreSQL
```

Desktop không đi qua Vercel, không dùng WebView và không kết nối PostgreSQL trực tiếp.

## Thiết lập lần đầu

Người dùng chỉ nhập thông tin client-safe:

- tên cấu hình cài đặt;
- tên Công Ty hiển thị;
- API Base URL.

API Base URL bắt buộc là HTTPS origin, ví dụ:

```text
https://company.example.com
```

Không chấp nhận URL có user/password, query, fragment hoặc path con.

Trước khi lưu, Desktop phải xác nhận lần lượt:

```text
GET /health/live   -> data.status = "ok"
GET /health/ready  -> data.status = "ready"
```

Nếu một trong hai bước không đạt, cấu hình không được coi là sẵn sàng.

## Xác thực

Desktop dùng trực tiếp contract canonical của backend Công Ty:

```text
POST /api/internal-auth/login
GET  /api/internal-auth/me
POST /api/internal-auth/logout
```

Session là opaque Bearer token do backend cấp. Desktop không tự giải mã, không tự gia hạn và không tự tạo session.

Raw token chỉ được lưu trong Windows Credential Manager qua abstraction `ISecureCredentialStore`. Tệp `settings.json` không chứa token, password hoặc server secret.

Sau login, Desktop bắt buộc gọi `/api/internal-auth/me` trước khi lưu session local. Quyền và scope dùng trong UI lấy từ response `/me`, không lấy từ bảng hard-code của Desktop.

## Owner / quản trị

Khi backend trả:

```text
INTERNAL_AUTH_OWNER_CHALLENGE_REQUIRED
```

Desktop chuyển sang bước nhập mã xác minh và gọi lại login bằng cùng `loginName`, password và `sourceApp`, kèm `ownerCode`.

Desktop không tự quyết định tài khoản nào là Owner. `ownerKind`, role, permission và scope đều do backend Công Ty trả về.

## Session hết hiệu lực

Desktop fail closed khi:

- `/me` trả 401;
- `/me` trả 403;
- `session.expiresAt` đã qua;
- response auth không đủ contract bắt buộc.

Trong các trường hợp session hết hiệu lực, credential local bị xóa và người dùng phải đăng nhập lại.

Khi backend tạm thời 503 hoặc mất mạng, Desktop không coi session là hợp lệ để mở workspace nhưng cũng không tự xóa credential chỉ vì lỗi kết nối tạm thời.

## Thay đổi installation

Nếu đổi sang API Base URL khác, Desktop xóa session credential của installation cũ trước khi lưu cấu hình mới. Credential key được dẫn xuất từ HTTPS authority nên session giữa hai installation không dùng chung.
