using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.Refunds;

public sealed record RefundRequestDto(
    Guid Id,
    Guid OrderId,
    Guid OrderItemId,
    Guid DateChangeRequestId,
    decimal Amount,
    string Currency,
    string PaymentTransactionId,
    string Reason,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>FR-ANAL-005: ops refund queue, optionally filtered by status.</summary>
public sealed record GetRefundRequestsQuery(string? Status = null) : IRequest<Result<IReadOnlyList<RefundRequestDto>>>;

public sealed class GetRefundRequestsQueryHandler
    : IRequestHandler<GetRefundRequestsQuery, Result<IReadOnlyList<RefundRequestDto>>>
{
    private readonly IRefundRequestRepository _refunds;

    public GetRefundRequestsQueryHandler(IRefundRequestRepository refunds) => _refunds = refunds;

    public async Task<Result<IReadOnlyList<RefundRequestDto>>> Handle(
        GetRefundRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = string.IsNullOrWhiteSpace(request.Status) || request.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
            ? await _refunds.GetPendingAsync(cancellationToken)
            : await _refunds.GetByStatusAsync(request.Status, cancellationToken);

        return Result.Success<IReadOnlyList<RefundRequestDto>>(rows.Select(RefundMapping.ToDto).ToList());
    }
}

/// <summary>
/// FR-ANAL-005: ops approves (and executes within the original order's scope) or rejects a pending refund.
/// </summary>
public sealed record ProcessRefundCommand(
    Guid RefundRequestId,
    bool Approve,
    string? Note,
    Guid ActorId) : IRequest<Result<RefundRequestDto>>;

public sealed class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, Result<RefundRequestDto>>
{
    private readonly IRefundRequestRepository _refunds;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessRefundCommandHandler(
        IRefundRequestRepository refunds,
        IOrderRepository orders,
        IUnitOfWork unitOfWork)
    {
        _refunds = refunds;
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RefundRequestDto>> Handle(ProcessRefundCommand command, CancellationToken cancellationToken)
    {
        var refund = await _refunds.GetByIdAsync(command.RefundRequestId, cancellationToken);
        if (refund is null)
            return Result.Failure<RefundRequestDto>(Error.NotFound("RefundRequest", command.RefundRequestId));
        if (refund.Status != "Pending")
            return Result.Failure<RefundRequestDto>(
                Error.Conflict($"Yêu cầu hoàn tiền đã được xử lý (trạng thái hiện tại: {refund.Status})."));

        if (command.Approve)
        {
            var order = await _orders.GetByIdAsync(refund.OrderId, cancellationToken);
            if (order is null)
                return Result.Failure<RefundRequestDto>(Error.NotFound("Order", refund.OrderId));

            // BR-LOG-002 / AC02: the refund ceiling is the original order, never the consolidated package.
            var refunded = order.RefundWithinOriginalOrderScope(refund.Amount);
            if (refunded.IsFailure)
                return Result.Failure<RefundRequestDto>(refunded.Error);

            var approved = refund.Approve();
            if (approved.IsFailure)
                return Result.Failure<RefundRequestDto>(approved.Error);
        }
        else
        {
            var rejected = refund.Reject();
            if (rejected.IsFailure)
                return Result.Failure<RefundRequestDto>(rejected.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(RefundMapping.ToDto(refund));
    }
}

internal static class RefundMapping
{
    internal static RefundRequestDto ToDto(RefundRequest request) => new(
        request.Id,
        request.OrderId,
        request.OrderItemId,
        request.DateChangeRequestId,
        request.Amount,
        request.Currency,
        request.PaymentTransactionId,
        request.Reason,
        request.Status,
        request.CreatedAtUtc);
}
