# CÔNG TY DESKTOP — MASTER PLAN

> Trạng thái: Kế hoạch nền tảng, chưa triển khai source ứng dụng.
>
> Repo: `binhnxwjfjxm/congty-desktop`
>
> Nguồn nghiệp vụ chuẩn đã audit gần nhất: `binhnxwjfjxm/NPP-Platform` `main@33e688b733ba7d69b2284a7fe22672c01120b791`.
>
> Migration source head đã audit: **136** (`database/migrations/inventory/136_inventory_tracking_policy_backfill.sql`). Migration 135 nằm ở `database/migrations/shared/135_sales_order_sku_search_indexes.sql`.
>
> Đây là trạng thái source/migration head. Trước thao tác DB production vẫn phải audit migration runtime thực tế, backup và restore gate; không mặc định production đã chạy 136 chỉ từ source.
>
> Trước mỗi lô triển khai phải audit lại `NPP-Platform/main`, PR đang mở và CI gần nhất; không mặc định trạng thái cũ còn đúng.

---

## 1. Mục tiêu sản phẩm

Xây dựng **Công Ty Desktop** là ứng dụng Windows native độc lập, phục vụ đầy đủ nghiệp vụ Công Ty hiện có nhưng không phụ thuộc giao diện web/Vercel để vận hành.

Mục tiêu dài hạn là dùng cùng một codebase để triển khai cho nhiều khách hàng, nhưng **mỗi khách hàng là một installation độc lập hoàn toàn**:

```text
Khách A
Desktop A -> Backend A -> PostgreSQL A

Khách B
Desktop B -> Backend B -> PostgreSQL B

Khách C
Desktop C -> Backend C -> PostgreSQL C
```

Không dùng chung backend giữa khách hàng.

Không dùng chung PostgreSQL giữa khách hàng.

Không thiết kế multi-tenant chung database.

Không fork source code riêng cho từng khách hàng.

Khác biệt giữa khách hàng phải đi qua cấu hình installation, module/feature, branding và adapter tích hợp; không sửa lõi thành các nhánh sản phẩm riêng lẻ.

---

## 2. Phạm vi

### 2.1. Trong phạm vi

- Công Ty Desktop cho Windows.
- Kết nối trực tiếp Công Ty API của installation tương ứng.
- Sử dụng đầy đủ nghiệp vụ canonical của backend Công Ty.
- Đăng nhập, phân quyền, phạm vi chi nhánh/kho.
- Danh mục nền.
- Bán hàng.
- Kho.
- Mua hàng.
- Kế toán/công nợ/thanh toán.
- Giao nhận thuộc phạm vi Công Ty.
- Báo cáo.
- Nhập/xuất Excel.
- In ấn native.
- File, clipboard, phím tắt, nhiều cửa sổ khi cần.
- Cơ chế cập nhật desktop.
- Cấu hình installation để triển khai cho từng khách hàng độc lập.
- Module/feature/branding để bán theo nhu cầu từng khách hàng.

### 2.2. Ngoài phạm vi

- Retail trên điện thoại.
- MCP Field.
- Delivery app riêng.
- Admin MCP/NPP riêng.
- WebView hoặc đóng gói website Công Ty thành desktop.
- Electron/Tauri/WebView2 làm runtime giao diện.
- Desktop kết nối PostgreSQL trực tiếp.
- Desktop chứa `DATABASE_URL`, service-role key hoặc server secret.
- Một backend dùng chung cho nhiều khách hàng.
- Một database dùng chung cho nhiều khách hàng.
- Offline mutation trong V1.

---

## 3. Kiến trúc đích

### 3.1. Web Công Ty tiếp tục tồn tại

```text
Công Ty Web -> Vercel -> Công Ty API -> PostgreSQL
```

### 3.2. Desktop Công Ty

```text
Công Ty Desktop -> HTTPS -> Công Ty API -> PostgreSQL
```

Desktop **không đi qua Vercel**.

Web và Desktop là hai frontend khác nhau nhưng dùng chung business authority của cùng installation.

### 3.3. Ranh giới trách nhiệm

**Backend Công Ty chịu trách nhiệm:**

- business rules;
- authentication authority;
- authorization;
- permission/scope;
- pricing;
- inventory state;
- document lifecycle;
- receivable/payable;
- accounting state;
- idempotency;
- audit/outbox;
- canonical validation;
- database transaction;
- storage authority khi có dữ liệu nghiệp vụ.

**Desktop chịu trách nhiệm:**

