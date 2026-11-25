using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private init; }
    public Guid QuoteId { get; private set; }
    public string OrderNumber { get; private init; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public DateOnly PromisedDeliveryDate { get; private set; }
    public DateOnly? RequestedDeliveryDate { get; private set; }
    public string DispatchMode { get; private set; } = "Manual";
    public Address DeliveryAddress { get; private init; } = null!;
    public DateTime CreatedAt { get; private init; }
    public DateTime? ConfirmedAt { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public bool IsDelayed { get; private set; }

    /// <summary>
    /// Warranty anchor — the moment the CUSTOMER confirms receipt (30-day guarantee starts here).
    /// The hub's physical "delivered" handover deliberately does NOT stamp this (rule 2).
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>Carrier tracking number captured by the hub when it ships the order (FR-HUB / BR: required).</summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// Contract number signed with the customer. Generated exactly once when the order is
    /// confirmed, one per order (derived from the immutable id, so it is race-free).
    /// </summary>
    public string? ContractNumber { get; private set; }

    /// <summary>True when the customer accepted the terms at placement (FR-CUST-007 evidence).</summary>
    public bool TermsAccepted { get; private set; }

    /// <summary>Timestamp of the terms acceptance recorded with the order.</summary>
    public DateTime? TermsAcceptedAtUtc { get; private set; }

    /// <summary>The customer who confirmed receipt — pairs with <see cref="DeliveredAt"/> (who/when).</summary>
    public Guid? ReceiptConfirmedBy { get; private set; }

    /// <summary>Timestamp when the order entered QualityCheck / became ready at Hub for 24h consolidation window (FR-HUB-006 / BR-LOG-001).</summary>
    public DateTime? ReadyForPackingAtUtc { get; private set; }

    /// <summary>Shared package identifier when multiple orders of the same customer+address are consolidated into 1 shipment (FR-HUB-006).</summary>
    public Guid? ConsolidatedShipmentId { get; private set; }

    /// <summary>Total amount refunded on this specific order — strictly bounded by this order's TotalPrice (BR-LOG-002 / AC02).</summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>Deposit required before production starts (FR-CUST-012 / BR-PAY-007).</summary>
    public decimal DepositAmount { get; private set; }

    /// <summary>Amount the customer has actually paid so far (deposit + settlement instalments).</summary>
    public decimal PaidAmount { get; private set; }

    /// <summary>Payment method for the remaining balance: "COD" (Cash on Delivery) or "Online" (pre-paid in full).</summary>
    public string PaymentMethod { get; private set; } = "COD";

    /// <summary>Whether the customer has confirmed they will receive the order for COD payment.</summary>
    public bool IsCodConfirmed { get; private set; }

    /// <summary>When the COD confirmation request was sent to the customer.</summary>
    public DateTime? CodConfirmationRequestedAtUtc { get; private set; }

    /// <summary>Deadline for the customer to confirm COD receipt (24h after request).</summary>
    public DateTime? CodConfirmationDeadlineUtc { get; private set; }

    /// <summary>Number of times COD confirmation was requested (for tracking re-requests).</summary>
    public int CodConfirmationRequestCount { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>FR-CUST-012 / BR-PAY-007: the deposit has been covered, so production may start.</summary>
    public bool IsDeposited => PaidAmount >= DepositAmount;

    /// <summary>FR-CUST-012 / BR-PAY-008: the order is fully paid and may leave the hub.</summary>
    public bool IsSettled => PaidAmount >= TotalPrice.Amount;

    /// <summary>
    /// The customer has been refunded everything they actually paid, so there is nothing left to ship
    /// against. An order that never collected money (PaidAmount = 0) is never "fully refunded".
    /// </summary>
    public bool IsFullyRefunded => PaidAmount > 0m && RefundedAmount >= PaidAmount;

    public void MarkAsDelayed() => IsDelayed = true;

    public void SetReadyForPackingAt(DateTime readyAtUtc) => ReadyForPackingAtUtc = readyAtUtc;

    public Result ApplyApprovedDeliveryDate(DateOnly newDate)
    {
        if (newDate <= PromisedDeliveryDate)
            return Result.Failure(Error.Validation("The approved delivery date must be later than the current promise"));
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict("A closed order cannot change its promised delivery date"));
        PromisedDeliveryDate = newDate;
        IsDelayed = true;
        return Result.Success();
    }

    private Order() { }

    public static Order CreateFromQuote(
        Quote quote,
        Address deliveryAddress,
        string orderNumber,
        decimal depositPercent = 0m,
        bool termsAccepted = true,
        DateTime? termsAcceptedAtUtc = null,
        // Doc §6.1 / §6.2: machine selection context from quote config
        string? techConstraintsJson = null,
        string modelBindingKind = "không",
        Guid? pinnedModelId = null,
        Guid? referenceLineId = null,
        DateOnly? requestedDeliveryDate = null,
        DateOnly? promisedDeliveryDate = null)
    {
        var depositPercentSafe = Math.Clamp(depositPercent, 0m, 100m);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            OrderNumber = orderNumber,
            Status = quote.Status is QuoteStatus.Draft or QuoteStatus.PendingReview
                ? OrderStatus.AwaitingApproval
                : OrderStatus.PaymentPending,
            TotalPrice = quote.TotalPrice,
            PromisedDeliveryDate = promisedDeliveryDate ?? quote.PromisedDeliveryDate,
            RequestedDeliveryDate = requestedDeliveryDate,
            DispatchMode = PrintGrid.SharedKernel.Interfaces.OrderRoutingPolicy.RequiresManualDispatch(quote.Items.Sum(i => i.Quantity)) ? "Manual" : "Automatic",
            DeliveryAddress = deliveryAddress,
            CreatedAt = DateTime.UtcNow,
            IsDelayed = false,
            TermsAccepted = termsAccepted,
            TermsAcceptedAtUtc = termsAccepted ? (termsAcceptedAtUtc ?? DateTime.UtcNow) : null,
            DepositAmount = decimal.Round(
                quote.TotalPrice.Amount * depositPercentSafe / 100m,
                2,
                MidpointRounding.AwayFromZero)
        };

        foreach (var quoteItem in quote.Items)
        {
            var item = OrderItem.FromQuoteItemWithBinding(order.Id, quoteItem,
                modelBindingKind, pinnedModelId, referenceLineId);
            if (item.TechConstraintsJson is null && techConstraintsJson is not null) item.FreezeTechConstraints(techConstraintsJson);
            order._items.Add(item);
        }

        return order;
    }

    public Result ApplyApprovedQuote(Quote quote, DateOnly? promisedDeliveryDate = null)
    {
        if (Status != OrderStatus.AwaitingApproval || quote.Id != QuoteId || quote.Status != QuoteStatus.Ready)
            return Result.Failure(Error.Conflict("Order is not awaiting approval for this quote"));

        var depositPercent = TotalPrice.Amount <= 0m ? 0m : DepositAmount / TotalPrice.Amount;
        TotalPrice = quote.TotalPrice;
        PromisedDeliveryDate = promisedDeliveryDate ?? quote.PromisedDeliveryDate;
        DepositAmount = decimal.Round(
            quote.TotalPrice.Amount * depositPercent, 2, MidpointRounding.AwayFromZero);
        Status = OrderStatus.PaymentPending;
        return Result.Success();
    }

    /// <summary>
    /// Deterministic, race-free contract number: derived from the immutable order id and the
    /// confirmation date, so two concurrent confirmations cannot collide. The unique filtered
    /// index on ContractNumber is the database backstop.
    /// </summary>
    private static string BuildContractNumber(Guid orderId, DateTime confirmedAtUtc) =>
        $"PG-CT-{confirmedAtUtc:yyyyMMdd}-{orderId.ToString("N")[..12].ToUpperInvariant()}";

    /// <summary>
    /// Records a customer payment (FR-CUST-012 / BR-PAY-007/008). The first payment that covers the
    /// deposit confirms the order and raises <see cref="OrderConfirmedEvent"/> — jobs are only created
    /// once the deposit is in, never on placement alone.
    /// </summary>
    /// <param name="amount">Amount received. Must be positive and may not exceed the remaining balance.</param>
    /// <param name="paymentReference">Gateway/manual reference stored on the order for reconciliation.</param>
    public Result ApplyPayment(decimal amount, string? paymentReference = null)
    {
        if (Status == OrderStatus.AwaitingApproval)
            return Result.Failure(Error.Conflict("Đơn đang chờ Ops duyệt, chưa thể ghi nhận thanh toán."));
        if (amount <= 0m)
            return Result.Failure(Error.Validation("Số tiền thanh toán phải lớn hơn 0."));

        if (Status == OrderStatus.Cancelled)
            return Result.Failure(Error.Conflict($"Đơn hàng {OrderNumber} đã bị huỷ, không thể ghi nhận thanh toán."));

        var remaining = TotalPrice.Amount - PaidAmount;
        if (amount > remaining)
        {
            return Result.Failure(Error.Conflict(
                $"Số tiền thanh toán ({amount:N0} {TotalPrice.Currency}) vượt quá số tiền còn lại của đơn {OrderNumber} ({remaining:N0} {TotalPrice.Currency})."));
        }

        if (!string.IsNullOrWhiteSpace(paymentReference))
            PaymentTransactionId = paymentReference.Trim();

        PaidAmount += amount;

        if (Status == OrderStatus.PaymentPending && PaidAmount >= DepositAmount)
        {
            var reference = string.IsNullOrWhiteSpace(paymentReference)
                ? $"PAYMENT-{Guid.NewGuid():N}"
                : paymentReference.Trim();
            var confirmed = ConfirmPayment(reference);
            if (confirmed.IsFailure)
                return confirmed;
        }

        return Result.Success();
    }

    public Result ConfirmPayment(string paymentTransactionId)
    {
        if (Status != OrderStatus.PaymentPending)
            return Result.Failure(Error.Conflict($"Order {OrderNumber} is already past payment"));

        Status = OrderStatus.Confirmed;
        PaymentTransactionId = paymentTransactionId;
        ConfirmedAt = DateTime.UtcNow;
        ContractNumber ??= BuildContractNumber(Id, ConfirmedAt.Value);

        AddDomainEvent(new OrderConfirmedEvent(
            Id,
            _items.Select(item => new OrderConfirmedItem(
                item.Id,
                item.ModelId,
                item.Quantity,
                item.Configuration.MaterialCode,
                item.Configuration.ColorCode,
                item.Configuration.LayerHeightMm,
                item.EstimatedPrintMinutes,
                item.EstimatedMaterialGrams,
                item.BoundingWidthMm,
                item.BoundingDepthMm,
                item.BoundingHeightMm,
                item.CalculationJson,
                item.QuotedMachineId,
                item.Configuration.ToleranceMm)).ToList(),
            PromisedDeliveryDate,
            DispatchMode == "Automatic"));

        return Result.Success();
    }

    public Result TransitionTo(OrderStatus next)
    {
        var allowed = Status switch
        {
            OrderStatus.Confirmed => next is OrderStatus.InProduction or OrderStatus.Cancelled,
            OrderStatus.InProduction => next is OrderStatus.QualityCheck or OrderStatus.Cancelled,
            OrderStatus.QualityCheck => next is OrderStatus.Shipping or OrderStatus.InProduction,
            OrderStatus.Shipping => next is OrderStatus.Delivered,
            _ => false
        };

        if (!allowed)
            return Result.Failure(Error.Conflict($"Cannot move order from {Status} to {next}"));

        Status = next;
        if (next == OrderStatus.QualityCheck)
        {
            ReadyForPackingAtUtc ??= DateTime.UtcNow;
        }
        return Result.Success();
    }

    /// <summary>
    /// Requests COD confirmation from the customer. Called when order enters QualityCheck status.
    /// Sets a 24-hour deadline for the customer to confirm they will receive the order.
    /// </summary>
    public Result RequestCodConfirmation()
    {
        if (Status != OrderStatus.QualityCheck)
            return Result.Failure(Error.Conflict("COD confirmation can only be requested when order is in QualityCheck status."));

        if (IsCodConfirmed)
            return Result.Success(); // Already confirmed

        var now = DateTime.UtcNow;
        CodConfirmationRequestedAtUtc = now;
        CodConfirmationDeadlineUtc = now.AddHours(24);
        CodConfirmationRequestCount++;
        return Result.Success();
    }

    /// <summary>
    /// Customer confirms or rejects COD receipt. If rejected, the deposit is forfeited per BR-PAY-010.
    /// </summary>
    public Result ConfirmCodReceipt(bool accepted, Guid? confirmedBy = null)
    {
        if (!string.Equals(PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Conflict("This order does not use COD payment method."));

        if (Status != OrderStatus.QualityCheck && Status != OrderStatus.Shipping)
            return Result.Failure(Error.Conflict("COD confirmation can only be processed in QualityCheck or Shipping status."));

        if (accepted)
        {
            IsCodConfirmed = true;
            return Result.Success();
        }
        else
        {
            // Customer rejected — deposit is forfeited per BR-PAY-010
            return Result.Failure(Error.Conflict(
                $"Customer rejected COD receipt for order {OrderNumber}. Deposit of {DepositAmount:N0} {TotalPrice.Currency} is forfeited per BR-PAY-010."));
        }
    }

    /// <summary>
    /// Checks if COD confirmation is required (COD payment + not yet confirmed + deadline not expired).
    /// </summary>
    public bool IsCodConfirmationRequired()
    {
        if (!string.Equals(PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            return false;
        if (IsCodConfirmed)
            return false;
        if (CodConfirmationDeadlineUtc.HasValue && DateTime.UtcNow > CodConfirmationDeadlineUtc.Value)
            return false; // Deadline expired, order should be auto-cancelled
        return true;
    }

    /// <summary>
    /// Checks if the COD confirmation deadline has passed without customer confirmation.
    /// </summary>
    public bool IsCodConfirmationExpired()
    {
        if (!string.Equals(PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            return false;
        if (IsCodConfirmed)
            return false;
        if (!CodConfirmationDeadlineUtc.HasValue)
            return false;
        return DateTime.UtcNow > CodConfirmationDeadlineUtc.Value;
    }

    /// <summary>
    /// Auto-cancels the order due to COD confirmation timeout. Forfeits deposit per BR-PAY-010.
    /// </summary>
    public Result CancelDueToCodTimeout()
    {
        if (!IsCodConfirmationExpired())
            return Result.Failure(Error.Conflict("COD confirmation has not expired yet."));

        var transition = TransitionTo(OrderStatus.Cancelled);
        if (transition.IsFailure)
            return transition;

        return Result.Success();
    }

    /// <summary>
    /// Checks whether this order can be consolidated into one single package + waybill with <paramref name="other"/>
    /// (FR-HUB-006 / BR-LOG-001: same customer + same delivery address + both in QualityCheck + ready within 24 hours).
    /// </summary>
    public bool CanConsolidateWith(Order other, TimeSpan? maxReadyWindow = null)
    {
        if (Id == other.Id) return false;
        if (Status != OrderStatus.QualityCheck || other.Status != OrderStatus.QualityCheck) return false;
        if (CustomerId != other.CustomerId) return false;
        if (!Equals(DeliveryAddress, other.DeliveryAddress)) return false;

        var window = maxReadyWindow ?? TimeSpan.FromHours(24);
        var thisReady = ReadyForPackingAtUtc ?? ConfirmedAt ?? CreatedAt;
        var otherReady = other.ReadyForPackingAtUtc ?? other.ConfirmedAt ?? other.CreatedAt;

        return Math.Abs((thisReady - otherReady).TotalHours) <= window.TotalHours;
    }

    /// <summary>
    /// Hub goods-out. Requires a tracking number (BR: bắt buộc có vận đơn) and can only run on a
    /// quality-checked order; moves it to <see cref="OrderStatus.Shipping"/> and stores the carrier code.
    /// </summary>
    public Result Ship(string? trackingNumber, Guid? consolidatedShipmentId = null)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Result.Failure(Error.Validation("Tracking number is required to ship the order."));

        var moved = TransitionTo(OrderStatus.Shipping);
        if (moved.IsFailure)
            return moved;

        TrackingNumber = trackingNumber.Trim();
        ConsolidatedShipmentId = consolidatedShipmentId;
        return Result.Success();
    }

    /// <summary>
    /// Refunds an amount strictly within the scope of this original order (BR-LOG-002 / AC02).
    /// Even if the order was shipped in a consolidated package with other orders, the refundable
    /// ceiling is strictly this order's <see cref="TotalPrice"/>.
    /// </summary>
    public Result RefundWithinOriginalOrderScope(decimal amount)
    {
        if (amount <= 0m)
            return Result.Failure(Error.Validation("Số tiền hoàn phải lớn hơn 0."));

        // The real ceiling is what the customer actually paid — a refund can only give back money
        // that was collected, never the quoted (unpaid) balance.
        if (RefundedAmount + amount > PaidAmount)
        {
            var refundable = Math.Max(0m, PaidAmount - RefundedAmount);
            return Result.Failure(Error.Conflict(
                $"Số tiền hoàn ({amount:N0} {TotalPrice.Currency}) vượt quá số tiền thực thu còn có thể hoàn của đơn {OrderNumber}: " +
                $"đã thu {PaidAmount:N0} {TotalPrice.Currency}, đã hoàn {RefundedAmount:N0} {TotalPrice.Currency}, " +
                $"còn có thể hoàn {refundable:N0} {TotalPrice.Currency}."));
        }

        // Enclosing business ceiling: a refund never exceeds the original order value (BR-LOG-002 / AC02).
        if (RefundedAmount + amount > TotalPrice.Amount)
        {
            return Result.Failure(Error.Conflict(
                $"Số tiền hoàn ({RefundedAmount + amount:N0} {TotalPrice.Currency}) vượt quá giá trị của đơn gốc {OrderNumber} ({TotalPrice.Amount:N0} {TotalPrice.Currency})."));
        }

        RefundedAmount += amount;
        return Result.Success();
    }

    /// <summary>
    /// Hub confirms physical handover of the parcel. Moves the order to <see cref="OrderStatus.Delivered"/>
    /// but deliberately does NOT touch <see cref="DeliveredAt"/> — the warranty clock starts only when the
    /// customer confirms receipt (rule 2).
    /// </summary>
    public Result MarkDelivered() => TransitionTo(OrderStatus.Delivered);

    /// <summary>
    /// Customer confirms they actually received the goods. This is the event that stamps the 30-day
    /// warranty anchor (<see cref="DeliveredAt"/>) and records who confirmed. Only valid after the hub
    /// has marked the order delivered.
    /// </summary>
    public Result ConfirmReceipt(Guid confirmedBy)
    {
        if (Status != OrderStatus.Delivered)
            return Result.Failure(Error.Conflict($"Only a delivered order can be confirmed by the customer (current status: {Status})."));

        DeliveredAt = DateTime.UtcNow;
        ReceiptConfirmedBy = confirmedBy;
        return Result.Success();
    }
}
