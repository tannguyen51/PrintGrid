using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public record GetModelQuery(
    Guid CustomerId,
    Guid ModelId) : IRequest<Result<ModelDto>>;