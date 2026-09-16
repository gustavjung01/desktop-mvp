# UI-2.9 — Số chứng từ — Web → Desktop audit

Base Desktop ban đầu: `main@80f8500178aab6380d2808bdb9c8543273290a68`  
Base đã đồng bộ sau UI-3.10: `main@e4de4eb3b5996afe01aa9770b114ab73eb2313b8`  
Nguồn Công Ty Web/backend: `NPP-Platform@4f468b5e2533debce7199433d99a8f6b10071d44`  
Route chuẩn: `/document-numbering`

## Matrix

| Công Ty Web | Desktop UI-2.9 | Contract |
| --- | --- | --- |
| Header Số chứng từ | Header shell + workspace Số chứng từ | `core.document-number.read` |
| Quy tắc đánh số dùng chung | Card đầu + Thêm quy tắc | GET/POST `/api/document-number-series` |
| Tìm + trạng thái + cập nhật | Toolbar trên bảng | query/list canonical |
| Bảng quy tắc | Quy tắc / Cấu trúc số / Chu kỳ / Số đã cấp / Trạng thái / thao tác | series response |
| Chi tiết | Khối dưới bảng | GET allocations |
| Cấp số tham chiếu | Ngày chứng từ + cấp số thật | POST `/:id/allocate` |
| Tiến độ đánh số | Số tiếp theo từng kỳ | counters |
| Lịch sử cấp số | Bảng full-width | allocations |
| Tạo/Sửa | Modal cùng workspace | POST/PATCH series |

## Trường cấu hình công khai

Desktop chỉ cho phép đúng 5 trường như Web:

1. Loại chứng từ
2. Tên quy tắc
3. Ký hiệu đầu số
4. Cấu trúc số
5. Chu kỳ đánh lại số

Không đưa technical `code`, `sequence_width`, `start_counter`, timezone ra UI.

## Luật bắt buộc

- Mỗi loại chứng từ chỉ có một series active.
- Sau allocation đầu tiên, format bị khóa; đổi format phải ngừng series cũ rồi tạo series mới.
- Allocation tham chiếu là số thật, ghi counter/history nhưng không tạo chứng từ nghiệp vụ.
- POST create/allocate dùng shared canonical Idempotency-Key; retry cùng payload reuse key cũ.
- PATCH dùng `expectedUpdatedAt`.
- Read: `core.document-number.read`; write/allocate: `core.document-number.write`.
- Không backend/DB/migration/deploy.

## Concurrency

PR #55 UI-3.10 đã merge vào main. UI-2.9 đã ghép lại trên main mới, giữ nguyên Lô hàng ở workspace index 16 và chuyển Số chứng từ sang workspace index 17. App/Shell giữ đầy đủ code của cả hai lô.
