using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

/// <param name="AcceptTerms">FR-CUST-007: the customer must accept the service terms for the
/// order to be created — enforced by the validator, never defaulted server-side.</param>
public record PlaceOrderCommand(
    Guid CustomerId,
    Guid QuoteId,
    string Street,
    string Ward,
    string District,
    string City,
    string PostalCode,
    bool AcceptTerms) : IRequest<Result<OrderDto>>;
