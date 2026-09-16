# Công Ty Desktop — Foundation Architecture

Baseline nghiệp vụ đã audit: `binhnxwjfjxm/NPP-Platform@ca146ccfab5168f0223a6bd086ac19ac61f5542c`.

## Ranh giới runtime

```text
Công Ty Desktop (WPF)
    -> HTTPS
Backend Công Ty
    -> PostgreSQL
```

Desktop không dùng WebView, không phụ thuộc Vercel để chạy, không kết nối PostgreSQL trực tiếp và không chứa server secret.

## Các lớp Lô 1

- `CongTy.Desktop`: AppShell, navigation, workspace, theme và composition root.
- `CongTy.ApiClient`: typed HttpClient, request ID, canonical response/error envelope handling, safe HTTP logging và connection state.
- `CongTy.Contracts`: C# contract nền tương ứng canonical envelope của backend.
- `CongTy.Windows`: local settings client-safe và Windows secure credential abstraction.
- `CongTy.UnitTests`: regression cho nền Lô 1.

## Contract đã khóa trong Lô 1

Request ID theo canonical backend:

```text
^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$
```

Error envelope:

```json
{
  "error": {
    "code": "CODE",
    "message": "Message",
    "details": {},
    "retryable": false
  },
  "requestId": "req_xxx",
  "receivedAt": "ISO-8601"
}
```

Lô 1 chưa triển khai login, quyền, scope hay mutation nghiệp vụ.
