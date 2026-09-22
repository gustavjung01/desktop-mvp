# Parity rebaseline — FACE backend sau Desktop Lô 2

Ngày audit: 2026-09-22.

## Sự cố

PR Desktop #60 đã có exact-head CI xanh ở `f5222a0205e815634dbdb36e3aba90e80d4c6c24`.
Sau merge, push-CI trên Desktop `main@a03c4f5a1d1b226f9fb4d630cf1622f698fb21b5`
checkout `NPP-Platform/main@1fbedf9d5407401ae930880276ff1892008a9048` và fail đúng tại parity baseline.

Nguyên nhân là Web main đã tiến sau exact-head CI của PR #60.

## Drift đã audit

So với baseline `4186ea9638470d2f89882f51de8fa0aa51347654`:

- thêm backend route source `npp-core/api/src/routes/workforce-face.js`;
- đổi `npp-core/api/src/server.js` để đăng ký FACE route;
- thêm migration/repository/service/test FACE;
- `workforce.js` route blob không đổi;
- `npp-core/web/app` tree không đổi;
- `npp-core/api/src/access` tree không đổi;
- `packages/contracts/index.js`, `packages/contracts/index.d.ts` không đổi;
- canonical `npp-core/api/src/idempotency.js` không đổi.

Inventory exact source mới:

- 83 screens;
- 331 Web routes;
- 92 API source files;
- 436 endpoint candidates;
- 233 permissions;
- 322 mutation candidates.

## Phân loại Desktop

FACE backend surface mới được ghi nhận **planned** theo parity policy chung.
Task sửa CI này không triển khai FACE vào Desktop, không thay contract Lô 2,
không thêm mutation runtime Desktop và không sửa Web/backend/DB/migration.

Canonical Idempotency-Key vẫn là nguồn hiện hành:
retry cùng logical operation phải reuse cùng key.
