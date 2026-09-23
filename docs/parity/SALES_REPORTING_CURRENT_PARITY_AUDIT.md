# Báo cáo bán hàng Desktop — parity Web hiện hành

## Baseline

- Desktop: `gustavjung01/desktop-mvp`, baseline `7e8ac7ef295903bf161b635442480ec812d62e30`.
- Desktop baseline không có PR mở; push-CI run `35797692062` success.
- Web chuẩn: `binhnxwjfjxm/NPP-Platform`, audit trên `main@8b7eb5dce148413b67cce0de47b34e8b214e500a`.
- Luồng Phân tích bắt nguồn từ Web PR #1091; contract hiện hành đã có các sửa tiếp theo trên main.
- Update filter Nhãn hàng nằm trong commit Web `4c9d6652d900f883f8c6cf07316dd46fee8715bf`.

## Parity được bổ sung

- Bỏ bộ lọc Tiền tệ khỏi UI phân tích Desktop; tiền tệ vẫn là thuộc tính số liệu và vẫn hiển thị trong bảng/tổng/xu hướng/export.
- Thêm Nhãn hàng khi chiều đang xem là Sản phẩm.
- GET `/api/reporting/sales` truyền `brandId`; đọc `filters.brandId` và `classification.options.brands`.
- Export danh sách giữ Excel/CSV + chọn cột; Sản phẩm truyền productGroupId/brandId/includeZeroProducts, Khách hàng truyền customerGroupId.
- Export Phân tích chỉ Excel, chọn đúng 2 tiêu chí trong Sản phẩm / Loại khách / Kênh bán / Nhóm hàng.
- Nếu có Sản phẩm thì Sản phẩm luôn là chiều dòng.
- Chọn Doanh thu, Sản lượng hoặc cả hai; hỗ trợ sold/carton/base bằng nhãn vận hành.
- Hỗ trợ sort canonical name-asc, revenue-desc/asc, quantity-desc/asc theo metric được chọn.
- Preview cột dựng từ payload canonical GET hiện hành; Desktop không tính lại doanh thu/sản lượng.
- Export gửi `analysis.<row>.<column>.<revenue|quantity|both>`, các `column`, `quantityDisplay` khi có Sản lượng và `sort`.
- File Excel do backend canonical tạo qua `GET /api/reporting/sales-export`; Desktop chỉ lưu bytes trả về.

## Ranh giới

- Không sửa Web/backend/DB/migration.
- Không deploy production.
- Không thêm nghiệp vụ ngoài contract Web hiện hành.
- Đây là luồng GET/export read-only, không phát sinh mutation hay Idempotency-Key.
