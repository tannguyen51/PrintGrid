# Báo cáo tiến độ: Cấu hình thương mại và chấm điểm lab

## Tóm tắt

Triển khai các yêu cầu về **kích hoạt cấu hình thương mại** và **chấm điểm lab tự động từ dữ liệu vận hành** trên nhánh `feature/Tan-labscoring`.

## 1. Kích hoạt cấu hình thương mại

### Đã làm
- Thêm `ScheduledActivationAtUtc` vào `CommercialConfigurationVersion`.
- Phân biệt rõ **"kích hoạt ngay"** và **"hẹn giờ kích hoạt"**:
  - Kích hoạt ngay: chuyển `Status = Active`, lưu `ActivatedAtUtc`.
  - Hẹn giờ: giữ `Status = Draft`, lưu `ScheduledActivationAtUtc`, tự động materialize khi đến giờ.
- `CommercialPolicyProvider` lazy-materialize phiên bản đã đến hạn, archive phiên bản cũ trong transaction.
- Kiểm tra duy nhất một phiên bản Active (unique index vẫn giữ nguyên).
- Cập nhật DTO, handler, repository, controller, migration, và frontend UI.

### Đảm bảo không ảnh hưởng báo giá/đơn đã chốt

- `Quote.PricingVersion` được freeze tại thi điểm tạo quote; thay đổi cấu hình mới chỉ áp dụng cho quote/order sau đó.

## 2. Chấm điểm lab tự động t dữ liệu vận hành

### Đã làm
- Tách `LabReliability` thành `OnTimeRate` và `FirstPassYield` trong `ScoringWeights`.
- Cập nhật `AssignmentScorer` sử dụng 6 tiêu chí: slack hạn, đúng hạn, chất lượng lần đầu, cân tải, chi phí, vận chuyển.
- Chuẩn hóa về 0..1, tổng trọng số = 1.
- Lưu `Breakdown` chi tiết từng thành phần vào decision log.
- Tạo `LabScoreService` tính điểm từ `LabMetricsCalculator`.
- Tạo `LabScoreCalculator` với công thức làm mượn `(k × base + n × actual) / (k + n)` cho lab mới.
- Nhãn **"Tạm thời"** khi `completedJobs < ProvisionalThreshold`.
- Lab mới không bị loại vì thiếu lịch sử, cũng không được mặc định chất lượng tuyệt đối.

### Tham số cấu hình cho người vận hành
- `AutoLaneThreshold` (ngưỡng tự duyệt)
- `MaxBatchSize` (giới hạn lô)
- `AssignmentWeights` (trọng số gán việc)
- `LabProvisionalBaseScore` (điểm nền)
- `LabProvisionalWeightK` (trọng số làm mượn k)
- `LabProvisionalThreshold` (ngưỡng chuyển chính thức)

## 3. UI/UX

- `AdminCommercialConfigurationPage`: hiển thị ngày gi kích hoạt/ hẹn giờ, lịch sử phiên bản, dialog hẹn giờ, và các trưng trọng số + điểm lab mới.
- `AdminScoringConfigurationPage`: hiển thị 6 trọng số, cập nhật form kích hoạt.
- `AdminLabScoresPage`: trang mới giải thích điểm lab từ dữ liệu vận hành.
- API mới: `GET /api/v1/scheduling/lab-scores` giải thích điểm lab.

## 4. Kiểm thử

- Build API: OK
- Build unit tests: OK
- Unit test toàn bộ: 333 pass, 3 fail (2 fail pre-existing, không liên quan đến thay đổi này)
- Migrations:
  - `AddCommercialScheduledActivation`
  - `SplitLabReliabilityScoringWeights`
  - `SeedAdminUser`
- Local test:
  - Backend chạy ở `http://localhost:5000`, health `Healthy`
  - Login admin `admin@test.com` / `Admin123!` thành công, JWT có role `Admin`
  - API `GET /api/v1/admin/commercial-configurations` trả về cấu hình thương mại
  - API `GET /api/v1/admin/scoring-configurations` trả về trọng số chấm điểm
  - API `GET /api/v1/scheduling/lab-scores` trả về rỗng (chưa có lab active)

## 5. Lưu ý khi triển khai

- `appsettings.Development.json` đã được đổi sang kết nối PostgreSQL local trên port `5433` (cluster khởi tạo bằng `initdb` trong user profile). Nếu muốn dùng lại PostgreSQL service mặc định ở port `5432`, cần đổi lại connection string.
- Đã tạo user admin `admin@test.com` / `Admin123!` qua migration seed để test local.

## Commit chính

- `7b2a5faf` — feat: commercial config activation, scheduled activation, and operational lab scoring
- `42eaa448` — feat: add lab scores explanation page and route
- `3b0c5960` — chore: seed admin user migration for local testing
