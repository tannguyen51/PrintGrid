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
| `JobCompletedEvent` | JobId, MachineId, ActualMinutes, MaterialGrams | `/jobs/{id}/complete` (T2/T3) | hub (chờ lô), kho (quyết toán), calibration |
| `HandoverRecordedEvent` | BatchCode, JobIds[], Carrier, Waybill | `/lab/handover` (T2) | hub receipt |
| `BatchReceiptFinalizedEvent` | BatchCode, Received[], Missing[] | `/hub/batches/.../receipt` (T2) | thông báo lab + ops |
| `InspectionFailedEvent` | JobId, DefectCode, Attribution(LAB\|HUB\|CUSTOMER_GEOMETRY), ChecklistVersionId | inspection (T3) | engine tạo in lại; ghi chi phí |
| `OrderProgressedEvent` | OrderId, NewStatus, DeliveredAt? | chuỗi hub pack/ship/deliver (T2) | SignalR tracking khách + notify |
| `DateChangeRequestedEvent` | OrderId, RequestToken, Reason(enum 3 điều kiện B16) | ops (T4) | link approve của khách |
| `PaymentWebhookReceivedEvent` / `PaymentConfirmedEvent` | OrderId, TxnId, Amount | SEPay webhook / confirm-API (T7) | OrderConfirmed flow — **chỉ event Confirmed mới đổi trạng thái đơn** |

`Trigger` của ReschedulingTriggered (enum chốt): `LAB_DECLINE` · `PRINT_FAILURE` · `MACHINE_BREAKDOWN` · `TIMEOUT_2H` · `REPRINT_INSERTED`.
