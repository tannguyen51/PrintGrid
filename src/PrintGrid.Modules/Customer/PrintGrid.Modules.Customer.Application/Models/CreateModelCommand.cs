using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public record CreateModelCommand(
    Guid CustomerId,
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string>? Tags) : IRequest<Result<ModelDto>>;