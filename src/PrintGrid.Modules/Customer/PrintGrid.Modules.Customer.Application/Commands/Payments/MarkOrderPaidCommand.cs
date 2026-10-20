using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Payments;

/// <summary>
/// FR-CUST-012 / BR-PAY-007/008 — ops records a received payment. When the amount is omitted the
/// full remaining balance is applied. Crossing the deposit threshold confirms the order and raises
/// the confirmation event (jobs only start after the deposit).
/// </summary>
public sealed record MarkOrderPaidCommand(Guid OrderId, decimal? Amount) : IRequest<Result<OrderPaymentDto>>;

public sealed class MarkOrderPaidCommandHandler : IRequestHandler<MarkOrderPaidCommand, Result<OrderPaymentDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public MarkOrderPaidCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderPaymentDto>> Handle(MarkOrderPaidCommand command, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderPaymentDto>(Error.NotFound("Order", command.OrderId));

        var remaining = order.TotalPrice.Amount - order.PaidAmount;
        var amount = command.Amount ?? remaining;
        if (amount <= 0m)
            return Result.Failure<OrderPaymentDto>(Error.Validation("Đơn hàng đã được thanh toán đầy đủ."));

        var reference = $"MANUAL-{Guid.NewGuid():N}";
        var applied = order.ApplyPayment(amount, reference);
        if (applied.IsFailure)
            return Result.Failure<OrderPaymentDto>(applied.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PaymentMapping.ToDto(order));
    }
}

internal static class PaymentMapping
{
    internal static OrderPaymentDto ToDto(Domain.Entities.Order order) => new(
        order.TotalPrice.Amount,
        order.DepositAmount,
        order.PaidAmount,
        Math.Max(0m, order.TotalPrice.Amount - order.PaidAmount),
        order.IsDeposited,
        order.IsSettled,
        order.TotalPrice.Currency);
}