- giao diện;
- điều hướng;
- trạng thái hiển thị;
- nhập liệu;
- file picker;
- Excel presentation/parsing khi phù hợp;
- printing;
- clipboard;
- phím tắt;
- local preferences;
- update client;
- gọi API và hiển thị lỗi canonical.

Không copy business rule từ backend vào Desktop để tự quyết định kết quả nghiệp vụ.

---

## 4. Kết quả audit nền hiện tại

Audit baseline trên `NPP-Platform` cho thấy kiến trúc hiện tại thuận lợi để làm desktop:

- `npp-core/web` chủ yếu đóng vai trò UI + gateway tới Công Ty API.
- Không thấy luồng giao diện Công Ty dùng PostgreSQL trực tiếp làm business path.
- Sales, Purchasing, Customer, Inventory, Reporting đã có gateway/API backend rõ ràng.
- Auth backend đã có login/me/logout và session/token authority.
- Permission/scope được backend cung cấp.
- Idempotency có canonical contract dùng chung.
- Một số xử lý file/presentation như XLSX đang nằm phía Next.js; desktop có thể thay bằng xử lý native miễn business validation vẫn do backend quyết định.
- Product image route có phần validation file phía web nhưng quyền lưu/prepare/commit/delete vẫn thuộc backend API.

Các file audit đại diện:

```text
npp-core/web/app/api/sales-orders/route.ts
npp-core/web/lib/sales-order-gateway.ts
npp-core/web/app/api/purchase-orders/route.ts
npp-core/web/app/api/customers/route.ts
npp-core/web/app/api/inventory/balances/route.ts
npp-core/web/app/api/reporting/sales/route.ts
npp-core/web/app/api/data-exchange/xlsx/route.ts
npp-core/web/app/api/products/images/route.ts
packages/contracts/index.js
```

Kết luận: **không viết desktop bằng cách chép từng màn web**. Phải inventory toàn bộ backend + web + quyền rồi map sang desktop.

---

## 5. Công nghệ desktop

### 5.1. Stack chính

- C#.
- .NET 10 LTS.
- WPF.
- Windows x64 là mục tiêu đầu tiên.
- MVVM rõ ràng nhưng không over-engineer framework nội bộ.
- `HttpClient`/typed client cho API.
- Dependency Injection chuẩn .NET.
- Structured logging có redaction.
- MSIX hoặc cơ chế Windows packaging tương đương sau khi release strategy được audit/chốt.

### 5.2. Tiêu chí giao diện

WPF chỉ là runtime UI. Giao diện phải được thiết kế theo tiêu chí **phần mềm văn phòng/ERP hiện đại**, không dùng phong cách Windows cổ.

Yêu cầu:

- sidebar rõ ràng;
- workspace rộng;
- DataGrid mật độ hợp lý;
- virtualization cho bảng lớn;
- typography thống nhất;
- light/dark/theme doanh nghiệp;
- form nhập liệu tối ưu bàn phím;
- trạng thái/nút có màu ngữ nghĩa nhưng không lòe loẹt;
- **Công Ty Web là chuẩn bố cục nghiệp vụ**: Desktop phải giữ cùng thứ tự khu vực, tab, nhóm thông tin, bộ lọc, thao tác chính và luồng danh sách -> hồ sơ/chi tiết -> thao tác;
- không được gom, tách hoặc đổi thứ tự bố cục theo cách làm mất khả năng nhận biết nghiệp vụ giữa Web và Desktop;
- tên menu/tab/nút và thuật ngữ nghiệp vụ phải thống nhất với Web, trừ khi có quyết định đổi tên chung cho cả hai frontend;
- Desktop được phép nâng cấp trải nghiệm bằng DataGrid native, mật độ hiển thị, phím tắt, dialog, resize, keyboard navigation và thao tác hàng loạt;
- không bê pixel giao diện web sang desktop một cách máy móc: yêu cầu parity là **information architecture + business flow**, không phải pixel;
- màn Desktop bổ sung tiện ích riêng phải đặt trong đúng ngữ cảnh nghiệp vụ Web, không đẩy nghiệp vụ Web bắt buộc sang vị trí khó tìm;
- ngôn ngữ UI là ngôn ngữ văn phòng, tránh từ kỹ thuật không cần thiết.

---

## 6. Nguồn sự thật chống sót nghiệp vụ

Lô đầu tiên phải tạo và duy trì 5 manifest.

### 6.1. `SCREEN_MANIFEST`

Quét toàn bộ màn Công Ty hiện hành.

Mỗi màn phải ghi:

