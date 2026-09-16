# UI-7.2 — Audit COD và đối soát

Ngày audit: 2026-09-16

## Baseline thực tế

- Desktop repo: `gustavjung01/desktop-mvp`.
- Desktop baseline: `main@36332eb79727a16f9adee5d6656c75fc546e3715`.
- Baseline CI: run #5 — PASS.
- Công Ty Web/backend audit ban đầu: `binhnxwjfjxm/NPP-Platform@0f645e584378e6763720594bd3604828644cb583`. Trước commit source tiến tới `13e2b94a1d24956082500e0f4e9e13aefab2a242`; compare xác nhận thay đổi chỉ thuộc xuất/hiển thị điều chỉnh tồn kho, không chạm COD Web/backend/permission.
- Web chuẩn: `/accounting/cod-reporting`.
- Route cũ `/accounting/cod-reconciliation` chỉ redirect vào tab `Kế toán xác nhận` của workspace hiện hành.
- Không backend, DB, migration hay deploy trong UI-7.2.

## Ranh giới

UI-7.2 là workspace COD hiện hành gồm cả báo cáo vận hành và đối soát kế toán:
- 5 tab đọc từ báo cáo COD;
- 1 tab kế toán xác nhận có mutation;
- không tạo màn đối soát tổng hợp riêng trong scope này;
- không suy đoán COD từ trạng thái giao hàng.

## Matrix Web → Desktop

| Thứ tự | Web chuẩn | Desktop UI-7.2 |
| --- | --- | --- |
| 1 | Kicker `Kế toán & công nợ` | `KẾ TOÁN & CÔNG NỢ` |
| 2 | Title `COD & đối soát` | Giữ đúng |
| 3 | Subtitle | Giữ đúng nghĩa nghiệp vụ |
| 4 | Action `Đối soát tổng hợp` | Giữ vị trí/nhãn; disabled đến khi màn riêng được triển khai |
| 5 | Filter | Từ ngày → Đến ngày → Kho → Lọc báo cáo |
| 6 | Kho mặc định | `Tất cả kho được cấp quyền` |
| 7 | Notice | Tiền tài xế giữ là snapshot hiện tại; kỳ chỉ áp dụng hoạt động |
| 8 | KPI 1 | Khoản tiền tài xế đang giữ |
| 9 | KPI 2 | Bàn giao chờ kế toán nhận |
| 10 | KPI 3 | Lời hẹn thu đã quá hạn |
| 11 | KPI 4 | Cần kiểm tra / chênh lệch |
| 12 | Tab 1 | `Tài xế giữ tiền` |
| 13 | Tab 2 | `Thu trong kỳ` |
| 14 | Tab 3 | `Bàn giao & kế toán` |
| 15 | Tab 4 | `Kế toán xác nhận` |
| 16 | Tab 5 | `Hẹn thu quá hạn` |
| 17 | Tab 6 | `Cần kiểm tra` |
| 18 | Keyboard Desktop | F5 tải lại report và tab kế toán nếu đang mở |

### Tài xế giữ tiền

Bảng:
- Tài xế
- Loại tiền
- Số khoản thu
- Đang giữ
- Cũ nhất

### Thu trong kỳ

Bảng:
- Loại tiền
- Phương thức
- Trạng thái
- Số lượt
- Phải thu
- Đã nhận

### Bàn giao & kế toán

Ba section đúng thứ tự:
1. Bàn giao trong kỳ
2. Kế toán tiếp nhận trong kỳ
3. Bàn giao chờ kế toán tiếp nhận

Dòng chờ tiếp nhận có action mở tab Kế toán xác nhận đúng bàn giao.

### Kế toán xác nhận

Bố cục:
- trái: danh sách Bàn giao COD;
- phải: Đối chiếu và xác nhận.

Chi tiết:
- Tài xế khai bàn giao
- Tiền thừa chưa gắn phiếu
- Chênh lệch lúc bàn giao
- Trạng thái
- các phiếu giao: khách hàng, đang giữ, bàn giao

Mutation:
- xác nhận tiền thực nhận;
- đảo xác nhận;
- đảo bàn giao;
- đảo khoản thu sau khi bàn giao đã đảo.

Mọi thao tác đảo dùng bản ghi bù; UI không sửa dữ liệu cũ.

### Hẹn thu quá hạn

Bảng:
- Phiếu giao
- Chuyến / tài xế
- Số phải thu
- Hẹn bởi
- Quá hạn

### Cần kiểm tra

