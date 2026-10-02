# Web delta 29/09–01/10/2026 — Lô 2 Cài đặt + final hardening

## Baseline

- Desktop: `main@8fd5217de090e8b40aad17231acb0183a39068e9`.
- Web authority: `binhnxwjfjxm/NPP-Platform@a5f76e7782e51ee2925f8d05d1e53ca58b40a367`.
- Desktop Issue: #95.
- Web deltas audited: #1228, #1236, #1237, #1241.

## MCP và tuyến

Web #1236 chuyển owner `/settings/mcp-routes` từ báo cáo sang route master setup. Web server mới là security boundary sang MCP. Desktop không có server-side secret boundary tương đương và Core chưa expose route CRUD cho workforce client.

Desktop Lô 2:
- tách báo cáo `employee-mcp` khỏi Cài đặt;
- `MCP và tuyến` trở thành owner riêng;
- deny-by-default theo owner hoặc `mcp.route.write`;
- không render mutation giả;
- không copy server credential xuống client.

## Nội dung đặt hàng

Core contract hiện đủ cho Desktop:
- GET `/api/customer-ordering-home-content`;
- PATCH cùng route, canonical Idempotency-Key;
- PUT `/api/customer-ordering-home-content/banner`, `image/webp`, canonical Idempotency-Key;
- title 1–80;
- `programContent` tối đa 4.000 ký tự;
- visible boolean;
- banner trả `bannerUrl/imagePresent`.

Desktop dùng `ProductImageProcessor` hiện có để đổi JPG/PNG/WebP sang WebP và giữ giới hạn output 5 MB. Không upload trực tiếp R2, không chứa R2 credential.

Retry:
- save fingerprint = title + programContent + visible;
- upload fingerprint = SHA-256 bytes;
- cùng logical payload retry giữ key cũ;
- payload khác sinh key mới;
- key chỉ clear sau success.

## Final hardening

- Cài đặt Công Ty có đúng thứ tự: Thiết lập chung → MCP và tuyến → Nội dung đặt hàng.
- Báo cáo hiệu suất được tách sang Nhân sự để không giả owner `MCP và tuyến`.
- Không backend/DB/migration.
- Không deploy production.
