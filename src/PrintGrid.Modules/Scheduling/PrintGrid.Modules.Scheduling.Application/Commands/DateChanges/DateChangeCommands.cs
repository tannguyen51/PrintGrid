using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.Modules.Scheduling.Application.Services;
using PrintGrid.Modules.Scheduling.Domain.Entities;
using PrintGrid.Modules.Scheduling.Domain.Enums;
using PrintGrid.Modules.Scheduling.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Scheduling.Application.Commands.DateChanges;

public sealed record DateChangeRequestDto(
    string Token, DateOnly OriginalDeliveryDate, DateOnly ProposedDeliveryDate,
    string ReasonCode, string Reason, string Status, DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc);

public sealed record GetDateChangeRequestQuery(string Token, Guid CustomerId)
    : IRequest<Result<DateChangeRequestDto>>;

public sealed record DecideDateChangeCommand(string Token, Guid CustomerId, bool Approve)
    : IRequest<Result<DateChangeRequestDto>>;

public sealed class GetDateChangeRequestQueryHandler(
    IDateChangeRequestRepository dateChanges,
    IOrderRepository orders) : IRequestHandler<GetDateChangeRequestQuery, Result<DateChangeRequestDto>>
{
    public async Task<Result<DateChangeRequestDto>> Handle(GetDateChangeRequestQuery query, CancellationToken ct)
    {
        var request = await dateChanges.GetByTokenAsync(query.Token, ct);
        if (request is null) return Result.Failure<DateChangeRequestDto>(Error.NotFound("Date change request", query.Token));
        var order = await orders.GetByItemIdAsync(request.OrderItemId, ct);
        if (order is null || order.CustomerId != query.CustomerId)
            return Result.Failure<DateChangeRequestDto>(Error.NotFound("Date change request", query.Token));
        return Result.Success(Map(request));
    }

    internal static DateChangeRequestDto Map(DateChangeRequest request) => new(
        request.Token, request.OriginalDeliveryDate, request.ProposedDeliveryDate,
        request.ReasonCode.ToString(), request.Reason, request.Status.ToString(), request.CreatedAtUtc,
        request.DecidedAtUtc);
}

public sealed class DecideDateChangeCommandHandler(
    IDateChangeRequestRepository dateChanges,
    IJobRepository jobs,
    IOrderRepository orders,
    IRefundRequestRepository refunds,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IRequestHandler<DecideDateChangeCommand, Result<DateChangeRequestDto>>
{
    public async Task<Result<DateChangeRequestDto>> Handle(DecideDateChangeCommand command, CancellationToken ct)
    {
        var request = await dateChanges.GetByTokenAsync(command.Token, ct);
        if (request is null) return Result.Failure<DateChangeRequestDto>(Error.NotFound("Date change request", command.Token));
        if (request.Status != DateChangeRequestStatus.Proposed)
            return Result.Failure<DateChangeRequestDto>(Error.Conflict("This date-change request has already been decided"));

        var order = await orders.GetByItemIdAsync(request.OrderItemId, ct);
        if (order is null || order.CustomerId != command.CustomerId)
            return Result.Failure<DateChangeRequestDto>(Error.NotFound("Date change request", command.Token));
        var item = order.Items.SingleOrDefault(x => x.Id == request.OrderItemId);
        var job = await jobs.GetByIdAsync(request.JobId, ct);
        if (item is null || job is null)
            return Result.Failure<DateChangeRequestDto>(Error.Conflict("The order item or production job no longer exists"));

        if (command.Approve)
        {
            var orderChanged = order.ApplyApprovedDeliveryDate(request.ProposedDeliveryDate);
            if (orderChanged.IsFailure) return Result.Failure<DateChangeRequestDto>(orderChanged.Error);
            var internalDate = request.ProposedDeliveryDate.AddDays(-PlacementShortfallService.HubAndTransitBufferDays);
            var jobChanged = job.ApplyApprovedInternalDueDate(internalDate);
            if (jobChanged.IsFailure) return Result.Failure<DateChangeRequestDto>(jobChanged.Error);
            request.Approve(command.CustomerId, clock.UtcNow);
        }
        else
        {
            if (await refunds.ExistsForDateChangeAsync(request.Id, ct))
                return Result.Failure<DateChangeRequestDto>(Error.Conflict("A refund is already queued for this request"));
            request.Reject(command.CustomerId, clock.UtcNow);
            await refunds.AddAsync(RefundRequest.Queue(
                order.Id, item.Id, request.Id, item.LineTotal.Amount, item.LineTotal.Currency,
                order.PaymentTransactionId ?? "PAYMENT-REFERENCE-UNAVAILABLE",
                $"Customer rejected B16 date change {request.Id}: {request.Reason}", clock.UtcNow), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(GetDateChangeRequestQueryHandler.Map(request));
    }
}