Bảng:
- Loại
- Nguồn
- Kho
- Chi tiết

Chênh lệch bàn giao và lỗi loại tiền có handover thật được mở sang tab kế toán. Lifecycle exception không hiển thị UUID và không tạo link giả tới màn đối soát tổng hợp chưa có.

## Permissions backend thật

- xem báo cáo: `core.reporting.cod.read`
- xem tab kế toán: `core.cod-reconciliation.read`
- xác nhận tiền thực nhận: `core.cod-reconciliation.accept`
- đảo/điều chỉnh: `core.cod-adjustment.create`

Tất cả deny-by-default và giữ warehouse scope trên server.

## API contract

### Báo cáo

`GET /api/reporting/cod?from&to&warehouseId`

- snapshot tiền tài xế giữ / chờ tiếp nhận / hẹn thu quá hạn / chênh lệch hiện tại không bị giới hạn bởi kỳ;
- kỳ chỉ áp dụng hoạt động thu, bàn giao và kế toán tiếp nhận;
- tiền tệ không được cộng chéo.

### Đối soát kế toán

Read:
- `GET /api/cod-reconciliation?limit=1000`
- `GET /api/cod-reconciliation/{handoverId}`

Mutation:
- `POST /api/cod-reconciliation/{handoverId}/accept`
- `POST /api/cod-reconciliation/acceptances/{acceptanceId}/reverse`
- `POST /api/cod-reconciliation/handovers/{handoverId}/reverse`
- `POST /api/cod-reconciliation/collections/{collectionId}/reverse`

## Invariants mutation

### Xác nhận

- acceptedAmount là decimal chính xác tối đa 6 chữ số thập phân;
- acceptedAt là timestamp;
- nếu tiền thực nhận khác tiền bàn giao + tiền thừa chưa gắn phiếu thì bắt buộc lý do;
- không xác nhận bàn giao đã đảo;
- không xác nhận lại acceptance còn hiệu lực;
- acceptance đã đảo thì phải đảo bàn giao và lập bàn giao mới.

### Đảo

- cần lý do;
- đảo acceptance trước khi đảo handover;
- đảo handover trước khi đảo collection;
- đảo collection cũng đảo allocation/payment liên quan qua accounting service;
- backend audit/outbox transaction giữ append-only.

## Idempotency Desktop

Desktop dùng `ICanonicalIdempotencyKeyProvider`, không tự ghép key.

- accept intent: handover + số tiền chuẩn hóa + lý do + ghi chú;
- acceptedAt được cache cùng intent để retry không đổi payload;
- reverse acceptance: acceptance + lý do;
- reverse handover: handover + lý do;
- reverse collection: collection + lý do;
- retry cùng intent reuse đúng key;
- key/timestamp chỉ xóa sau thành công.

## Workspace index

- UI-7.1 Tuổi nợ: 31
- UI-5.1 Báo cáo bán hàng: 32
- UI-5.2 Lãi gộp trên branch song song `agent/ui5-2-gross-margin`: 33
- UI-7.2 COD và đối soát: 34

UI-7.2 chủ động tránh index 33 sau khi audit branch UI-5.2 trước commit. Không thay thế navigation/host/DI của 7.1, 5.1 hoặc 5.2. Trước merge vẫn phải refresh main và branch song song.

## Ngôn ngữ văn phòng

UI dùng: bàn giao, tiền thực nhận, chênh lệch, bản ghi bù, khoản thu, hẹn thu. Không hiển thị UUID, API path, permission key, idempotency, canonical hay tên bảng.


## Refresh parity baseline trước CI cuối

Ngay trước CI cuối, Công Ty source tiến tới `d0b992757f9d055bb2896f0448d4c1d891310596`.

Đã đối chiếu delta từ `13e2b94`:
- thêm `npp-core/web/app/api/inventory/export/route.ts`;
- thêm action/model xuất dữ liệu tồn kho;
- sửa `app-shell.tsx`;
- **không thêm screen**: vẫn 73 screen;
- Web route tăng từ 286 lên 287 do Next runtime route mới;
- không đổi backend API routes, permission catalog, shared contract hay idempotency;
- không chạm Web/backend COD.

Theo policy Desktop, Next runtime route là `not_applicable` vì Desktop gọi Công Ty backend trực tiếp, nên refresh `SCREEN_MANIFEST`, `WEB_ROUTE_MANIFEST` và `DESKTOP_PARITY_MATRIX` sang exact source hiện tại; không thay đổi mapping nghiệp vụ UI-7.2.
