using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

/// <summary>
/// Copies a published platform sample into a customer's own library (FR-CUST-014).
/// The caller has already streamed the sample binary into the customer's storage prefix;
/// geometry is carried over so the quote is identical to uploading the same file.
/// </summary>
public record CreateModelFromSampleCommand(
    Guid CustomerId,
    string Name,
    string? Description,
    string FileName,
    string FileFormat,
    long SizeBytes,
    IReadOnlyList<string>? Tags,
    string StorageKey,
    string? Sha256,
    decimal BoundingWidthMm,
    decimal BoundingDepthMm,
    decimal BoundingHeightMm,
    decimal VolumeCm3,
    int EstimatedPrintMinutes,
    decimal EstimatedMaterialGrams) : IRequest<Result<ModelDto>>;

public class CreateModelFromSampleCommandHandler : IRequestHandler<CreateModelFromSampleCommand, Result<ModelDto>>
{
    private readonly IModelRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public CreateModelFromSampleCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    {
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ModelDto>> Handle(CreateModelFromSampleCommand command, CancellationToken cancellationToken)
    {
        var model = Domain.Entities.Model.Create(
            command.CustomerId,
            command.Name,
            command.Description,
            command.FileName,
            command.FileFormat,
            command.SizeBytes,
            command.Tags,
            command.StorageKey,
            command.Sha256,
            emitUploadReceipt: false);

        model.ApplyGeometry(
            command.BoundingWidthMm,
            command.BoundingDepthMm,
            command.BoundingHeightMm,
            command.VolumeCm3,
            command.EstimatedPrintMinutes,
            command.EstimatedMaterialGrams,
            isWatertight: true,
            isManifold: true,
            isPrintable: true);

        await _models.AddAsync(model, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ModelMappings.ToDto(model));
    }
}
