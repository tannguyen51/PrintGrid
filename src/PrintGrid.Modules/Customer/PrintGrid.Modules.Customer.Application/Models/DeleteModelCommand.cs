using MediatR;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public record DeleteModelCommand(
    Guid CustomerId,
    Guid ModelId) : IRequest<Result>;