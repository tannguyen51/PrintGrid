using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public sealed record GetReprintRequestsQuery(Guid OrderId, Guid CustomerId)
    : IRequest<Result<IReadOnlyList<ReprintRequestDto>>>;
