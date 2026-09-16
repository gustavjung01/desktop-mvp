# Desktop parity foundation

Lô 0 khóa đường biên giữa Desktop Công Ty và nguồn nghiệp vụ hiện tại. Năm manifest trong thư mục này là baseline có kiểm chứng, không phải danh sách viết tay tách rời source.

## Cách phân loại baseline

Scanner đọc trực tiếp source `binhnxwjfjxm/NPP-Platform` và tạo inventory cho toàn bộ `page.tsx`, `route.ts`, backend route sources/endpoints có thể phát hiện, permission keys và mutation candidates. Baseline dùng Git object SHA của đúng các subtree/blob đã audit. Vì vậy mọi phần tử trong snapshot hiện tại được phân loại theo rule của manifest; khi source thay đổi, snapshot cũ không còn hợp lệ và CI chặn thay vì tự động coi phần mới là `planned`.

`page.tsx` hiện tại = `planned`. Next `route.ts` = `not_applicable` với Desktop vì Desktop gọi Công Ty backend qua HTTPS. Backend endpoints, permission keys và mutation mappings hiện tại = `planned` cho các lô nghiệp vụ sau. `implemented` chỉ được dùng khi Desktop đã làm xong contract tương ứng.

## Gate CI

`tools/parity-check.mjs` tạo `artifacts/parity-report.json`, ghi inventory thực tế, mapping và fingerprint. CI fail bằng đúng các mã:

- `UNCLASSIFIED_BACKEND_API`
- `UNCLASSIFIED_WEB_ROUTE`
- `UNCLASSIFIED_COMPANY_SCREEN`
- `UNCLASSIFIED_PERMISSION`
- `API_CONTRACT_DRIFT`
- `PERMISSION_CONTRACT_DRIFT`
- `IDEMPOTENCY_CONTRACT_DRIFT`
- `UNMAPPED_MUTATION`

`tools/parity-self-test.mjs` cố tình tạo các tình huống drift ở tầng đánh giá để bảo đảm các gate trên thực sự chặn.

## Lô 2 — Installation + Auth + Access

Lô 2 triển khai trực tiếp các canonical boundary hiện hành:

- `GET /health/live`
- `GET /health/ready`
- `POST /api/internal-auth/login`
- `GET /api/internal-auth/me`
- `POST /api/internal-auth/logout`

Desktop không dùng Next/Vercel auth adapter. Role, permission và branch/warehouse/territory scope chỉ lấy từ `/me`. UI deny-by-default và không tự giữ permission catalog làm authority.

Baseline được audit lại trên `NPP-Platform/main@ba8d295be718ac97beaf446da312876723994bf6`. Từ baseline Lô 0 đến revision này, thay đổi ảnh hưởng fingerprint chỉ nằm ở component Sales hiện hữu dưới `npp-core/web/app`; không thêm/xóa page hoặc Next route. Auth fix gần nhất chỉ đổi repository implementation dưới `npp-core/api/src/db/repositories`, không đổi backend route tree, permission tree, shared contract hoặc idempotency contract.

## Khi Công Ty source thay đổi

Không sửa SHA chỉ để làm CI xanh. Audit diff nguồn trước, cập nhật đúng classification/mapping, kiểm tra API/permission/idempotency impact, rồi mới rebaseline các fingerprint liên quan trong cùng PR Desktop. Mutation phải dùng canonical `Idempotency-Key`; retry cùng thao tác phải reuse đúng key cũ.


## Lô 3.1 — Tổ chức nội bộ

Desktop triển khai native WPF cho nhóm dữ liệu nền tổ chức đang có trên Công Ty Web:

- Chi nhánh: danh sách, tìm kiếm, lọc trạng thái, tạo, sửa, kích hoạt/ngưng hoạt động.
- Kho hàng: danh sách, tìm kiếm, lọc trạng thái, tạo, sửa, chính sách xuất vượt tồn, kích hoạt/ngưng sử dụng.
- Vị trí kho: danh sách theo kho, tạo, sửa, kích hoạt/ngưng sử dụng.
- Sơ đồ kho: xem trước, xác nhận chuyển chế độ và lịch sử thay đổi theo canonical backend.
- Nhân sự: danh sách, tìm kiếm, lọc trạng thái/chi nhánh, tạo, sửa, kích hoạt/ngưng làm việc.

Desktop gọi trực tiếp Công Ty backend qua HTTPS. Các Next route dưới `/api/organization/**` và `/api/access/**` chỉ là Web adapter và không được mang sang Desktop.

Quyền UI lấy từ `/me` và deny-by-default với các canonical permission:

