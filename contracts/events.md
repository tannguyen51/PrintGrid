# Sổ đăng ký sự kiện miền (event registry) — khóa 27/09

Quy tắc: **tên sự kiện và trường dữ liệu trong file này là hợp đồng.**
Nhà sản xuất chỉ được PHÁT đúng tên + đúng payload; tiêu thụ đăng ký tại đây trước khi code.
Đổi bất kỳ dòng nào = sửa file này, PR qua chủ vỏ, rồi mới sửa code.

## Sự kiện ĐÃ TỒN TẠI trong code (đối chiếu 27/09)

| Event (tên phát) | Payload thật | Sinh ra tại | Tiêu thụ hiện tại | Tiêu thụ kế hoạch |
|---|---|---|---|---|
| `CustomerRegisteredEvent` | CustomerId, Email | Customer/auth | ⚠ **chưa có handler** | T1: gửi email xác minh (ghế Nền tảng) |
| `OrderConfirmedEvent` | OrderId, CustomerId, PromisedDeliveryDate, Items[OrderItemId, ModelId, Quantity, MaterialCode, ColorCode, LayerHeightMm, InfillPercent, ToleranceMm] | thanh toán/mark-paid | ✅ `OrderConfirmedEventHandler` (tách job — bản hằng số, T2 thay bằng số thật) | T2 engine; T7 đối soát ghi sổ |
| `JobAssignedEvent` | JobId, LabId, MachineId, PlannedStartUtc, PlannedEndUtc, Score | engine gán việc | ⚠ chưa có | T2: thông báo lab + realtime khách |
| `JobFailedEvent` | JobId, LabId, MachineId, Reason, AttemptNumber | báo sự cố lab (T3) | ⚠ chưa có | T3: `ReschedulingHandler` (ghế Engine) |
| `ReschedulingTriggeredEvent` | JobId, Trigger, InternalDueDate | từ-chối/in-hỏng/chết-máy/quá-hạn-2h | ⚠ **đang phát mà 0 ai nghe** — lỗ hổng lớn nhất | T3: handler vá lịch ≤30s |

## Sự kiện CẦN THÊM theo kế hoạch (chưa có trong code)

| Event đề xuất | Payload tối thiểu | Sinh ra tại | Tiêu thụ |
|---|---|---|---|
| `JobDeclinedEvent` | JobId, LabId, Reason(enum), AttemptNumber | `/jobs/{id}/decline` (T2) | engine (giống JobFailed), trừ acceptance rate |
| `JobCompletedEvent` | JobId, MachineId, ActualMinutes, MaterialGrams, **ProofPhotoKeys[], ProofStatus** | `/jobs/{id}/complete` (T2/T3) | hub (chờ lô), kho (settle BR-STOCK-001), calibration; **phát tiếp `LabProofSubmittedEvent` chờ UC-030 duyệt trước khi handover** |
| `HandoverRecordedEvent` | BatchCode, JobIds[], Carrier, Waybill | `/lab/handover` (T2) | hub receipt |
| `BatchReceiptFinalizedEvent` | BatchCode, Received[], Missing[] | `/hub/batches/.../receipt` (T2) | thông báo lab + ops |
| `InspectionFailedEvent` | JobId, DefectCode, Attribution(LAB\|HUB\|CUSTOMER_GEOMETRY), ChecklistVersionId | inspection (T3) | engine tạo in lại; ghi chi phí |
| `OrderProgressedEvent` | OrderId, NewStatus, DeliveredAt? | chuỗi hub pack/ship/deliver (T2) | SignalR tracking khách + notify |
| `DateChangeRequestedEvent` | OrderId, RequestToken, Reason(enum 3 điều kiện B16) | ops (T4) | link approve của khách |
| `PaymentWebhookReceivedEvent` / `PaymentConfirmedEvent` | OrderId, TxnId, Amount | SEPay webhook / confirm-API (T7) | OrderConfirmed flow — **chỉ event Confirmed mới đổi trạng thái đơn** |

`Trigger` của ReschedulingTriggered (enum chốt): `LAB_DECLINE` · `PRINT_FAILURE` · `MACHINE_BREAKDOWN` · `TIMEOUT_2H` · `REPRINT_INSERTED`.

## Bổ sung theo GVHD Review 1 (30/09) — chờ khóa sau khi nhóm duyệt mã hóa

| Event đề xuất | Payload tối thiểu | Sinh ra tại | Tiêu thụ |
|---|---|---|---|
| `ModelUploadedEvent` ★R1 | CustomerId, ModelId, FileName, StorageKey, **Sha256**, SizeBytes | upload model (FR-CUST-002) | ✅ **ĐÃ CODE (02/10)**: ModelUploadedEmailHandler → shared.email_outbox → Hangfire `email-outbox-delivery` mỗi phút (dev = log; đổi channel ở T7) |
| `FileAckRecordedEvent` ★R1 | JobId, LabId, HashMatched(bool), AckAtUtc | accept offer (FR-LAB-004) | audit chuỗi bàn giao IP (BR-IP-003) |
| `QuoteDraftedEvent` ★R1 | QuoteId, DraftPrice, DraftDate, PlacementBasis, EngineDurationMs | engine (FR-SCHED-005/010) | hàng đợi UC-029 (hoặc auto-lane publish ngay — BR-QUOTE-008) |
| `QuoteApprovedEvent` ★R1 | QuoteId, StaffId, Adjustments[{field,before,after,reason}], PublishedAt | workbench UC-029 | expiry đồng hồ bắt đầu; notify khách |
| `OfferExtendedEvent` ★R1 | JobId, LabIds[top-k], ExpiresAt | engine (BR-ASSIGN-010) | lab inbox; auto-expire |
| `LabProofSubmittedEvent` ★R1 | JobId, PhotoKeys[], SelfReport | complete (BR-QC-011) | queue UC-030 |
| `LabProofApprovedEvent` / `LabProofRejectedEvent` ★R1 | JobId, StaffId, Decision, Reason? | UC-030 | mở khóa handover / trả về lab |
| `PaymentDepositConfirmedEvent` ★R1 | OrderId, TxnId, Amount, DepositPct | deposit leg (FR-CUST-012) | order: PAID_DEPOSIT → decompose + assign (thay vì PaymentConfirmed đủ) |
| `PaymentBalanceConfirmedEvent` ★R1 | OrderId, TxnId, Amount | balance leg trước pack | pack gate BR-QC-008 thêm điều kiện SETTLED |
| `StockTransactionEvent` ★R1 | LabId, TxnCode, Material, Color, DeltaGrams, Reason(JobId/Manual/Adjust), RunningTotal | mọi nhập/xuất (BR-STOCK-003) | ledger + low-stock alert + settleJob |
| `ShipmentBatchSealedEvent` ★R1 | BatchCode, JobIds[], WeightKg, Zone, AllowancePreview | UC-033 | hub receipt trước khớp (UC-016) |
| `ShipmentsMergedEvent` ★R1 | Waybill, OrderIds[], CustomerId | UC-017/FR-HUB-006 | tracking + khiếu nại vẫn theo đơn gốc (BR-LOG-002) |

Quy tắc cũ giữ nguyên: chỉ đổi khi sửa file này + PR. **Các event ★R1 chưa được code — khóa mã khi B19–B24 chốt xong.**