- route web;
- tên nghiệp vụ;
- nhóm menu;
- action người dùng;
- quyền hiển thị;
- API sử dụng;
- trạng thái desktop.

### 6.2. `WEB_ROUTE_MANIFEST`

Quét toàn bộ:

```text
npp-core/web/app/api/**
```

Mỗi route phải được phân loại:

- proxy/gateway thuần;
- presentation/file processing;
- auth/session adapter;
- business logic cần di chuyển về backend;
- web-only không liên quan desktop.

Không được để route ở trạng thái “chưa biết”.

### 6.3. `BACKEND_API_MANIFEST`

Quét toàn bộ API Công Ty.

Mỗi endpoint cần có tối thiểu:

- domain;
- method;
- path;
- request contract;
- response contract;
- auth mode;
- permission;
- scope;
- idempotency policy;
- error codes;
- caller web hiện tại;
- caller desktop;
- contract test status.

### 6.4. `PERMISSION_MANIFEST`

Quét canonical permission catalog và scope.

Không tự nghĩ quyền riêng desktop nếu backend chưa có authority tương ứng.

### 6.5. `DESKTOP_PARITY_MATRIX`

Nối 4 manifest trên thành ma trận duy nhất:

```text
Màn
-> chức năng
-> thao tác
-> web route
-> backend API
-> permission/scope
-> request/response
-> lỗi
-> desktop feature
-> contract test
-> integration test
-> UI test
-> trạng thái
```

Một module chỉ được coi là hoàn thành khi tất cả action của nó đã được classified và đạt gate.

---

## 7. CI chống drift và chống sót

Repo desktop phải tự audit `NPP-Platform/main`.

Các trường hợp phải làm CI đỏ:

```text
UNCLASSIFIED_BACKEND_API
UNCLASSIFIED_WEB_ROUTE
UNCLASSIFIED_COMPANY_SCREEN
UNCLASSIFIED_PERMISSION
API_CONTRACT_DRIFT
PERMISSION_CONTRACT_DRIFT
IDEMPOTENCY_CONTRACT_DRIFT
UNMAPPED_MUTATION
```

Nguyên tắc:

- backend thêm API -> desktop phải classify;
- web thêm màn/action -> desktop phải classify;
- permission thay đổi -> desktop phải cập nhật manifest;
- contract thay đổi -> test parity phải phát hiện;
- không bắt buộc mỗi API đều phải có UI desktop nếu đó là API nội bộ/web-only, nhưng phải có lý do classification rõ ràng.

Mỗi manifest phải lưu NPP source SHA đã audit để biết desktop đang đối chiếu với phiên bản nào.

---

## 8. Contract và Idempotency

Desktop C# không tự tạo convention khác backend.

Phải dựng bộ **contract conformance test** giữa canonical contract hiện tại và implementation C#.

Ít nhất bao gồm:

- response envelope;
- error envelope;
- decimal;
- tiền;
- quantity;
- request ID;
- Idempotency-Key validation;
- Idempotency-Key generator test vectors;
- retry behavior.

Quy tắc bắt buộc:

```text
Idempotency-Key chỉ dùng canonical generator/contract.
Không tự ghép key.
Không dùng ký tự ngoài [A-Za-z0-9._-].
Retry cùng thao tác phải reuse đúng key cũ.
```

Mutation không được retry mù.

GET/read có thể retry theo policy có giới hạn.

---

## 9. Authentication và Authorization

### 9.1. Auth

Desktop gọi trực tiếp auth API Công Ty của installation.

Luồng nền:

```text
Login native
-> Công Ty API
-> session/token
-> lưu bằng Windows secure credential storage
-> gọi /me
-> nhận user/role/permission/scope
```

Không dùng cookie Vercel làm dependency.

Không đóng gói `CORE_API_SERVER_TOKEN` hoặc server secret vào desktop.

### 9.2. Authorization

Desktop áp dụng **deny-by-default**.

UI dùng permission để quyết định khả năng hiển thị/thao tác, nhưng backend vẫn là nơi kiểm quyền cuối cùng.

```text
Ẩn nút != bảo mật.
Backend authorization = authority.
```

Scope chi nhánh/kho phải lấy từ backend canonical, không duy trì một bảng quyền riêng trong desktop.

---

## 10. Installation độc lập cho từng khách hàng

Đây là nguyên tắc sản phẩm bắt buộc.

### 10.1. Mỗi khách hàng

Có riêng:

