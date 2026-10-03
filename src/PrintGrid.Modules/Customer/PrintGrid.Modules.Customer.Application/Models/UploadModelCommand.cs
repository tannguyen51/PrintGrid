using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public record UploadModelCommand(
    Guid CustomerId,
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string>? Tags,
    string StorageKey) : IRequest<Result<ModelDto>>;