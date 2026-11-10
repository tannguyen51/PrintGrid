using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Commands.PlaceOrder;

/// <param name="AcceptTerms">FR-CUST-007: the customer must accept the service terms for the
/// order to be created — enforced by the validator, never defaulted server-side.</param>
/// <param name="TechConstraintsJson">Section 3: hard tech constraints from customer (JSONB).</param>
/// <param name="ModelBindingKind">Section 4/5: "không" | "theo_model" | "như_đơn_cũ".</param>
/// <param name="PinnedModelId">When kind is "theo_model", the model GUID.</param>
/// <param name="ReferenceLineId">When kind is "như_đơn_cũ", reference order line GUID.</param>
public record PlaceOrderCommand(
    Guid CustomerId,
    Guid QuoteId,
    string Street,
    string Ward,
    string District,
    string City,
    string PostalCode,
    bool AcceptTerms,
    // Doc §6.1 / §6.2: machine selection context carried from quote config
    string? TechConstraintsJson = null,
    string ModelBindingKind = "không",
    Guid? PinnedModelId = null,
    Guid? ReferenceLineId = null) : IRequest<Result<OrderDto>>;
