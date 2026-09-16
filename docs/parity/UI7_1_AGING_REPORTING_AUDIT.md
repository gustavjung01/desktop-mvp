# UI-7.1 — Audit Tuổi nợ

Ngày audit: 2026-09-16

## Baseline thực tế

- Desktop: `main@1794e265712e55547ab40611c83f78719bcddb30`.
- Desktop baseline CI: run #453 — PASS.
- Công Ty Web/backend: `NPP-Platform/main@698324a530f4c1741cd20ab0ba1ee0596fc862d8`.
- Web chuẩn: `/accounting/aging`.
- Backend chuẩn: `GET /api/reporting/aging`.
- UI-5.1 đang chạy song song trên branch `agent/ui5-1-sales-reporting-parity`. Khi bắt đầu audit branch ở baseline `1794e265...`; trong lúc UI-7.1 chạy CI, UI-5.1 tiến tới `ec5f4e5d51848899c6b1609e1814c7d50a3c8789` và dùng workspace index 31, đồng thời chạm `App.xaml.cs`, `ShellViewModel.cs`, `MainWindow.xaml/.cs`.
- Không backend, DB, migration hay deploy trong UI-7.1.\n- Trong lúc CI chạy, Công Ty source tiến từ `21751ced` lên `7f7910c`. Re-audit xác nhận contract `/accounting/aging` và `/api/reporting/aging` không đổi; thay đổi mới chỉ bổ sung xuất báo cáo lãi gộp. Parity manifest được cập nhật sau khi kiểm endpoint mới là GET export, không phải mutation nghiệp vụ. Source sau đó tiến tiếp tới `1d5fdb8`; compare xác nhận contract aging không đổi. Trước merge, source tiến tới `698324a`; thay đổi mới bổ sung route xuất Đơn mua hàng phía Web, không đổi API/permission aging. Parity baseline được đồng bộ lại theo source hiện hành.

## Ranh giới nghiệp vụ

UI-7.1 là báo cáo **chỉ đọc**.

- Không có mutation.
- Không có Idempotency-Key.
- Không có filter ngày.
- Filter duy nhất là kho trong phạm vi được cấp.
- Backend tự tính ngày chốt theo `Asia/Ho_Chi_Minh`.

## Matrix Web → Desktop

| Thứ tự | Web chuẩn | Desktop UI-7.1 |
| --- | --- | --- |
| 1 | Kicker `Kế toán & công nợ` | Shell `KẾ TOÁN & CÔNG NỢ` |
| 2 | Title `Tuổi nợ phải thu / phải trả` | Giữ đúng |
| 3 | Subtitle | Giữ nghĩa nghiệp vụ, bỏ từ kỹ thuật |
| 4 | Header action `Công nợ phải thu` | Giữ đúng vị trí; disabled cho đến UI-7.3 |
| 5 | Header action `Công nợ phải trả` | Giữ đúng vị trí; disabled cho đến UI-7.6 |
| 6 | Filter `Kho` | Giữ đúng |
| 7 | Kho mặc định | `Tất cả kho được cấp quyền` |
| 8 | Action | `Áp dụng` → `Đặt lại` |
| 9 | Error | Hiển thị lỗi tải báo cáo |
| 10 | Initial loading | `Đang tải tuổi nợ…` |
| 11 | Notice | Ngày chốt → cách tính phải thu → phải trả → tách tiền tệ |
| 12 | Tab 1 | `Phải thu` |
| 13 | Section 1 | `Phải thu khách hàng` |
| 14 | Bảng 1 | Tiền tệ → Tuổi khoản phải thu → Chứng từ → Còn phải thu |
| 15 | Empty 1 | `Không có khoản phải thu đang mở.` |
| 16 | Section 2 | `Khách hàng còn công nợ` |
| 17 | Bảng 2 | Khách hàng → Tiền tệ → Chứng từ → Còn phải thu → Chứng từ cũ nhất → Tuổi lớn nhất |
| 18 | Empty 2 | `Không có dữ liệu.` |
| 19 | Tab 2 | `Phải trả` |
| 20 | Section 1 | `Phải trả nhà cung cấp` |
| 21 | Bảng 1 | Tiền tệ → Trạng thái hạn → Chứng từ → Còn phải trả |
| 22 | Empty 1 | `Không có khoản phải trả đang mở.` |
| 23 | Section 2 | `Nhà cung cấp còn công nợ` |
| 24 | Bảng 2 | Nhà cung cấp → Tiền tệ → Chứng từ → Còn phải trả → Hạn sớm nhất → Quá hạn lớn nhất |
| 25 | Empty 2 | `Không có dữ liệu.` |
| 26 | Keyboard Desktop | F5 tải lại, không đổi workflow Web |

## Contract backend thật

### Permission

- `core.reporting.aging.read`.

### Query

- `warehouseId` tùy chọn.
- Backend từ chối `from` hoặc `to` cho aging.
- Kho yêu cầu phải nằm trong warehouse scope của tài khoản.
- Nếu tài khoản không có warehouse scope, backend deny-by-default.

### Response dùng trên UI

- `currentDate`
- `scopeWarehouses`
- `filters.warehouseId`
- `receivable.summary`
- `receivable.customers`
- `payable.summary`
- `payable.suppliers`

Backend còn trả document detail nhưng Web UI hiện hành không render bảng document; Desktop không tự thêm section ngoài Web.

## Cách tính đã đối chiếu backend

### Phải thu

- Chỉ số dư phải thu đang mở / phân bổ một phần, còn số dư > 0.
- Tuổi tính từ ngày chứng từ nguồn vì phải thu hiện không có ngày đến hạn riêng.
- Bucket: 0–30 / 31–60 / 61–90 / trên 90 ngày.

### Phải trả

- Chỉ số dư phải trả đang mở / phân bổ một phần, còn số dư > 0.
- Quá hạn dùng đúng ngày đến hạn trên chứng từ.
- Bucket: chưa đến hạn / quá hạn 1–30 / 31–60 / 61–90 / trên 90 ngày.

### Tiền tệ

Không cộng chéo các loại tiền tệ.

## Action sang màn kế tiếp

Web có link tới Công nợ phải thu và Công nợ phải trả ở header và section. Desktop UI-7.1 giữ đúng vị trí/nhãn nhưng **disabled có chủ đích** vì UI-7.3/UI-7.6 chưa được triển khai; không tạo flow giả. Khi hai màn đó hoàn thiện phải nối lại action thật.

## Song song UI-5.1

UI-7.1 hiện dùng workspace index 31 trên branch độc lập vì branch được tạo trước khi UI-5.1 có commit. UI-5.1 sau đó cũng dùng index 31. **Đây là collision đã biết và tuyệt đối không được merge nguyên trạng cả hai.** Trước merge bắt buộc refresh `main` + head UI-5.1:
- nếu UI-5.1 merge trước, tích hợp `main` mới vào UI-7.1 bằng commit mới, giữ toàn bộ Sales Reporting host/navigation/DI và chuyển Aging sang index kế tiếp liên tục;
- nếu UI-7.1 merge trước, UI-5.1 phải thực hiện quy tắc tương đương ở branch của nó;
- không force-push, không thay thế nguyên file shell bằng bản cũ;
- exact-head CI phải xanh lại sau tích hợp.

## Ngôn ngữ văn phòng

Desktop không hiển thị UUID, permission key, API path, tên bảng hay thuật ngữ `canonical`. Notice được viết thành ngôn ngữ nghiệp vụ: ngày chứng từ, ngày đến hạn, loại tiền tệ.
