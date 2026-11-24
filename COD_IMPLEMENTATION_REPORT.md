# COD (Cash on Delivery) Implementation Report

## Tổng quan
Đã implement cơ chế thanh toán COD (Cash on Delivery) cho PrintGrid, cho phép khách hàng thanh toán 50% balance khi nhận hàng thay vì phải trả trước khi ship.

## Thay đổi chính

### 1. Domain Layer - Order Entity
**File:** `src/PrintGrid.Modules/Customer/PrintGrid.Modules.Customer.Domain/Entities/Order.cs`

Thêm các field và method mới:
- `PaymentMethod` (string, default "COD") - Phương thức thanh toán
- `IsCodConfirmed` (bool) - Khách đã xác nhận sẽ nhận hàng chưa
- `CodConfirmationRequestedAtUtc` (DateTime?) - Thời điểm gửi yêu cầu xác nhận
- `CodConfirmationDeadlineUtc` (DateTime?) - Deadline 24h để khách xác nhận
- `CodConfirmationRequestCount` (int) - Số lần đã gửi yêu cầu

Thêm các method:
- `RequestCodConfirmation()` - Gửi yêu cầu xác nhận COD (set deadline 24h)
- `ConfirmCodReceipt(bool accepted)` - Khách xác nhận接受/từ chối
- `IsCodConfirmationRequired()` - Kiểm tra có cần xác nhận không
- `IsCodConfirmationExpired()` - Kiểm tra quá hạn chưa
- `CancelDueToCodTimeout()` - Auto-cancel khi quá hạn

### 2. Application Layer - Command Handlers
**File:** `src/PrintGrid.Modules/Customer/PrintGrid.Modules.Customer.Application/Commands/Shipment/ConfirmCodReceiptCommand.cs` (MỚI)

Tạo 2 commands:
- `ConfirmCodReceiptCommand` - Khách xác nhận接受/từ chối COD
- `RequestCodConfirmationCommand` - Hub/Ops gửi yêu cầu xác nhận

### 3. Shipping Gate Updates
**Files:** 
- `src/PrintGrid.Modules/Customer/PrintGrid.Modules.Customer.Application/Commands/Shipment/ShipOrderCommandHandler.cs`
- `src/PrintGrid.Modules/Customer/PrintGrid.Modules.Customer.Application/Commands/Shipment/ConsolidateShipmentCommands.cs`

Thay đổi gate B-02:
- **Trước:** `IsSettled` (phải trả 100%)
- **Sau:** `IsDeposited` + `IsCodConfirmed` (đã đặt cọc + đã xác nhận COD)

### 4. API Layer
**File:** `src/PrintGrid.Api/Controllers/OrdersController.cs`

Thêm 2 endpoints:
- `POST /api/v1/orders/{id}/cod-confirm` - Khách xác nhận COD (accept/reject)
- `POST /api/v1/orders/{id}/request-cod-confirmation` - Hub/Ops gửi yêu cầu xác nhận (yêu cầu RequireHub policy)

**File:** `src/PrintGrid.Api/EventHandlers/QualityCheckCodConfirmationHandler.cs` (MỚI)

Event handler tự động gửi yêu cầu xác nhận COD khi order vào QualityCheck status.

### 5. Infrastructure Layer
**File:** `src/PrintGrid.Modules/Customer/PrintGrid.Modules.Customer.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`

Thêm EF configuration cho các field COD mới.

**File:** `src/PrintGrid.Infrastructure.Shared/Persistence/Migrations/20261124120000_AddCodPaymentFields.cs` (MỚI)

Migration để thêm các column mới vào bảng Orders.

### 6. Documentation Updates
**Files:**
- `documentation/project/requirements/08-Business-Rules.md`
- `documentation/project/requirements/09-Functional-Requirements.md`

Cập nhật:
- **BR-PAY-008:** Từ "balance paid before pack gate" → "balance collected via COD at delivery"
- **BR-PAY-010:** Từ "UNDECIDED" → "deposit forfeited if customer refuses COD without valid reason"
- **FR-CUST-012:** Cập nhật mô tả COD model
- **FR-HUB-006:** Cập nhật "balance settled" → "COD confirmed"

### 7. Test Updates
**File:** `tests/PrintGrid.UnitTests/Customer/ShipmentGateTests.cs`

Cập nhật helper methods để auto-confirm COD trong tests:
- `BuildQualityCheckOrder()` - Auto-confirm COD
- `BuildQualityCheckOrderWithCodPending()` - Helper mới cho tests COD pending

## Luồng hoạt động

```
1. Customer đặt hàng → Thanh toán 50% deposit
   ↓
2. Order → InProduction → QC pass → QualityCheck
   ↓
3. [AUTO] QualityCheckCodConfirmationHandler triggered
   ↓
4. Hệ thống gửi notification yêu cầu khách xác nhận nhận hàng (24h deadline)
   ↓
5. Khách xác nhận "Tôi sẽ nhận hàng"
   ↓
6. Hub ghi vận đơn → Shipping
   ↓
7. Khách nhận hàng + trả 50% balance (COD)
   ↓
8. Customer xác nhận đã nhận hàng → Delivered
```

## Cơ chế bảo vệ Platform

### 1. COD Confirmation Required
- Khách phải xác nhận sẽ nhận hàng trước khi ship
- Deadline 24h để xác nhận
- Nếu không xác nhận → không thể ship

### 2. Deposit Forfeiture Policy (BR-PAY-010)
- Nếu khách từ chối nhận hàng không có lý do chính đáng → mất 50% cọc
- Sau 2+ lần từ chối trong 6 tháng → blacklist, phải thanh toán 100% online

### 3. Auto-Cancel on Timeout
- Nếu quá 24h không xác nhận → order tự động cancel
- Deposit bị forfeit

### 4. Tracking & Audit
- `CodConfirmationRequestCount` theo dõi số lần gửi yêu cầu
- `CodConfirmationRequestedAtUtc` và `CodConfirmationDeadlineUtc` để audit

## Pre-existing Test Failures (Không liên quan đến COD changes)

Có 2 tests fail trước khi implement COD:
1. `QuoteReviewTests.Order_created_from_draft_waits_for_ops_then_moves_to_payment`
2. `GoldenPathR1Tests.Placing_an_order_without_accepting_terms_is_invalid`

Cả 2 đều liên quan đến validation logic cũ, không phải do COD implementation.

## Build Status
✅ Build succeeded
✅ ShipmentGateTests: 10/10 passed
⚠️ UnitTests: 334/336 passed (2 pre-existing failures)

## Next Steps (Recommended)

1. **Frontend Implementation:**
   - Thêm COD confirmation UI trong Customer Portal
   - Hiển thị countdown timer 24h
   - Nút "Xác nhận sẽ nhận hàng" / "Từ chối nhận hàng"

2. **Notification System:**
   - Gửi email/SMS khi order vào QualityCheck
   - Reminder sau 12h nếu chưa xác nhận
   - Final warning trước deadline 2h

3. **Blacklist Mechanism:**
   - Track refusal count per customer
   - Auto-flag customer sau 2+ refusals
   - Require 100% online payment for flagged customers

4. **Ops Dashboard:**
   - Xem danh sách orders đang chờ COD confirmation
   - Manual trigger re-request confirmation
   - Override COD confirmation cho special cases

5. **Integration Tests:**
   - Viết integration tests cho COD flow đầy đủ
   - Test timeout scenarios
   - Test refusal scenarios
