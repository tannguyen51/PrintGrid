using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.ReprintRequests;

public sealed record CreateReprintRequestCommand(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    string Description,
    IReadOnlyList<string> Photos) : IRequest<Result<ReprintRequestDto>>;
