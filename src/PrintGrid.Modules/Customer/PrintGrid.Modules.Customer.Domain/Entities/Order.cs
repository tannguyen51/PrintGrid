using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Events;
using PrintGrid.SharedKernel.Common;
using PrintGrid.SharedKernel.Results;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Domain.Entities;

public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Guid QuoteId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public DateOnly PromisedDeliveryDate { get; private set; }
    public Address DeliveryAddress { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
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

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>FR-CUST-012 / BR-PAY-007: the deposit has been covered, so production may start.</summary>
    public bool IsDeposited => PaidAmount >= DepositAmount;

    /// <summary>FR-CUST-012 / BR-PAY-008: the order is fully paid and may leave the hub.</summary>
    public bool IsSettled => PaidAmount >= TotalPrice.Amount;

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
        decimal depositPercent = 0m)
    {
        var depositPercentSafe = Math.Clamp(depositPercent, 0m, 100m);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            OrderNumber = orderNumber,
            Status = OrderStatus.PaymentPending,
            TotalPrice = quote.TotalPrice,
            PromisedDeliveryDate = quote.PromisedDeliveryDate,
            DeliveryAddress = deliveryAddress,
            CreatedAt = DateTime.UtcNow,
            IsDelayed = false,
            DepositAmount = decimal.Round(
                quote.TotalPrice.Amount * depositPercentSafe / 100m,
                2,
                MidpointRounding.AwayFromZero)
        };

        foreach (var quoteItem in quote.Items)
        {
            order._items.Add(OrderItem.FromQuoteItem(order.Id, quoteItem));
        }

        return order;
    }

    /// <summary>
    /// Records a customer payment (FR-CUST-012 / BR-PAY-007/008). The first payment that covers the
    /// deposit confirms the order and raises <see cref="OrderConfirmedEvent"/> — jobs are only created
    /// once the deposit is in, never on placement alone.
    /// </summary>
    /// <param name="amount">Amount received. Must be positive and may not exceed the remaining balance.</param>
    /// <param name="paymentReference">Gateway/manual reference stored on the order for reconciliation.</param>
    public Result ApplyPayment(decimal amount, string? paymentReference = null)
    {
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

        AddDomainEvent(new OrderConfirmedEvent(
            Id,
            CustomerId,
            PromisedDeliveryDate,
            _items.Select(i => new OrderConfirmedItem(
                i.Id,
                i.ModelId,
                i.Quantity,
                i.Configuration.MaterialCode,
                i.Configuration.ColorCode,
                i.Configuration.LayerHeightMm,
                i.Configuration.InfillPercent,
                i.Configuration.ToleranceMm,
                i.EstimatedPrintMinutes,
                i.EstimatedMaterialGrams,
                i.BoundingWidthMm,
                i.BoundingDepthMm,
                i.BoundingHeightMm)).ToList()));

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