- Desktop installation/profile;
- backend deployment;
- PostgreSQL;
- biến môi trường backend;
- backup/restore;
- migration state;
- release/smoke backend;
- storage nếu khách yêu cầu cách ly hoàn toàn;
- domain/API URL;
- branding/config/module profile.

### 10.2. Không dùng

- shared customer database;
- tenant ID để trộn dữ liệu nhiều khách trong một DB;
- backend chung rồi phân tenant;
- DB credentials trên desktop.

### 10.3. Desktop Setup

Lần đầu chạy, desktop chỉ cần cấu hình các giá trị client-safe như:

```text
Tên installation
Tên Công Ty hiển thị
API Base URL
Mã installation công khai nếu thực sự cần
Kênh cập nhật app
```

Desktop không hỏi hoặc lưu:

```text
DATABASE_URL
DB password
R2 secret
service-role key
backend signing secret
```

### 10.4. Backend Setup

Mỗi installation backend có bộ setup riêng để nhập biến server thủ công.

Nhóm biến dự kiến:

```text
DATABASE_URL
DATABASE_SSL_MODE
PUBLIC_API_URL
SESSION/AUTH secrets
Storage provider config
Storage credentials
Mail provider config
Allowed origins / network policy
Observability config
```

Tên biến chính xác phải lấy từ backend hiện hành tại thời điểm triển khai; không hardcode plan này thành contract khi repo nguồn thay đổi.

### 10.5. Quy trình cài một khách mới

```text
1. Chuẩn bị PostgreSQL riêng.
2. Chuẩn bị backend riêng.
3. Nhập env server.
4. Test kết nối DB.
5. Xác nhận backup/restore policy.
6. Chạy migration theo repo.
7. Bootstrap tài khoản chủ installation.
8. Health/live + health/ready PASS.
9. Cấu hình Desktop API URL.
10. Login + smoke nghiệp vụ.
11. Ghi version backend/desktop/migration của installation.
```

Không sửa production DB thủ công để “cài nhanh”.

---

## 11. Product hóa để bán theo nhu cầu

Một codebase phải hỗ trợ nhiều cấu hình khách hàng mà không fork source.

### 11.1. Module/Feature

Các module có thể bật/tắt theo installation sau khi thiết kế contract phù hợp, ví dụ:

- Sales;
- Inventory;
- Purchasing;
- Accounting;
- Logistics;
- Reporting;
- advanced printing;
- advanced import/export;
- integration adapters.

Feature flag không được dùng để bỏ qua authorization backend.

### 11.2. Branding

Cho phép cấu hình:

- tên Công Ty;
- logo;
- màu chủ đạo;
- tên hiển thị ứng dụng;
- mẫu in được backend cho phép;
- thông tin liên hệ.

Branding không được tạo fork code.

### 11.3. Integration Adapter

Khách hàng cần tích hợp đặc thù phải đi qua adapter/interface có boundary rõ, ví dụ:

- storage;
- email;
- máy in/chứng từ;
- hệ thống ngoài;
- import source;
- webhook.

Không rải `if customer == ...` khắp codebase.

### 11.4. Custom nghiệp vụ

Nếu một khách cần nghiệp vụ mới:

1. xác định đó là khả năng sản phẩm chung hay custom module;
2. nếu là business rule, authority phải nằm backend của installation;
3. desktop chỉ hiển thị/gọi contract;
4. không hardcode riêng trong UI nếu backend không có contract;
5. thay đổi lõi phải có migration/contract/test đầy đủ.

---

## 12. Các phân hệ cần đạt parity

### 12.1. Tổng quan

- Dashboard.
- KPI/cảnh báo hiện có.
- shortcut nghiệp vụ.
- trạng thái cần xử lý.

### 12.2. Cơ cấu Công Ty

- Chi nhánh.
- Kho.
- Vị trí kho.
- Thiết lập liên quan cơ cấu.
- Số chứng từ nếu nằm trong phạm vi người dùng Công Ty.

### 12.3. Nhân sự & Phân quyền

- Danh mục nhân sự.
- Vai trò và phân quyền.
- Người dùng.
- Tài khoản.
- Phạm vi chi nhánh & kho.
- Credential/session administration nếu UI hiện hành cho phép.

### 12.4. Khách hàng

- Danh sách.
- Chi tiết/360°.
- Địa chỉ.
- Liên hệ.
- Import.
- Cập nhật hàng loạt.
- Dữ liệu mua hàng/công nợ liên quan.
- Media nếu có.

### 12.5. Nhà cung cấp

- Danh mục.
- Địa chỉ/liên hệ.
- Lịch sử mua.
- Công nợ liên quan.

