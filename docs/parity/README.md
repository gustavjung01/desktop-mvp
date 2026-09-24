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

## Rebaseline 2026-09-18 — kiểm kê lớn 2.000 dòng và shared contract

Trong lúc CI của slice **Global quick actions Desktop** chạy, `NPP-Platform/main` tiến tới
`d90755933095bc6ec894529eb1135f8cf4686607` (PR #1100: tối ưu kiểm kê lớn đến 2.000 dòng). Desktop đã audit diff nguồn trước khi rebaseline.

Kết luận audit:

- inventory parity không đổi: **73 screens / 289 Web routes / 90 API source files / 389 endpoint candidates / 205 permissions / 281 mutation candidates**;
- `npp-core/api/src/routes`, `npp-core/api/src/server.js`, permission catalog và canonical idempotency implementation không đổi fingerprint;
- Web app tree đổi do implementation hiện hữu, không thêm/xóa screen hoặc Next route;
- shared contracts thêm đúng `STOCKTAKE_MAX_LINES = 2000` trong JS và type declaration, phục vụ giới hạn kiểm kê; không đổi envelope/contract của Sản phẩm, Khách hàng hoặc Đơn bán;
- backend stocktake chuyển sang batch insert và tiếp tục dùng canonical `Idempotency-Key`; thay đổi này không yêu cầu Desktop quick actions sửa backend/API/DB;
- component `global-quick-actions.tsx` hiện hành vẫn giữ đúng ba shortcut `Sản phẩm`, `Khách hàng`, `Tạo đơn bán` và mở bằng `target="_blank"`; Desktop ánh xạ hành vi đó sang modeless WPF Window trong cùng process.

Rebaseline chỉ cập nhật `webAppTree`, hai blob shared contracts và revision audit. Các snapshot identity, API route fingerprint,
permission fingerprint, server fingerprint và idempotency fingerprint giữ nguyên.

## Rebaseline 2026-09-18 — stocktake line details/annotation

Trong khi PR khôi phục navigation Desktop chạy CI, `NPP-Platform/main` tiến tới
`d8baadc5e9447de3d5785b63851997163a3de1b6`.

Audit diff từ baseline trước xác nhận thay đổi chỉ thuộc nghiệp vụ kiểm kê kho: migration 138–139,
repository/service/route stocktake và UI stocktake bổ sung chi tiết/ghi chú dòng. Inventory tổng vẫn giữ
**73 screens / 289 Web routes / 90 API source files / 389 endpoint candidates / 205 permissions / 281 mutation candidates**.
Permission catalog, server blob, contracts và canonical idempotency blob không đổi.

Desktop slice này chỉ khôi phục shell/navigation đã triển khai, không thay backend/API/DB. Rebaseline cập nhật
Web app tree, API routes tree và hai snapshot identity endpoint/mutation theo source hiện hành sau audit.



## Rebaseline 2026-09-18 — stocktake file UX

Baseline parity được audit từ `c93bd323b64aca5df4ecc9aa516e93e73618f369` tới
`NPP-Platform/main@ebbe90ce4559f27e501ad74c48346f09e3e4dcee`.

Diff gồm đúng 3 commit của stocktake file UX và chỉ sửa implementation/test Web hiện hữu:
`StocktakePrintDock.tsx`, `stocktake-workspace.module.css`, `stocktake-workspace.tsx` cùng hai test stocktake.
Không thêm/xóa screen, Next route, backend route source/endpoint candidate, permission hay mutation candidate;
shared contracts, server registry và canonical Idempotency-Key implementation không đổi.
Vì vậy snapshot identity giữ nguyên và chỉ `webAppTree` được rebaseline
`1316b14c83b2cc9744453ff9cfacb9ba8acf7e7e` → `e95285d6207a0ea76468280b9153561b80446959`.


## Rebaseline 2026-09-20 — Tra cứu tồn theo số giữ cấp kho

Baseline parity được audit từ `ebbe90ce4559f27e501ad74c48346f09e3e4dcee` tới
`NPP-Platform/main@1f211f128cc50bd478ef31b3892e172e5554e6bc` trong PR Desktop đồng bộ **Tra cứu tồn kho**.

Audit compare xác nhận:
- `npp-core/web/app` chỉ đổi `inventory/balances/inventory-balances-workspace.tsx`; không thêm/xóa screen hoặc Next route;
- `npp-core/api/src/routes` chỉ đổi `inventory-core.js`, enrich **GET `/api/inventory/balances`** bằng
  `business_on_hand_quantity`, `business_held_quantity`, `business_available_quantity`;
- service `inventory-business-holds.js` chỉ bổ sung lọc `baseVariantIds` để tính hold theo trang; các thay đổi reporting-sales nằm ngoài route surface Tra cứu tồn;
- inventory parity giữ nguyên **73 screens / 289 Web routes / 90 API source files / 389 endpoint candidates / 205 permissions / 281 mutation candidates**;
- permission tree, server registry, shared contracts và canonical Idempotency-Key implementation không đổi;
- endpoint/mutation identity hash đổi do source của route hiện hữu thay đổi, không có endpoint hay mutation mới.

Desktop map contract business-level hiện hành, gom hiển thị theo **Kho + SKU**, giữ dòng vị trí/lô và paging 100 không cắt đôi nhóm.


## Rebaseline 2026-09-21 — Điều chỉnh tồn Web mới; Workforce chỉ ghi nhận planned

Trong lúc PR Desktop đồng bộ **Điều chỉnh tồn** chạy CI, `NPP-Platform/main` đã tiến từ
`1f211f128cc50bd478ef31b3892e172e5554e6bc` tới
`7a6cee4d647b6a639bb8300a6d4dd5beba678045`.

Audit compare xác nhận hai nhóm thay đổi tách biệt:

- **Inventory Adjustment**: Web/backend bổ sung đúng contract mà PR Desktop này đang đồng bộ:
  kho `UNMANAGED` dùng tồn không vị trí, bulk tự chọn reason đối soát canonical,
  giới hạn 2.000 dòng, `reconciliationBatchCode`, snapshot dòng và mẫu in phiếu.
- **Workforce/Nhân sự**: thêm các màn, Next adapters, backend route, permission và mutation mới.
  Theo phạm vi đã khóa với Owner, PR Desktop này **không triển khai Workforce**; các phần tử mới chỉ được
  đưa vào baseline parity với trạng thái mặc định `planned` để gate tiếp tục phát hiện drift ở các PR sau.
- Ngoài ra có thay đổi deployment/migration runtime; không tạo thay đổi Desktop runtime trong PR này.
- Canonical idempotency implementation giữ nguyên blob
  `faba39af51fd81d0b8d778b20f669a17825da3f7`; retry cùng logical mutation trên Desktop vẫn reuse key hiện có.
- Shared `packages/contracts/index.js` đổi để công bố giới hạn bulk inventory adjustment; type declaration và
  canonical idempotency source không đổi.

Snapshot hiện hành sau audit: **81 screens / 316 Web routes / 91 API source files /
410 endpoint candidates / 223 permissions / 302 mutation candidates**.

Rebaseline này chỉ cập nhật metadata parity theo source đã audit; không mang nghiệp vụ Nhân sự,
backend, DB hay migration vào Desktop.


## Rebaseline 2026-09-22 — Workforce hoàn chỉnh và khung Desktop Lô 0

Baseline cập nhật từ `7a6cee4d647b6a639bb8300a6d4dd5beba678045` sang
`NPP-Platform/main@4186ea9638470d2f89882f51de8fa0aa51347654` sau khi Workforce Web hoàn tất tới chốt lương/phiếu lương.

Audit exact source xác nhận:
- Workforce hiện có 10 màn menu; hai screen mới so với baseline cũ là `/workforce/overtime` và `/workforce/payroll`;
- Web inventory: **83 screens / 331 Web routes**;
- backend inventory: **91 API source files / 421 endpoint candidates / 312 mutation candidates**;
- permission inventory: **233 permissions**;
- route source đổi đúng ở `employees.js` và `workforce.js`; access đổi ở `permissions.js`;
- server registry, shared contracts và canonical Idempotency-Key source không đổi;
- Desktop Lô 0 chỉ tách navigation **Nhân sự** khỏi **Người dùng & phân quyền**, chuyển Danh mục nhân sự hiện có về đúng nhóm và dựng các mục planned cho Lô 2–9;
- không thêm Desktop business API/mutation, không sửa Web/backend/DB/migration và không deploy production.


## Rebaseline 2026-09-22 — FACE backend drift sau merge Lô 2

Ngay sau khi PR Desktop #60 merge, `NPP-Platform/main` tiến từ
`4186ea9638470d2f89882f51de8fa0aa51347654` tới
`1fbedf9d5407401ae930880276ff1892008a9048` với commit **FACE backend**.

Push-CI của Desktop `main` vì thế bắt đúng parity drift mới:
- API source files: **91 → 92**;
- endpoint candidates: **421 → 436**;
- mutation candidates: **312 → 322**.

Audit diff xác nhận thay đổi Web mới chỉ chạm FACE attendance/backend:
`157_workforce_face_attendance.sql`, `workforce-face.js`, registry `server.js`,
service/repository FACE và test tương ứng. Không đổi:
- Web app tree (**83 screens / 331 Web routes**);
- permission tree (**233 permissions**);
- shared contracts;
- canonical Idempotency-Key implementation;
- contract `employees` / `employee-organization` mà Desktop Lô 2 đang dùng.

Rebaseline này chỉ phân loại drift FACE mới là **planned backend surface** theo parity policy hiện hành.
Không triển khai FACE UI/client trong Desktop Lô 2, không sửa Web/backend/DB/migration.


## Rebaseline 2026-09-22 — Workforce FACE UI trong lúc làm Desktop Lô 3

Baseline cập nhật từ `1fbedf9d5407401ae930880276ff1892008a9048` tới
`NPP-Platform/main@9cd5ed9c52932d3b078647e8e754b927af0bd27c` trong task Desktop **Ca / lịch làm việc — Lô 3**.

Audit exact source xác nhận:
- thay đổi sau baseline cũ chỉ gồm retail bottom navigation và FACE UI/rollout;
- `npp-core/web/app` tree đổi do các workspace Chấm công/Chính sách/Bảng công FACE;
- không thêm/xóa page hoặc Next route: snapshot giữ **83 screens / 331 Web routes**;
- backend route tree, permission tree, server registry, shared contracts và canonical Idempotency-Key source không đổi;
- backend snapshot giữ **92 API source files / 436 endpoint candidates / 322 mutation candidates / 233 permissions**;
- contract Lô 3 `/api/workforce/schedules` và `/api/workforce/schedule-planning` không đổi.

Desktop Lô 3 triển khai đúng workspace **Ca / lịch làm việc** từ Web PR #1146:
lịch cá nhân, ca mẫu, lịch tuần, ngày lễ/ngày nghỉ và xếp hàng loạt.
FACE không được kéo vào phạm vi Lô 3 này.

Không sửa Web/backend/DB/migration và không deploy production.


## Rebaseline 2026-09-23 — pricing/workforce Web drift trong lúc sửa updater

Trong lúc PR Desktop sửa lỗi updater file staging bị khóa chạy CI, `NPP-Platform/main` đã tiến từ
`4c9d6652d900f883f8c6cf07316dd46fee8715bf` tới
`efa215eda16ead7dfa6480fb5d11ffd9f37bccd1`.

Audit compare 24 commit xác nhận:
- `npp-core/web/app` đổi ở workspace **Pricing** và **Workforce** hiện hữu; không thêm/xóa `page.tsx` hoặc Next `route.ts`, nên snapshot giữ nguyên **83 screens / 331 Web routes**;
- Pricing bổ sung luồng điều chỉnh giá theo thời điểm trên service hiện hữu (`replaceFrom` / `applyAt`) và chỉnh UX popup; đây là thay đổi nghiệp vụ của Pricing, không thuộc phạm vi PR updater và phải được xử lý ở lô parity Pricing riêng;
- Workforce chủ yếu chuẩn hóa ngôn ngữ văn phòng trên các màn hiện hữu. Sửa FACE ngày 23/09 chỉ điều chỉnh service/repository xác thực credential cũ; không đổi backend route tree hay permission surface;
- các commit Retail/Ordering AI nằm ngoài Công Ty Desktop;
- fingerprint backend route tree, permission tree, server registry, shared contracts và canonical Idempotency-Key source đều **không đổi** so với baseline trước.

Vì vậy PR updater chỉ rebaseline fingerprint `webAppTree`
`4b9df64b76532a790f2ffcd7ccf166474dc13f10` →
`e5b6a7b90fc748c8477e6269e1bdfbab6db8fecc`, không tự kéo Pricing/Workforce feature vào phạm vi updater,
không sửa Web/backend/DB/migration và không deploy production.


## Rebaseline 2026-09-24 — Workforce attendance method UI drift trong lúc sửa Desktop shell

Trong lúc PR Desktop sửa collision workspace chạy CI, `NPP-Platform/main` đã tiến từ
`efa215eda16ead7dfa6480fb5d11ffd9f37bccd1` tới
`921ba8ccf50341a5078794eb5403b40231e57d32`.

Audit compare 4 commit xác nhận:
- Web chỉ sửa các workspace Workforce hiện hữu: Chấm công, Điều chỉnh công và Chính sách làm việc;
- không thêm/xóa `page.tsx` hoặc Next `route.ts`, nên snapshot giữ nguyên **83 screens / 331 Web routes**;
- backend thay đổi service/migration 158 cho tổ hợp phương thức chấm công, nhưng không đổi `npp-core/api/src/routes`, permission catalog, server registry, shared contracts hoặc canonical Idempotency-Key source;
- PR Desktop hiện tại chỉ sửa shell routing/index collision, không kéo nghiệp vụ Chấm công/Chính sách mới vào phạm vi.

Vì vậy chỉ rebaseline `webAppTree`
`e5b6a7b90fc748c8477e6269e1bdfbab6db8fecc` →
`d59ee4eb239e207079c292685c479ebbd336bf0b`.


## Rebaseline 2026-09-24 — Workforce policy onboarding drift trong lúc sửa scroll Danh mục nhân sự

Trong lúc PR Desktop sửa bố cục cuộn của **Danh mục nhân sự**, `NPP-Platform/main` tiến từ
`921ba8ccf50341a5078794eb5403b40231e57d32` tới
`9e2cd0eaaaf3f6bad53d8fefe229afe9fd37cb7c` qua PR Web #1173.

Audit compare exact 1 commit xác nhận:
- chỉ đổi các workspace Web hiện hữu `workforce/employees` và `workforce/policies` cùng test/CSS;
- không thêm/xóa `page.tsx` hoặc Next `route.ts`, nên snapshot giữ nguyên **83 screens / 331 Web routes**;
- không đổi backend route tree, permission catalog, server registry, shared contracts hoặc canonical Idempotency-Key source;
- thay đổi Web bổ sung luồng áp dụng chính sách làm việc ngay hoặc theo ngày; đây là nghiệp vụ Workforce riêng, không thuộc phạm vi PR chỉ sửa scroll của Danh mục nhân sự.

Vì vậy PR này chỉ rebaseline `webAppTree`
`d59ee4eb239e207079c292685c479ebbd336bf0b` →
`5a8a22c45a0580155df2a3c99df49601703c06e1`,
không tự kéo thêm nghiệp vụ Chính sách làm việc vào Desktop, không sửa Web/backend/DB/migration.
