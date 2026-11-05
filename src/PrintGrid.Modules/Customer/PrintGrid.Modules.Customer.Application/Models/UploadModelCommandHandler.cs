using MediatR;
using PrintGrid.Modules.Customer.Application.DTOs;
using PrintGrid.Modules.Customer.Domain.Repositories;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.Results;

namespace PrintGrid.Modules.Customer.Application.Models;

public class UploadModelCommandHandler(IModelRepository models, IUnitOfWork unitOfWork)
    : IRequestHandler<UploadModelCommand, Result<ModelDto>>
{
    public async Task<Result<ModelDto>> Handle(UploadModelCommand command, CancellationToken cancellationToken)
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
            command.Sha256);

        await models.AddAsync(model, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ModelMappings.ToDto(model));
    }
}
