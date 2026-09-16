# UI-4.1 — Audit Hiệu suất giao hàng

Ngày audit: 2026-09-16

## Baseline

- Desktop: `main@24a6eed0b952ea0d3474a7057764198b740ec3b8`; post-merge Desktop CI #360 PASS.
- Không có PR Desktop mở tại thời điểm bắt đầu.
- Công Ty Web/backend: `NPP-Platform/main@4f468b5e2533debce7199433d99a8f6b10071d44`.
- Web chuẩn: `/logistics/reporting`, component `logistics-reporting-workspace.tsx`.
- Backend chuẩn: `GET /api/reporting/logistics`, quyền `core.reporting.logistics.read`, phạm vi kho deny-by-default.
- Không backend/DB/migration/deploy trong UI-4.1.

## Matrix Web → Desktop

| Thứ tự | Công Ty Web | Desktop UI-4.1 |
| --- | --- | --- |
| 1 | Kicker Giao nhận & điều phối | Giữ đúng |
| 2 | Hiệu suất giao hàng / Logistics | Giữ đúng |
| 3 | Action Mở chuyến giao / Kết quả lần giao | Giữ đúng vị trí; hiển thị disabled cho tới đúng màn UI-4.3/UI-4.5, không tạo luồng giả |
| 4 | Từ ngày | DatePicker |
| 5 | Đến ngày | DatePicker |
| 6 | Kho | ComboBox; Tất cả kho được cấp quyền |
| 7 | Áp dụng | GET lại canonical report |
| 8 | Tháng hiện tại | Bỏ filter để backend chuẩn hóa đầu tháng → hôm nay |
| 9 | Notice về SLA | Giữ ý nghĩa, đổi câu chữ sang ngôn ngữ văn phòng |
| 10 | 6 KPI | Giữ đúng thứ tự và số liệu |
| 11 | Tài xế | Bảng 10 cột theo Web |
| 12 | Phương tiện | Bảng 10 cột theo Web |
| 13 | Kết quả giao | Kết quả / lý do / số lần |
| 14 | Chuyến gần nhất | 100 chuyến; không hiện UUID kỹ thuật trên giao diện |
| 15 | Ngoại lệ | Phiếu nhận hàng trả + thiếu SLA + chưa có kết quả |
| 16 | Loading | “Đang tải hiệu suất giao hàng…” |
| 17 | Empty | Empty state riêng từng tab |
| 18 | Error | Canonical office message + request ID khi có |
| 19 | Keyboard | F5 làm mới cùng bộ lọc |

## Contract

Query:
- `from`
- `to`
- `warehouseId`

Backend tự mặc định:
- from = ngày 1 của tháng hiện tại theo Asia/Ho_Chi_Minh;
- to = hôm nay;
- khoảng tối đa 366 ngày;
- warehouse phải thuộc phạm vi được cấp.

Dữ liệu dùng đúng facts:
- delivery trip;
- trip stop;
- dispatch item / Delivery Order;
- immutable delivery attempt;
- return receipt reconciliation.

Đúng hạn chỉ tính `delivered_full` có planned arrival; thiếu planned arrival không được coi là đúng hạn.

## Ngôn ngữ giao diện

Web hiện còn một số từ kỹ thuật như canonical/source/partial/fail/exception/reconciliation. Desktop giữ nghiệp vụ nhưng chuyển sang ngôn ngữ văn phòng:
- partial → Giao một phần;
- fail → Thất bại/Giao không thành công;
- exception → Ngoại lệ;
- reconciliation → Đối soát;
- source/canonical facts → dữ liệu nghiệp vụ chính thức.

Không hiển thị UUID, permission key hay tên bảng/API trên UI.
