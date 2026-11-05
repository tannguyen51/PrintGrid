using MediatR;
using PrintGrid.Modules.Customer.Domain.Entities;
using PrintGrid.Modules.Customer.Domain.Enums;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public class CreateReprintRequestCommandHandler(
    IOrderRepository orders,
    IReprintRequestRepository requests,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IEmailOutbox emailOutbox)
    : IRequestHandler<CreateReprintRequestCommand, Result<ReprintRequestDto>>
{
    public async Task<Result<ReprintRequestDto>> Handle(CreateReprintRequestCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null || order.CustomerId != command.CustomerId)
            return Result.Failure<ReprintRequestDto>(Error.NotFound("Order", command.OrderId));
        if (order.Status != OrderStatus.Delivered || order.DeliveredAt is null)
            return Result.Failure<ReprintRequestDto>(Error.Conflict("Only delivered orders can be reported."));
        if (DateTime.UtcNow > order.DeliveredAt.Value.AddDays(30))
            return Result.Failure<ReprintRequestDto>(Error.Conflict("The 30-day guarantee window has expired."));
        if (await requests.HasOpenRequestAsync(order.Id, cancellationToken))
            return Result.Failure<ReprintRequestDto>(Error.Conflict("This order already has an active reprint request."));

        var request = ReprintRequest.Create(order.Id, command.CustomerId, command.Reason, command.Description, command.Photos);
        await requests.AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var customer = await customers.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is not null)
        {
            await emailOutbox.QueueAsync(customer.Email, $"[PrintGrid] Đã nhận khiếu nại {order.OrderNumber}",
                $"PrintGrid đã nhận yêu cầu {request.Id} cho đơn {order.OrderNumber}.\n" +
                "Trạng thái hiện tại: Đang xem xét. Chúng tôi sẽ thông báo khi có kết quả.", cancellationToken);
        }

        return Result.Success(Map(request));
    }

    internal static ReprintRequestDto Map(ReprintRequest x) => new(
        x.Id, x.OrderId, x.Reason, x.Description, x.Photos, ToApiStatus(x.Status),
        x.CreatedAt, x.UpdatedAt, x.ResolutionNote);

    private static string ToApiStatus(ReprintRequestStatus status) => status switch
    {
        ReprintRequestStatus.UnderReview => "under_review",
        _ => status.ToString().ToLowerInvariant()
    };

}
