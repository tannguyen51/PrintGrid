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

### Đảm bảo không nh hưởng báo giá/đơn đã chốt
- `Quote.PricingVersion` được freeze tại thời điểm tạo quote; thay đổi cấu hình mới chỉ áp dụng cho quote/order sau đó.

## 2. Chấm điểm lab tự động từ dữ liệu vận hành

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

- `AdminCommercialConfigurationPage`: hiển thị ngày giờ kích hoạt/ hẹn giờ, lịch sử phiên bản, dialog hẹn giờ, và các trưng trọng số + điểm lab mới.
- `AdminScoringConfigurationPage`: hiển thị 6 trọng số, cập nhật form kích hoạt.
- API mới: `GET /api/v1/scheduling/lab-scores` giải thích điểm lab.

## 4. Kiểm thử

- Build API: OK
- Build unit tests: OK (các test do thay đổi này pass)
- Unit test toàn bộ: 333 pass, 3 fail (2 fail pre-existing, không liên quan đến thay đổi này)
- Migrations: `AddCommercialScheduledActivation`, `SplitLabReliabilityScoringWeights`

## 5. Vấn đề chưa giải quyết

- PostgreSQL local đã cài nhưng chưa tạo được database `printgrid_db` vì chưa có mật khẩu user `postgres`.
- Cần user cung cấp mật khẩu PostgreSQL hoặc cho phép cấu hình `pg_hba.conf` trust để khởi tạo DB, sau đó chạy `dotnet ef database update` và test UI deploy.

## Commit

`7b2a5faf` — feat: commercial config activation, scheduled activation, and operational lab scoring