### 12.6. Sản phẩm

- Sản phẩm.
- SKU.
- Đơn vị tính.
- Barcode.
- Quy đổi.
- Khối lượng.
- Ảnh.
- Chính sách tồn kho.
- Import/update.

### 12.7. Giá

- Bảng giá.
- Giá SKU.
- Giá theo kênh nếu có.
- Resolve/apply price.
- Import/update giá.
- Quyền override giá/chiết khấu.
- Giá lần mua trước nếu contract hiện hành đã merge.

### 12.8. Bán hàng

Không được coi Sales là CRUD đơn giản.

Phải inventory đầy đủ:

- danh sách;
- tìm kiếm/lọc;
- tạo nháp;
- sửa nháp;
- SKU search;
- context giá;
- entry settings;
- khách hàng;
- địa chỉ;
- kho;
- kênh bán;
- luồng giao;
- xác nhận;
- điều chỉnh/amendment;
- hủy;
- manual edit nếu canonical API cho phép;
- issue stock;
- close execution;
- fulfillment;
- giao nhận;
- thanh toán/công nợ liên quan;
- in;
- export;
- lỗi 409/422/503;
- permission;
- idempotency.

### 12.9. Kho

- tồn kho;
- ledger/balance;
- reservation;
- fulfillment;
- transfer;
- stocktake;
- adjustment;
- manual opening/import nếu có;
- lot/batch;
- costing;
- vị trí kho;
- cảnh báo liên quan.

### 12.10. Mua hàng

- Purchase Order.
- Draft/update/confirm/cancel theo contract.
- Giá mua.
- Nhận hàng/Goods Receipt.
- Supplier Return.
- Liên kết công nợ phải trả.
- In/export.

### 12.11. Kế toán

- Receivable.
- Customer Payment.
- Allocation/reversal.
- Return credit nếu có.
- Payable.
- Supplier payment.
- COD.
- Aging.
- Reconciliation.
- Permission và audit.

### 12.12. Giao nhận

Chỉ phần thuộc Công Ty Desktop:

- delivery order;
- trip;
- dispatch;
- handover;
- delivery attempt;
- COD/reconciliation;
- trạng thái giao.

Không biến repo này thành app Delivery riêng.

### 12.13. Báo cáo

- Sales.
- Purchasing.
- Inventory.
- Gross profit.
- Accounting/aging.
- Logistics.
- COD.
- Filter/kỳ/kho/chi nhánh.
- Drill-down.
- Export.

Danh sách cuối cùng phải lấy từ manifest hiện hành, không lấy riêng danh sách này làm nguồn sự thật.

---

## 13. Excel / Data Exchange

Desktop xử lý file native nhưng không trở thành business authority.

Luồng chuẩn:

```text
Open file
-> parse workbook
-> preview
-> map cột
-> validation trình bày/cấu trúc
-> gửi canonical payload tới backend
-> backend validate nghiệp vụ + ghi DB
-> trả kết quả từng dòng
-> desktop hiển thị/export lỗi
```

Có thể xử lý native:

- đọc/ghi XLSX;
- chọn sheet;
- mapping cột;
- preview;
- giới hạn kích thước;
- định dạng file kết quả.

Không được xử lý riêng trên desktop:

- tính giá canonical;
- xác định tồn hợp lệ cuối cùng;
- bypass permission;
- tự ghi database;
- tự sinh document lifecycle khác backend.

---

## 14. In ấn native

Desktop phải tận dụng Windows printing thay vì phụ thuộc browser print dialog.

Phạm vi:

- chọn máy in;
- máy in mặc định;
- printer profile;
- A4/A5/80mm khi nghiệp vụ cần;
- hướng giấy;
- margin;
- số bản;
- tray nếu driver hỗ trợ;
- preview;
- in trực tiếp;
- remember preference theo máy/người dùng khi an toàn.

Dữ liệu chứng từ và business amount vẫn lấy từ backend canonical.

Không tính lại tổng tiền/công nợ/tồn kho trong template in.

---

## 15. Local storage và Offline

### 15.1. Được lưu local

- theme;
- window size/position;
- column layout;
- printer profile;
- recent search;
- cache lookup không authoritative;
- update state;
- client-safe installation profile;
- session token chỉ qua Windows secure credential storage.

### 15.2. Không offline mutation trong V1

Không queue local để tạo sau:

- đơn bán;
- PO;
- nhập kho;
- điều chỉnh tồn;
- thanh toán;
- công nợ;
- giao nhận mutation.

