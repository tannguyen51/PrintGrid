using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

public record PlaceOrderCommand(
    Guid CustomerId,
    Guid QuoteId,
    string Street,
    string Ward,
    string District,
    string City,
    string PostalCode) : IRequest<Result<OrderDto>>;