- `core.branch.read/write`
- `core.warehouse.read/write`
- `core.warehouse.location.read/write`
- `core.employee.read/write`

Các thao tác tạo mới dùng shared `CanonicalIdempotencyKeyProvider`; retry cùng form giữ nguyên khóa cũ cho đến khi thành công hoặc người dùng hủy. PATCH dùng `expectedUpdatedAt` của bản ghi backend để khóa optimistic concurrency.

Baseline được audit lại trên `NPP-Platform/main@c1d2b6ab4db89c271d760001fd46369d26a676fc`. So với baseline trước:
- Web app tree đổi do sửa màn tồn kho hiện hữu; không thêm/xóa page hoặc Next route.
- API route tree đổi tại `routes/organization.js` để khôi phục mapping 404/409 cho lỗi service; không thêm route organization mới.
- permission tree, server route registry, shared contracts và canonical idempotency implementation không đổi.


## Lô 3.2 — Đối tác

Desktop triển khai native WPF cho master data đối tác đang có trên Công Ty Web/backend:

- Khách hàng: danh sách, lọc, tạo/sửa, trạng thái, nhóm khách hàng, địa chỉ và ảnh dùng chung với MCP Thị trường.
- Tổng quan 360°: đọc `/api/customers/{id}/overview`; doanh số/công nợ chỉ hiển thị theo permission backend.
- Nhập/cập nhật khách hàng: đọc `.xlsx` / `.csv` local, gửi matrix canonical, bắt buộc preview trước apply.
- Nhà cung cấp: danh sách, lọc, tạo/sửa, trạng thái, liên hệ, địa chỉ và điều khoản thanh toán.

Desktop gọi trực tiếp Công Ty backend qua HTTPS; không dùng Next/Vercel adapters.

Quyền UI deny-by-default:

- `core.customer.read/write`
- `core.supplier.read/write`
- `core.employee.read` chỉ để tải lookup nhân sự phụ trách khi được cấp.

Mọi POST create dùng shared `CanonicalIdempotencyKeyProvider`. Bulk import/update reuse chính `operationKey` backend trả từ dry-run preview. PATCH master data dùng `expectedUpdatedAt`.

Ảnh khách hàng dùng canonical prepare → presigned PUT → finalize. Presigned URL đi qua kênh HTTP riêng không có request logger để không ghi query ký.

Các lịch sử đơn bán, thu tiền, công nợ chi tiết, giao/trả và mua hàng thuộc domain nghiệp vụ sau; Lô 3.2 không tự tính hoặc sao chép business logic đó.

Current Công Ty Web không có export riêng trên màn Khách hàng/Nhà cung cấp; export doanh nghiệp thuộc khu Dữ liệu & sao lưu / Office Data Exchange, nên Desktop không tự chế endpoint export riêng ở Lô 3.2.

Lô 3.2 được triển khai trên contract đã audit; baseline parity hiện hành được cập nhật theo các mục Rebaseline bên dưới sau mỗi lần source Công Ty thay đổi và được kiểm tra thực tế.


## Rebaseline 2026-09-14 — Công Ty sales search latency fix

Baseline cập nhật sang `NPP-Platform/main@c1d2b6ab4db89c271d760001fd46369d26a676fc`.

So với `8542511deb5452a8c84afefacde7940d39e25134`:
- Công Ty Web thay đổi `npp-core/web/app/sales/sales-orders/SalesOrderCommercialForm.tsx` để sửa độ trễ tìm hàng/lập đơn.
- Backend thay đổi ở service/repository/migration index phục vụ tối ưu tìm SKU; không đổi API route registry.
- Không thêm/xóa screen, web route, API route source, permission hoặc mutation candidate.
- Inventory parity giữ nguyên: 72 screens, 280 web routes, 87 API source files, 385 endpoint candidates, 205 permissions, 277 mutation candidates.
- Chỉ `webAppTree` đổi từ `c67060097a6bce1b24768edb71b6d6f9289db837` sang `6504583a5411c00c76428561062467f500e14510`.


## Rebaseline 2026-09-14 — chính sách lô và migration 136

Baseline cập nhật từ `NPP-Platform/main@bc18e220d10e668a1d765859d6f7d7c3921bbd7f` sang `NPP-Platform/main@ead5ba76840e7dcc7b61500c90c38cef83b6d6e2` sau khi Desktop CI phát hiện drift.