Mất mạng phải báo rõ trạng thái kết nối.

Không tự replay mutation với key mới.

Offline mutation chỉ được thiết kế sau này nếu có yêu cầu thực tế và phải có outbox/conflict/idempotency contract riêng.

---

## 16. Error model và Observability

Mỗi request desktop cần giữ context an toàn:

```text
requestId
method
endpoint pattern
duration
HTTP status
business error code
retryable
desktop version
installation public identifier nếu an toàn
```

Không log:

- password;
- session token;
- DB credentials;
- storage secret;
- raw sensitive business payload nếu không cần thiết.

Desktop phải giữ nguyên ý nghĩa lỗi backend:

- 400: request/validation hình thức;
- 401: hết/không có session;
- 403: không có quyền;
- 404: không tồn tại;
- 409: conflict nghiệp vụ;
- 422: dữ liệu nghiệp vụ không hợp lệ;
- 503: tạm thời không sẵn sàng.

Không gom tất cả thành “Có lỗi xảy ra”.

Khi hỗ trợ khách hàng, request ID phải là khóa đối chiếu chính với backend log.

---

## 17. UX Desktop chuẩn văn phòng

Desktop phải tối ưu thao tác thực tế:

```text
Ctrl+F  tìm kiếm
Ctrl+N  tạo mới
Ctrl+S  lưu khi phù hợp
F2      sửa khi phù hợp
Enter/Tab nhập liệu nhanh
Esc     đóng/hủy ngữ cảnh phù hợp
```

Không gán phím tắt làm mutation nguy hiểm nếu không có xác nhận/guard phù hợp.

Các màn nghiệp vụ nặng như Sales/Purchasing/Inventory ưu tiên:

- keyboard-first;
- DataGrid virtualization;
- tìm SKU nhanh;
- focus rõ;
- không nhảy layout;
- không popup dư thừa;
- giữ context đang làm việc;
- cảnh báo lỗi ngay tại trường khi là validation cục bộ;
- business error từ backend hiển thị rõ và có request ID khi cần hỗ trợ.

---

## 18. Cấu trúc repo dự kiến

