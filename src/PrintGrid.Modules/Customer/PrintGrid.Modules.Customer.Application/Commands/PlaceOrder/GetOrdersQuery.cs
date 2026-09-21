using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public record GetOrdersQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<OrderDto>>>;