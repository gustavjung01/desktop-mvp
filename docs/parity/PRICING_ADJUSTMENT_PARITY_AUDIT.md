# Pricing Desktop — Điều chỉnh giá parity audit

Baseline Desktop: `b8cc30a627352f5dc4c0c0a57fa7c108694bddf4`.

Web chuẩn đã đổi tab `Bảng giá tổng hợp` thành `Điều chỉnh giá` tại `/pricing?view=all` và bỏ điều hướng nghiệp vụ từ màn này sang `/operations/data-exchange?tab=pricing`.

Nguồn Web đối chiếu:
- `ac53163cc3700905b2739f4afbc479c79951b314` — hoàn thiện điều chỉnh giá theo ngày.
- `efa215eda16ead7dfa6480fb5d11ffd9f37bccd1` — mở rộng workspace popup.

Desktop parity trong lô này:
- giữ nguyên workspace Giá bán và khuyến mãi;
- đổi tab thành `Điều chỉnh giá`;
- điều chỉnh trực tiếp ngay tại màn;
- tải file mẫu và nhập file ngay tại màn;
- hỗ trợ cập nhật ngay hoặc áp dụng từ ngày;
- mutation dùng `/api/pricing/import` với `matchBySku=true`, `replaceFrom=true`, `applyAt`;
- canonical Idempotency-Key được giữ theo fingerprint logical payload; retry cùng payload reuse key, chỉ xóa key sau khi mutation thành công;
- reload đúng bảng giá sau mutation để cập nhật tổng hợp;
- không sửa Web/backend/DB/migration và không deploy production.

Data Exchange vẫn là workspace độc lập cho các nghiệp vụ nhập/xuất khác; tab Điều chỉnh giá không điều hướng sang workspace đó.