```text
congty-desktop/
|
|-- src/
|   |-- CongTy.Desktop/
|   |   |-- App/
|   |   |-- Shell/
|   |   |-- Features/
|   |   |-- Controls/
|   |   |-- Themes/
|   |   `-- Resources/
|   |
|   |-- CongTy.ApiClient/
|   |-- CongTy.Contracts/
|   `-- CongTy.Windows/
|
|-- tests/
|   |-- CongTy.UnitTests/
|   |-- CongTy.ContractTests/
|   |-- CongTy.IntegrationTests/
|   `-- CongTy.UITests/
|
|-- parity/
|   |-- screens.json
|   |-- web-routes.json
|   |-- backend-apis.json
|   |-- permissions.json
|   `-- desktop-parity.json
|
|-- tools/
|   `-- parity-audit/
|
|-- docs/
|   |-- ARCHITECTURE.md
|   |-- API_PARITY.md
|   |-- INSTALLATION.md
|   |-- UI_STANDARD.md
|   |-- SECURITY.md
|   `-- RELEASE.md
|
`-- .github/workflows/
```

Không tách project C# thêm nếu chưa có boundary thực sự.

---

## 19. Kế hoạch triển khai theo lô

### Lô 0 — Audit & Parity Foundation

Mục tiêu: có máy kiểm inventory trước khi viết nghiệp vụ desktop.

Làm:

- bootstrap solution/repo tối thiểu;
- scanner màn Công Ty;
- scanner Next API routes;
- scanner backend routes;
- scanner permission catalog;
- tạo 5 manifest;
- lưu source SHA;
- CI phát hiện drift;
- contract test vector framework;
- phân loại mọi route hiện tại.

Gate:

```text
0 unclassified screen
0 unclassified web route
0 unclassified backend API
0 unclassified permission
```

Không yêu cầu tất cả đã implement desktop; yêu cầu tất cả đã **biết nó là gì và phải xử lý thế nào**.

### Lô 1 — Desktop Foundation

Làm:

- WPF AppShell;
- navigation;
- theme foundation;
- DI;
- typed HttpClient;
- request ID;
- error envelope;
- logging + redaction;
- local settings;
- secure credential abstraction;
- connection state.

Gate:

- app native khởi động ổn;
- không WebView;
- không Vercel runtime dependency;
- không DB direct;
- secret scan PASS.

### Lô 2 — Installation + Auth + Access

Làm:

- first-run installation setup;
- API URL validation;
- health check;
- login;
- me;
- logout;
- session expiry;
- role/permission/scope;
- deny-by-default navigation/action;
- owner/admin auth flows theo backend hiện hành.

Gate:

- auth parity PASS;
- permission parity PASS;
- scope parity PASS;
- không server secret trong desktop.

### Lô 3 — Master Data

Làm:

- Công Ty/chi nhánh;
- kho/vị trí;
- nhân sự;
- khách hàng;
- nhà cung cấp;
- sản phẩm;
- SKU;
- đơn vị tính;
- barcode;
- ảnh;
- giá/bảng giá;
- import/update tương ứng.

Gate:

- CRUD/action parity theo manifest;
- permission PASS;
- import/export contract PASS.

### Lô 4 — Sales

> Triển khai Desktop bắt đầu từ `congty-desktop/main@d0f63a42b7516d9e4b5577482a79b78bf04e390f`, đối chiếu Công Ty source `33e688b733ba7d69b2284a7fe22672c01120b791` và migration source head 136.
>
> Workspace Desktop giữ luồng nghiệp vụ Web: **tổng hợp → bộ lọc → danh sách → chi tiết → thao tác vòng đời**. Mutation dùng canonical Idempotency-Key; retry cùng thao tác phải reuse đúng key cũ.

Làm toàn bộ Sales lifecycle theo API manifest, không chỉ màn tạo đơn.

Gate:

- 100% Sales action classified;
- 100% required Sales action implemented;
- idempotency PASS;
- 409/422/503 behavior PASS;
- integration PASS;
- UI smoke PASS.

### Lô 5 — Inventory

Làm:

- balance/ledger;
- reservation;
- fulfillment;
- transfer;
- stocktake;
- adjustment;
- lot;
- costing;
- warehouse/location flows;
- các action canonical hiện hành.

Gate: Inventory parity PASS.

### Lô 6 — Purchasing

Làm:

- PO lifecycle;
- purchasing price;
- goods receipt;
- supplier return;
- payable linkage;
- print/export.

Gate: Purchasing parity PASS.

### Lô 7 — Accounting

Làm:

- receivable;
- customer payment;
- allocation/reversal;
- payable;
- supplier payment;
- COD;
- aging/reconciliation.

Gate: Accounting parity PASS.

### Lô 8 — Logistics thuộc Công Ty

Làm các chức năng giao nhận mà người dùng Công Ty cần quản trị.

Không đưa app Delivery riêng vào repo.

Gate: Company logistics parity PASS.

### Lô 9 — Reporting

Làm:

- dashboards;
- Sales reports;
- Purchasing reports;
- Inventory reports;
- gross profit;
- accounting/aging;
- logistics/COD;
- filters;
- drill-down;
- export.

Gate: Reporting parity PASS.

### Lô 10 — Native Office

Làm:

- Excel native;
- file handling;
- printing;
- printer profiles;
- keyboard productivity;
- clipboard;
- drag/drop khi cần;
- multi-window có kiểm soát;
- native dialogs.

Gate: các luồng desktop native chính PASS.

### Lô 11 — Productization

Làm nền bán cho nhiều khách:

- installation config schema;
- module/feature profile;
- branding profile;
- integration adapter boundaries;
- version compatibility;
- customer deployment checklist;
- installation inventory;
- không fork source.

Gate:

- tạo được 2 installation test độc lập hoàn toàn;
- backend/DB A không liên quan backend/DB B;
- cấu hình module/branding khác nhau không cần sửa source.

### Lô 12 — Full Parity Audit & Hardening

Audit lại từ đầu trên `NPP-Platform/main` mới nhất:

- screen;
- web routes;
- backend API;
- permission;
- contract;
- error behavior;
- performance;
- memory;
- large tables;
- long-running screens;
- session expiry;
- network interruption;
- recovery;
- printing;
- file import/export.

Gate:

```text
0 unclassified
0 unexplained parity gap
0 server secret
0 DB direct
0 mutation idempotency violation
```

### Lô 13 — Production Release

Làm:

- package/sign;
- versioning;
- update channel;
- rollback;
- release notes;
- installer smoke;
- backend compatibility declaration;
- customer installation runbook.

Gate:

- install PASS;
- update PASS;
- rollback PASS;
- auth PASS;
- critical business smoke PASS.

---

## 20. CI dự kiến

Tối thiểu:

```text
dotnet restore
dotnet build
unit tests
contract conformance tests
backend API parity scan
web route parity scan
screen parity scan
permission parity scan
integration tests
UI smoke tests
packaging test
secret scan
```

CI không được yêu cầu production credential.

Không dùng production DB cho test thường ngày.

---

## 21. Version và Compatibility

Mỗi desktop release phải biết tối thiểu:

```text
Desktop version
NPP source SHA đã parity
Minimum compatible backend contract/version
Manifest version
Release channel
```

Không giả định desktop mới luôn tương thích backend cũ hoặc ngược lại.

Trước update phải có compatibility check.

Không tự update giữa lúc người dùng đang mutation chứng từ.

---

## 22. Quy trình Git cho repo này

Sau commit khởi tạo repo, mọi thay đổi tiếp theo dùng:

```text
main
-> agent/<task>
-> code/audit/test
-> gom đủ nguyên nhân trước khi rerun
-> CI xanh
-> PR
-> review diff
-> merge khi có lệnh
-> verify main
-> sync local main
-> working tree sạch
-> xóa branch
```

Không force-push.

Không tạo thêm SHA chỉ để thử mò khi chưa xác định đủ nguyên nhân.

Không merge/deploy/release production nếu chưa có yêu cầu rõ.

---

## 23. Quy trình thay đổi backend khi Desktop phát hiện thiếu API

Nếu desktop cần một nghiệp vụ mà web hiện đang xử lý ở Next/Vercel và backend chưa có canonical API:

```text
1. Dừng implementation phần đó trên desktop.
2. Audit NPP-Platform.
3. Thiết kế API canonical ở Công Ty backend.
4. Permission/idempotency/audit đầy đủ.
5. Migration nếu thực sự cần DB change.
6. Test + CI.
7. Merge backend theo workflow riêng.
8. Cập nhật parity manifest.
9. Desktop mới gọi API đó.
```

Không copy business algorithm từ TypeScript sang C# để né việc bổ sung backend.

---

## 24. Database và Migration cho nhiều installation

Mỗi khách hàng có PostgreSQL riêng, nhưng schema/migration source vẫn dùng chung từ product backend.

Yêu cầu:

- migration nằm trong repo backend;
- không sửa DB production thủ công;
- mỗi installation ghi migration head/version;
- backup/restore là trách nhiệm riêng của installation;
- migration lớn cần backup xác nhận được + restore rehearsal + reconciliation;
- không phát tán DB credentials cho desktop hoặc người dùng cuối không cần quyền server.

Product hóa không được biến migration thành script tùy tiện riêng cho từng khách.

Nếu một khách có extension/schema khác biệt, phải thiết kế module migration rõ ràng thay vì sửa tay DB.

---

## 25. Security boundary

Desktop được coi là môi trường không tin cậy từ góc nhìn server.

Không đặt vào desktop:

- database password;
- service role;
- backend server token;
- signing private key;
- storage secret;
- mail API secret;
- migration credential.

Backend phải tự xác thực và phân quyền mọi request dù desktop đã ẩn chức năng.

HTTPS bắt buộc cho production API.

Secret backend được cấu hình theo từng installation trên server.

---

## 26. Definition of Done toàn sản phẩm

Chỉ gọi **Công Ty Desktop hoàn chỉnh** khi đạt:

```text
100% màn Công Ty được classified
100% web route được classified
100% backend API được classified
100% permission được classified
100% required business action có desktop parity

0 business mutation phụ thuộc Vercel
0 direct DB từ desktop
0 server secret trong desktop
0 mutation sai Idempotency-Key
0 unexplained parity gap

Auth PASS
Master Data PASS
Sales PASS
Inventory PASS
Purchasing PASS
Accounting PASS
Company Logistics PASS
Reporting PASS
Excel PASS
Printing PASS

Installation isolation PASS
Module/feature profile PASS
Branding profile PASS
Installer PASS
Update PASS
Rollback PASS
```

---

## 27. Việc tiếp theo

**Không bắt đầu bằng việc vẽ giao diện.**

Task tiếp theo sau khi plan được duyệt là **Lô 0 — Audit & Parity Foundation**:

1. bootstrap solution tối thiểu;
2. tạo branch `agent/desktop-parity-foundation`;
3. dựng scanner/inventory;
4. sinh 5 manifest từ `NPP-Platform/main` mới nhất;
5. phân loại đầy đủ;
6. dựng CI chống drift;
7. chưa làm màn nghiệp vụ cho tới khi Gate Lô 0 đạt.

Đây là gate bảo đảm Công Ty Desktop lấy đầy đủ nghiệp vụ hiện hành và vẫn theo kịp Công Ty backend về sau.