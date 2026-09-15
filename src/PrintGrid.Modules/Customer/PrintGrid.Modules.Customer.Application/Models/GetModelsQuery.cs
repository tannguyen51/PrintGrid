using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

/// <summary>All models belonging to one customer, optionally filtered by name.</summary>
public record GetModelsQuery(
    Guid CustomerId,
    string? Search) : IRequest<Result<IReadOnlyList<ModelDto>>>;