Audit diff nguồn xác nhận:
- Công Ty Web chỉ sửa implementation các màn hiện hữu `inventory/tracking-policies` và `products/product-quick-setup`; không thêm/xóa `page.tsx` hoặc `route.ts`.
- Backend chỉ đổi repository/migration phục vụ chính sách theo dõi lô; không đổi `npp-core/api/src/routes`, server route registry, permission catalog, shared contracts hoặc canonical idempotency implementation.
- Workflow migration production 136 và migration dữ liệu không tạo thêm Desktop business contract.
- Inventory parity giữ nguyên: 72 screens, 281 web routes, 88 API source files, 386 endpoint candidates, 205 permissions, 278 mutation candidates.
- Vì identity snapshot không đổi, chỉ `webAppTree` được rebaseline từ `05f27245b514c722cbd0838ec2ac8a01a234fa52` sang `262c46f9f1e5c2cae5a04c45bdada02dc5c67323`; các fingerprint API/permission/contract/idempotency giữ nguyên.



## Rebaseline 2026-09-14 — Retail batch price và Kho stale version

Baseline cập nhật từ `NPP-Platform/main@ead5ba76840e7dcc7b61500c90c38cef83b6d6e2` sang `NPP-Platform/main@33e688b733ba7d69b2284a7fe22672c01120b791` sau khi PR CI phát hiện drift.

Audit diff nguồn xác nhận:
- Backend thêm đúng một endpoint mới **POST `/api/retail/prices`** trong `npp-core/api/src/routes/retail-catalog.js` để Retail tính giá theo lô; endpoint này thuộc Retail, không thay đổi contract Đơn bán hàng Desktop Lô 4.
- Công Ty Web sửa `organization/warehouses/warehouse-workspace.tsx` để phục hồi sau `STALE_VERSION`; không thêm/xóa screen hay Next route.
- Permission catalog, server route registry, shared contracts và canonical idempotency implementation không đổi.
- Inventory mới: 72 screens, 281 web routes, 88 API source files, **387 endpoint candidates**, 205 permissions, **279 mutation candidates**.
- Rebaseline: `webAppTree` 262c46f9f1e5c2cae5a04c45bdada02dc5c67323 → c8e51673894816c9e4c0a9ad32a2a9007acd37ca; `apiRoutesTree` 766be926c7340fa675b81182ff23f47393accc1b → 3d0aaf0b1ae4d30096f79524cf1f04d511a93f75; endpoint/mutation identity snapshots cập nhật theo report CI #123.



## UI/UX business-layout parity

UI parity is governed by `docs/UI_PARITY_STANDARD.md`.

The Web remains the business-layout reference: Desktop must preserve section/tab order, grouping, primary actions and list-to-detail flow while using native WPF controls for better office ergonomics. Pixel copying is not required; losing or hiding Web business workflow is not allowed.

## Rebaseline 2026-09-16 — Sales reporting customer-group semantics

Baseline backend route tree được audit lại từ `NPP-Platform/main@4325511b0c9ff82a2513a6e1cf3cfe2ac280b931` sang `main@4f468b5e2533debce7199433d99a8f6b10071d44` sau khi Desktop CI #327 phát hiện drift.

Audit diff xác nhận:
- Trong `npp-core/api/src/routes` chỉ có `reporting-sales.js` thay đổi.
- Thay đổi chỉ điều chỉnh ngữ nghĩa phân tích **Nhóm khách hàng** trong báo cáo bán hàng: dùng nhóm hiện tại của khách hàng cho phân tích, đồng thời giữ snapshot lịch sử để truy vết/audit.
- Không thêm/xóa backend route source, endpoint candidate, permission hoặc mutation candidate.
- Inventory parity vẫn giữ đúng **88 API source files / 387 endpoint candidates / 205 permissions / 279 mutation candidates**.
- Shared contracts và canonical Idempotency-Key không đổi.
- Rebaseline chỉ cập nhật fingerprint `apiRoutesTree`; không thay đổi Desktop contract của UI-3.9 Chính sách lô.



## Rebaseline 2026-09-16 — Điều chỉnh tồn: màn và route xuất dữ liệu

Baseline Web được audit lại khi `NPP-Platform/main` tiến tới `13e2b94a1d24956082500e0f4e9e13aefab2a242` trong lúc CI UI-5.2 chạy.

Audit compare từ `0f645e584378e6763720594bd3604828644cb583` xác nhận thay đổi chỉ thuộc `inventory/adjustments` và in kiểm kê:
- thêm `inventory/adjustments/export/page.tsx`;
- thêm Next route xuất dữ liệu Điều chỉnh tồn;
- không chạm `sales/gross-margin`, gross-margin gateway/backend route, permission catalog hoặc idempotency contract;
- API/permission/mutation inventory giữ nguyên 88 / 388 / 205 / 280.

Snapshot Web mới: 73 screens / 286 routes. Chỉ Web screen/route fingerprint được rebaseline; UI-5.2 không đổi contract.
